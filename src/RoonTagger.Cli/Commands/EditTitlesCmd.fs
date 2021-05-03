module RoonTagger.Cli.Commands.EditTitles

open Argu
open System.IO
open FsToolkit.ErrorHandling
open FsToolkit.ErrorHandling.Operator.Result
open Spectre.Console
open RoonTagger.Metadata
open RoonTagger.Metadata.TrackHelpers
open RoonTagger.Cli.Arguments
open RoonTagger.Cli.Configuration
open RoonTagger.Cli.ProcessRunner
open RoonTagger.Cli.Models
open RoonTagger.Cli.Output

let log = Serilog.Log.Logger

let private titlesFilePath =
    let fileName = "roon-tagger-edit-titles.txt"
    let currentDir = Directory.GetCurrentDirectory()
    Path.Combine(currentDir, fileName)

let getTracks = List.traverseResultA Track.load

let extractTitles (OrderedTracks tracks) =
    tracks
    |> List.map (fun t -> Track.safeGetTagStringValue t TitleTag)
    |> List.map List.head

let writeTitlesFile (lines: string list) path =
    try
        File.WriteAllLines(path, lines)
        Ok path
    with ex ->
        log.Error("Writing titles file: {Ex}", ex)

        [ CliIOError $"Error writing titles file: {ex.Message}" ] |> Error

let cleanup path =
    try
        if File.Exists path then
            File.Delete path

        Ok()
    with ex ->
        log.Error("Cleanup titles file: {Ex}", ex)
        Error [ CliIOError $"Error deleting titles file: {ex.Message}" ]

type EditMethod =
    | EditDirectly
    | EditAsync
    override this.ToString() =
        match this with
        | EditDirectly -> "Edit directly with configured text editor"
        | EditAsync -> "Save a file and wait for me to edit it"

let selectEditMethod (config: ConfigurationV1) =
    match config.Editor with
    | Some _ ->
        let prompt = SelectionPrompt<EditMethod>()
        prompt.Title <- "\nSelect a method (up/down arrow) for editing the tracks titles:"
        prompt.AddChoices([ EditDirectly; EditAsync ]) |> ignore
        AnsiConsole.Prompt prompt
    | None -> EditAsync

let prompt filePath =
    let message =
        [ $"Titles are saved to: '{filePath}' ordered by track number. Please edit the file without deleting/adding lines and without modifying the order of the lines."
          ""
          "ENTER to continue after you edited the file (To cancel just press ENTER without editing the file): " ]
        |> String.concat "\n"

    let prompt = TextPrompt<string>(message)
    prompt.AllowEmpty <- true
    AnsiConsole.Prompt prompt |> ignore
    AnsiConsole.WriteLine ""

let editTitlesWithEditor editorCommand path =
    let cmd = editorCommand.Cmd |> searchInPath
    let args = editorCommand.Arguments @ [ path ]
    runCmd cmd args


let readTitles filePath =
    try
        File.ReadAllLines(filePath) |> List.ofArray |> Ok
    with ex ->
        log.Error("Reading titles file: {Ex}", ex)

        [ CliIOError $"Error reading titles file: {ex.Message}" ] |> Error

let applyTitles (OrderedTracks tracks) (titles: string list) =
    let applyTrack (track, title) =
        Track.setTag track (RoonTag.Title title) |> Result.mapError MError

    try
        List.zip tracks titles |> Ok
    with :? System.ArgumentException -> [ TitlesCountError ] |> Error
    |> Result.bind (
        List.traverseResultM applyTrack
        >> Result.mapError (fun err -> [ err ])
    )

let handleCmd (args: ParseResults<EditTitlesArgs>) (config: ConfigurationV1) : Result<unit, unit> =
    let applyTags = Track.applyTags >> Result.mapError (List.map MError)

    result {
        let! tracks =
            getTracks (args.GetResult EditTitlesArgs.Files)
            |> Result.bind OrderedTracks.Create
            |> Result.mapError (List.map MError)

        let titles = extractTitles tracks
        let! path = writeTitlesFile titles titlesFilePath
        let editMethod = selectEditMethod config

        match editMethod with
        | EditAsync -> prompt path
        | EditDirectly ->
            let editorCmd = config.Editor.Value
            editTitlesWithEditor editorCmd path

        let! newTitles = readTitles path

        if titles = newTitles then
            do handleOutput "No change required ..."
            do! cleanup titlesFilePath
        else
            do!
                applyTitles tracks newTitles
                |> Result.bind (List.traverseResultM applyTags)
                |> Result.bind (fun _ -> cleanup titlesFilePath)
                |> Result.map (fun _ -> handleOutput "Operation finished successfully")
    }
    |> Result.teeError (fun _ -> cleanup titlesFilePath |> ignore)
    |> Result.mapError (List.map cliError2string >> handleErrors)

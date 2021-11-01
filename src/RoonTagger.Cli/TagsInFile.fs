module RoonTagger.Cli.TagsInFile

open RoonTagger.Cli.Models
open RoonTagger.Cli.Configuration
open RoonTagger.Cli.ProcessRunner
open RoonTagger.Metadata
open RoonTagger.Metadata.TrackHelpers
open FsToolkit.ErrorHandling
open Spectre.Console
open System.IO

let log = Serilog.Log.Logger

type EditMethod =
    | EditDirectly
    | EditAsync

    override this.ToString() =
        match this with
        | EditDirectly -> "Edit directly with configured text editor"
        | EditAsync -> "Save a file and wait for me to edit it"

let selectEditMethod (config: ConfigurationV1) tagsName =
    match config.Editor with
    | Some _ ->
        let prompt = SelectionPrompt<EditMethod>()
        prompt.Title <- $"\nSelect a method (up/down arrow) for editing the tracks {tagsName}:"
        prompt.AddChoices([ EditDirectly; EditAsync ]) |> ignore
        AnsiConsole.Prompt prompt
    | None -> EditAsync

let constructTagsFilePath tagName =
    let fileName = $"roon-tagger-edit-{tagName}.txt"
    let currentDir = Directory.GetCurrentDirectory()
    Path.Combine(currentDir, fileName)

let writeValues (lines: string list) path =
    try
        File.WriteAllLines(path, lines)
        Ok path
    with
    | ex ->
        log.Error("Writing tags edit file: {Ex}", ex)

        [ CliIOError $"Error writing tags edit file: {ex.Message}" ] |> Error

let readValues path =
    try
        File.ReadAllLines(path) |> List.ofArray |> Ok
    with
    | ex ->
        log.Error("Reading tags edit file: {Ex}", ex)

        [ CliIOError $"Error reading tags edit file: {ex.Message}" ] |> Error

let editTagsWithEditor editorCommand path =
    let cmd = editorCommand.Cmd |> searchInPath
    let args = editorCommand.Arguments @ [ path ]
    runCmd cmd args

let cleanup path =
    try
        if File.Exists path then
            File.Delete path

        Ok()
    with
    | ex ->
        log.Error("Cleanup tags edit file: {Ex}", ex)
        Error [ CliIOError $"Error deleting tags edit file: {ex.Message}" ]

let applyValues (mkTag: string -> RoonTag) (ConsecutiveTracks tracks) (values: string list) =
    let applyTrack (track, value) =
        Track.setTag track (mkTag value) |> Result.mapError MError

    try
        List.zip tracks values |> Ok
    with
    | :? System.ArgumentException -> [ TagsCountError ] |> Error
    |> Result.bind (
        List.traverseResultM applyTrack
        >> Result.mapError (fun err -> [ err ])
    )

let prompt filePath subject =
    let message =
        [ $"{subject} are saved to: '{filePath}' ordered by track number. Please edit the file without deleting/adding lines and without modifying the order of the lines."
          ""
          "ENTER to continue after you edited the file (To cancel just press ENTER without editing the file): " ]
        |> String.concat "\n"

    let prompt = TextPrompt<string>(message)
    prompt.AllowEmpty <- true
    AnsiConsole.Prompt prompt |> ignore
    AnsiConsole.WriteLine ""

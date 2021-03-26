module RoonTagger.Cli.Commands.EditTitles

open Argu
open System.IO
open FsToolkit.ErrorHandling
open FsToolkit.ErrorHandling.Operator.Result
open Spectre.Console
open RoonTagger.Metadata
open RoonTagger.Cli.Arguments
open RoonTagger.Cli.Output

let private titlesFilePath =
    let fileName = "roon-tagger-edit-titles.txt"
    let currentDir = Directory.GetCurrentDirectory()
    Path.Combine(currentDir, fileName)

let getTracks = List.traverseResultA Track.load

let extractTitles =
    List.map (fun t -> Track.safeGetTagStringValue t TagName.Title)
    >> List.map List.head

let writeTitlesFile (lines: string list) path =
    try
        File.WriteAllLines(path, lines)
        Ok path
    with ex -> [ UnexpectedError ex.Message ] |> Error

let prompt filePath =
    let message =
        [ ""
          $"Titles are saved to: '{filePath}' ordered as provided files. Please edit the file without deleting/adding lines and without modifying the order of the lines."
          ""
          "ENTER to continue (after you edited the file), CTRL+c to cancel: " ]
        |> String.concat "\n"

    AnsiConsole.Markup(message)
    System.Console.Read() |> ignore
    AnsiConsole.MarkupLine("")

let readTitles filePath =
    try
        File.ReadAllLines(filePath) |> List.ofArray |> Ok
    with ex -> [ UnexpectedError ex.Message ] |> Error

let applyTitles (tracks: AudioTrack list) (titles: string list) =
    let applyTrack (track, title) =
        Track.setTag track (RoonTag.Title title)

    try
        List.zip tracks titles |> Ok
    with :? System.ArgumentException ->
        [ UnexpectedError "the number of tracks does not match the number of titles. Did you delete or add a title?" ]
        |> Error
    |> Result.bind (
        List.traverseResultM applyTrack
        >> Result.mapError (fun err -> [ err ])
    )

let handleCmd (args: ParseResults<EditTitlesArgs>) : Result<unit, unit> =
    result {
        let! tracks = getTracks (args.GetResult Files)
        let titles = extractTitles tracks
        let! path = writeTitlesFile titles titlesFilePath
        do prompt path
        let! newTitles = readTitles path

        if titles = newTitles then
            do handleOutput "No change required ..."
        else
            do!
                applyTitles tracks newTitles
                |> Result.bind (List.traverseResultM Track.applyTags)
                |> Result.map (fun _ -> handleOutput "Operation finished successfully")
    }
    |> Result.mapError (List.map error2String >> handleErrors)

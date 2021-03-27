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

let sortTrackByTrackNumber (tracks: AudioTrack list) =
    result {
        let tnsString =
            tracks
            |> List.map (fun t -> Track.safeGetTagStringValue t TagName.TrackNumber)
            |> List.map List.head

        let! tns =
            try
                tnsString |> List.map int |> Ok
            with :? System.FormatException ->
                Error [ UnexpectedError "Couldn't parse track number. Does all tracks have valid track numbers?" ]

        return
            List.zip tracks tns
            |> List.sortBy snd
            |> List.map fst
    }

let extractTitles =
    List.map (fun t -> Track.safeGetTagStringValue t TagName.Title)
    >> List.map List.head

let writeTitlesFile (lines: string list) path =
    try
        File.WriteAllLines(path, lines)
        Ok path
    with ex -> [ UnexpectedError ex.Message ] |> Error

let cleanup path =
    try
        if File.Exists path then
            File.Delete path

        Ok()
    with ex -> Error [ UnexpectedError ex.Message ]

let prompt filePath =
    let message =
        [ ""
          $"Titles are saved to: '{filePath}' ordered by track number. Please edit the file without deleting/adding lines and without modifying the order of the lines."
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
        let! tracks =
            getTracks (args.GetResult Files)
            |> Result.bind sortTrackByTrackNumber

        let titles = extractTitles tracks
        let! path = writeTitlesFile titles titlesFilePath
        do prompt path
        let! newTitles = readTitles path

        if titles = newTitles then
            do handleOutput "No change required ..."
            do! cleanup titlesFilePath
        else
            do!
                applyTitles tracks newTitles
                |> Result.bind (List.traverseResultM Track.applyTags)
                |> Result.bind (fun _ -> cleanup titlesFilePath)
                |> Result.map (fun _ -> handleOutput "Operation finished successfully")
    }
    |> Result.teeError (fun _ -> cleanup titlesFilePath |> ignore)
    |> Result.mapError (List.map error2String >> handleErrors)

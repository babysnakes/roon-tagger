module RoonTagger.Cli.Commands.EditTitles

open Argu
open FsToolkit.ErrorHandling
open FsToolkit.ErrorHandling.Operator.Result
open Spectre.Console
open RoonTagger.Metadata
open RoonTagger.Metadata.TrackHelpers
open RoonTagger.Cli.Arguments
open RoonTagger.Cli.Configuration
open RoonTagger.Cli.Models
open RoonTagger.Cli.Output
open RoonTagger.Cli.TagsInFile

let log = Serilog.Log.Logger

let getTracks = List.traverseResultA Track.load

let applyTitles = applyValues RoonTag.Title

let extractTitles (ConsecutiveTracks tracks) =
    tracks
    |> List.map (fun t -> Track.safeGetTagStringValue t TitleTag |> List.head)

let handleCmd (args: ParseResults<EditTitlesArgs>) (config: ConfigurationV1) : Result<unit, unit> =
    let applyTags = Track.applyTags >> Result.mapError (List.map MError)
    let titlesFilePath = constructTagsFilePath "titles"

    result {
        let! tracks =
            getTracks (args.GetResult EditTitlesArgs.Files)
            |> Result.bind ConsecutiveTracks.Create
            |> Result.mapError (List.map MError)

        let titles = extractTitles tracks
        let! path = writeValues titles titlesFilePath
        let editMethod = selectEditMethod config "titles"

        match editMethod with
        | EditAsync -> prompt path "Titles"
        | EditDirectly ->
            let editorCmd = config.Editor.Value
            editTagsWithEditor editorCmd path

        let! newTitles = readValues path

        if titles = newTitles then
            do handleOutput "No change required ..."
            do! cleanup path
        else
            do!
                applyTitles tracks newTitles
                |> Result.bind (List.traverseResultM applyTags)
                |> Result.bind (fun _ -> cleanup path)
                |> Result.map (fun _ -> handleOutput "Operation finished successfully")
    }
    |> Result.teeError (fun _ -> cleanup titlesFilePath |> ignore)
    |> Result.mapError (List.map cliError2string >> handleErrors)

let makeEditTitlesCmd (args: ParseResults<EditTitlesArgs>) (config: ConfigurationV1) =
    { new ISubCommand with
        member this.Run() = handleCmd args config

        member this.LongHelp() =
            [ Markup("help wanted on [italic]edit-titles[/]") ] }

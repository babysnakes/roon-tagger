module RoonTagger.Cli.Commands.ExtractWorks

open Argu
open FsToolkit.ErrorHandling
open Spectre.Console
open RoonTagger.Metadata
open RoonTagger.Metadata.TrackHelpers
open RoonTagger.Metadata.Utils
open RoonTagger.Metadata.WorkMovement
open RoonTagger.Cli.Arguments
open RoonTagger.Cli.Configuration
open RoonTagger.Cli.Models
open RoonTagger.Cli.Output
open RoonTagger.Cli.TagsInFile

let log = Serilog.Log.Logger

type WorksPromptResponse =
    | Save
    | Cancel
    | EditWorks

    override this.ToString() =
        match this with
        | Save -> "Save all works"
        | Cancel -> "Cancel"
        | EditWorks -> "Edit each work individually"

type WorkPromptResponse =
    | SaveWork
    | DeleteWork
    | ViewWork
    | EditWorkName
    | EditMovements

    override this.ToString() =
        match this with
        | SaveWork -> "Save this work"
        | DeleteWork -> "Ignore this work (don't save this work)"
        | ViewWork -> "View this work again"
        | EditWorkName -> "Edit work name"
        | EditMovements -> "Edit movements in a file"

let printWork (Work (name, ConsecutiveTracks tracks)) =
    log.Debug("Printing work '{Name}' with tracks: {Tracks}", name, tracks)
    let grid = Grid()
    let firstColumn = GridColumn()
    firstColumn.NoWrap |> ignore
    AnsiConsole.MarkupLine($"| [green]*[/] {name}") |> ignore
    grid.AddColumn(firstColumn) |> ignore
    grid.AddColumn(GridColumn()) |> ignore

    tracks
    |> List.iteri
        (fun idx t ->
            let movement = Track.safeGetTagStringValue t MovementTag |> List.head
            grid.AddRow($"|-> {idx + 1}:", $"{movement}") |> ignore)

    grid.AddEmptyRow() |> ignore
    AnsiConsole.Render(grid)

let printWorks = List.iter printWork

let applyMovements = applyValues RoonTag.Movement

let promptWorksOperation () =
    let prompt = SelectionPrompt<WorksPromptResponse>()
    prompt.Title <- "Check works above and use up/down to select operation below:"
    prompt.AddChoices([ Save; EditWorks; Cancel ]) |> ignore
    AnsiConsole.Prompt prompt

type WorkProcessor =
    { Config: ConfigurationV1 }

    member this.EditMovements(ConsecutiveTracks tracks as cTracks) : Result<unit, CliErrors list> =
        let movementsFilePath = constructTagsFilePath "movements"
        log.Debug("Movement file path is '{MovementsFilePath}'", movementsFilePath)

        result {
            let movements =
                tracks
                |> List.map (fun t -> Track.safeGetTagStringValue t MovementTag |> List.head)

            log.Verbose("Extracted movements: {Movements}", movements)

            let! path = writeValues movements movementsFilePath

            selectEditMethod this.Config "movements"
            |> function
            | EditAsync ->
                log.Verbose("User selected to edit movements async")
                prompt path "Movements"
            | EditDirectly ->
                log.Verbose("User selected to edit movements directly")
                editTagsWithEditor this.Config.Editor.Value path

            let! newMovements = readValues path
            log.Verbose("Edited movements: {NewMovements}", newMovements)

            if movements = newMovements then
                do! cleanup path
            else
                do! applyMovements cTracks newMovements |> Result.map ignore
                do! cleanup path
        }
        |> Result.teeError (fun _ -> cleanup movementsFilePath |> ignore)

    member _.PromptWorkOperation(Work (workName, _)) =
        let prompt = SelectionPrompt<WorkPromptResponse>()
        prompt.Title <- $"[yellow]{workName.EscapeMarkup()}:[/]"

        prompt.AddChoices(
            [ ViewWork
              SaveWork
              EditWorkName
              EditMovements
              DeleteWork ]
        )
        |> ignore

        AnsiConsole.Prompt(prompt)

    member this.HandleSingleWork(work: Work) : Result<string, CliErrors list> =

        let rec loop (Work (name, cTracks) as work) =
            let (ConsecutiveTracks tracks) = cTracks

            match (this.PromptWorkOperation work) with
            | SaveWork ->
                log.Debug("Applying work: {Name}", name)

                applyWork work
                |> Result.mapError (List.map MError)
                |> Result.map (fun _ -> $"Work [yellow]{name.EscapeMarkup()}[/] saved successfully")
            | DeleteWork ->
                log.Debug("Ignoring work: {Name}", name)
                Ok $"Work [yellow]{name.EscapeMarkup()}[/] ignored"
            | ViewWork ->
                log.Debug("Viewing work: {Name}", name)
                AnsiConsole.Render(Rule($"[yellow] * {name.EscapeMarkup()}[/]"))
                printWork work
                loop work
            | EditWorkName ->
                let prompt = TextPrompt("New Work Name")
                prompt.DefaultValue(name) |> ignore
                let response = AnsiConsole.Prompt(prompt)
                log.Debug("Rename work '{Name}' to '{Response}'", name, response)
                loop (Work(response, cTracks))
            | EditMovements ->
                log.Debug("Editing movements of: '{Name}'", name)

                match (this.EditMovements cTracks) with
                | Ok _ -> loop (Work(name, cTracks))
                | Error err -> Error err

        loop work

let handleCmd (args: ParseResults<ExtractWorksArgs>) (config: ConfigurationV1) : Result<unit, unit> =
    let inline toCliErrors input =
        input |> Result.mapError (List.map MError)

    result {
        let addRomans = args.Contains Add_Roman_Numerals
        let files = args.GetResult ExtractWorksArgs.Files
        let! tracks = List.traverseResultA Track.load files |> toCliErrors
        let! consecutiveTracks = tracks |> ConsecutiveTracks.Create |> toCliErrors
        let! works = extractWorks consecutiveTracks addRomans |> toCliErrors
        log.Debug("Extracted works: %A{Works}", works)
        printWorks works

        match (promptWorksOperation ()) with
        | Save ->
            do!
                works
                |> List.traverseResultM applyWork
                |> Result.map ignore
                |> toCliErrors

            AnsiConsole.WriteLine()
            handleOutput "Works Saved Successfully"
        | EditWorks ->
            AnsiConsole.WriteLine()
            let wProcessor = { Config = config }

            do!
                works
                |> List.traverseResultM wProcessor.HandleSingleWork
                |> Result.map (List.map handleOutput)
                |> Result.map ignore
        | Cancel ->
            AnsiConsole.WriteLine()
            infoMessage "Operation cancelled"
    }
    |> Result.mapError (List.map cliError2string >> handleErrors)

let makeExtractWorkCommand (args: ParseResults<ExtractWorksArgs>) (config: ConfigurationV1) =
    { new ISubCommand with
        member this.Run() = handleCmd args config

        member this.LongHelp() =
            infoMessage "help wanted on extract-work" |> Ok }

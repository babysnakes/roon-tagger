module RoonTagger.Cli.Commands.ExtractWorks

open Argu
open FsToolkit.ErrorHandling
open Spectre.Console
open RoonTagger.Metadata
open RoonTagger.Metadata.TrackHelpers
open RoonTagger.Metadata.WorkMovement
open RoonTagger.Cli.Arguments
open RoonTagger.Cli.Output

let log = Serilog.Log.Logger

let printWork (Work (name, tracks)) =
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

let handleCmd (args: ParseResults<ExtractWorksArgs>) : Result<unit, unit> =
    result {
        let files = args.GetResult ExtractWorksArgs.Files
        let! tracks = List.traverseResultA Track.load files
        let! consecutiveTracks = tracks |> ConsecutiveTracks.Create
        let! works = extractWorks consecutiveTracks
        log.Debug("Extracted works: %A{Works}", works)
        printWorks works

        if AnsiConsole.Confirm("Save these works/movements?", false) then
            do! works |> List.traverseResultM applyWork |> Result.map ignore
            AnsiConsole.WriteLine()
            handleOutput "Works Saved Successfully"
        else
            AnsiConsole.WriteLine()
            infoMessage "Operation cancelled"
    }
    |> Result.mapError (List.map error2String >> handleErrors)

module RoonTagger.Cli.Commands.View

open System
open Argu
open FsToolkit.ErrorHandling
open Spectre.Console
open RoonTagger.Cli.Arguments
open RoonTagger.Cli.Output
open RoonTagger.Metadata

let conditionallyPrint (grid: Grid) (head: string) (value: string list) =
    let processedValue =
        value
        |> List.filter (fun s -> String.IsNullOrEmpty(s) |> not)

    if not (List.isEmpty processedValue) then
        grid.AddRow($"[b]{head}[/]", processedValue |> String.concat ", ")
        |> ignore

let handleCmd (args: ParseResults<ViewArgs>) =
    result {
        let! track = args.GetResult File |> Track.load
        let getValue = Track.getTagStringValue track
        let album = getValue TagName.Album
        let artist = getValue TagName.Artist
        let title = getValue TagName.Title
        let tn = getValue TagName.TrackNumber
        let importDate = getValue TagName.ImportDate
        let ord = getValue TagName.OriginalReleaseDate
        let year = getValue TagName.Year

        let grid = Grid()
        let print = conditionallyPrint grid
        let fileName = track.Path |> IO.Path.GetFileName

        grid.AddColumn(
            GridColumn().PadRight(4)
            |> ColumnExtensions.NoWrap
        )
        |> ignore

        grid.AddColumn(GridColumn()) |> ignore
        grid.AddRow("") |> ignore
        print "Album" album
        print "Artist" artist
        print "Title" title
        print "Track Number" tn
        print "Import Date" importDate
        print "Release Date" ord
        print "Year" year

        let panel = PanelExtensions.Header(Panel(grid), $"Info: {fileName} ")
        AnsiConsole.Render(panel)
    }
    |> Result.mapError (fun err -> handleErrors [ error2String err ])

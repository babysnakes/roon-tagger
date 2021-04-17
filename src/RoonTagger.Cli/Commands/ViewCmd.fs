module RoonTagger.Cli.Commands.View

open System
open Argu
open FsToolkit.ErrorHandling
open Spectre.Console
open RoonTagger.Cli.Arguments
open RoonTagger.Cli.Output
open RoonTagger.Metadata

let conditionallyPrint (grid: Grid) (head: string) (value: string list) =
    let processedValue = value |> List.filter (fun s -> String.IsNullOrEmpty(s) |> not)

    if not (List.isEmpty processedValue) then
        grid.AddRow($"[b]{head}[/]", processedValue |> String.concat ", ")
        |> ignore

let printRawCredits (grid: Grid) (credits: string list) =
    if not (List.isEmpty credits) then
        grid.AddEmptyRow() |> ignore
        grid.AddRow("[bold]Credits:[/]") |> ignore

        for c in (List.sort credits) do
            grid.AddRow("", c) |> ignore

let printCredits (grid: Grid) (credits: string list) =
    let sp = credits |> List.map (fun s -> s.Split(" - ", 2) |> List.ofArray)
    let cs, other = List.partition (fun l -> List.length l = 2) sp
    let credits = cs |> List.map (fun l -> (l.[1], l.[0]))
    let byRole = credits |> List.groupBy fst

    if not (List.isEmpty sp) then
        grid.AddEmptyRow() |> ignore
        grid.AddRow("[bold]Credits:[/]") |> ignore

        for cs in byRole do
            grid.AddRow("", $"[bold]{cs |> fst}[/]") |> ignore

            for c in cs |> snd do
                grid.AddRow("", $"  * {c |> snd}") |> ignore


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
        let credits = getValue TagName.Credit

        let grid = Grid()
        let print = conditionallyPrint grid
        let fileName = track.Path |> IO.Path.GetFileName

        grid.AddColumn(GridColumn().PadRight(4) |> ColumnExtensions.NoWrap)
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

        if args.Contains Raw_Credits then
            printRawCredits grid credits
        else
            printCredits grid credits

        let panel = PanelExtensions.Header(Panel(grid), $"Info: {fileName} ")
        AnsiConsole.Render(panel)
    }
    |> Result.mapError (fun err -> handleErrors [ error2String err ])

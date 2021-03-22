module RoonTagger.Cli.Output

open RoonTagger.Metadata
open Spectre.Console

let error2String (err: MetadataErrors) : string =
    match err with
    | FileDoesNotExist err -> err
    | InvalidFileFormat err -> err
    | UnexpectedError err -> sprintf "Unexpected error: %s" err
    | UnsupportedTagOperation _
    | UnsupportedTagForFormat _
    | FileSaveError _ -> "TODO: ERROR"

let handleErrors (errs: MetadataErrors list) =
    AnsiConsole.MarkupLine("[red]Errors:[/]")
    let grid = Grid()

    grid.AddColumn(GridColumn() |> ColumnExtensions.NoWrap)
    |> ignore

    grid.AddColumn(GridColumn()) |> ignore

    errs
    |> List.iter
        (fun err ->
            grid.AddRow("  [red]*[/]", error2String (err))
            |> ignore)

    AnsiConsole.Render(grid)

let handleOutput (out: string) =
    AnsiConsole.MarkupLine($"[green]Success:[/] {out}")

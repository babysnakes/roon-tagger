module RoonTagger.Cli.Output

open RoonTagger.Metadata
open RoonTagger.Cli.Models
open Spectre.Console

let error2String (err: MetadataErrors) : string =
    match err with
    | FileDoesNotExist err -> err
    | InvalidFileFormat err -> err
    | UnexpectedError err -> sprintf "Unexpected error: %s" err
    | DeletingNonExistingPersonnel (track, err) -> $"'{track.Path}': Trying to delete non existing credit: {err}"
    | FileSaveError err -> $"Error saving file: {err}"
    | MissingOrInvalidTag tag -> $"Missing or invalid tag: {tag.ToString()}"
    | UnsupportedTagOperation _
    | UnsupportedTagForFormat -> "TODO: Error"

let cliError2string (err: CliErrors) : string =
    match err with
    | MError err -> error2String err
    | CliIOError message -> $"File error: {message}"
    | TitlesCountError -> "The number of tracks does not match the number of titles. Did you delete or add a title?"
    | ConfigurationParseError err -> $"Error parsing configuration: {err}"

let handleErrors (errs: string list) =
    AnsiConsole.MarkupLine("[red]Errors:[/]")
    let grid = Grid()
    grid.AddColumn(GridColumn() |> ColumnExtensions.NoWrap) |> ignore
    grid.AddColumn(GridColumn()) |> ignore

    errs
    |> List.iter (fun err -> grid.AddRow("  [red]*[/]", err) |> ignore)

    AnsiConsole.Render(grid)

let handleOutput (out: string) =
    AnsiConsole.MarkupLine($"[green]Success:[/] {out}")

let infoMessage (message: string) =
    AnsiConsole.MarkupLine($"[yellow]Info:[/] {message}")

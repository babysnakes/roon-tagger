// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open Argu
open System
open FsToolkit.ErrorHandling
open RoonTagger.Cli
open RoonTagger.Cli.Arguments
open RoonTagger.Cli.Commands
open Spectre.Console

[<EntryPoint>]
let main argv =
    let errorHandler =
        ProcessExiter(
            colorizer =
                function
                | ErrorCode.HelpText -> None
                | _ -> Some ConsoleColor.Red
        )

    let parser =
        ArgumentParser.Create<MainArgs>(programName = Info.Name, errorHandler = errorHandler)

    try
        parser.Parse argv
        |> Main.runMain
        |> Result.tee (fun _ -> Console.WriteLine("")) // Add an empty line for a little space
        |> function
        | Ok _ -> 0
        | _ -> 1
    with ex ->
        AnsiConsole.WriteException(ex)
        1

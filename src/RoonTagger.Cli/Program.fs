// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open Argu
open System
open RoonTagger.Cli.Arguments
open RoonTagger.Cli.Commands

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
        ArgumentParser.Create<MainArgs>(programName = "roon-tagger", errorHandler = errorHandler)

    parser.Parse argv
    |> Main.handleCmd
    |> function
    | Ok _ -> 0
    | _ -> 1

module RoonTagger.Cli.Commands.Configure

open Argu
open FsToolkit.ErrorHandling
open Spectre.Console
open RoonTagger.Cli.Arguments
open RoonTagger.Cli.Configuration
open RoonTagger.Cli.Models
open RoonTagger.Cli.Output
open System

let extractEditorWithArgs (args: ParseResults<ConfigureArgs>) =
    let parseEditor (cmd: string, argsAsStrings: string) =
        let args = argsAsStrings.Split(",") |> List.ofArray

        if List.exists String.IsNullOrWhiteSpace args then
            failwith $"Arguments contains empty or space only arguments: ({argsAsStrings})"

        (cmd, args)

    args.PostProcessResults(<@ Editor_With_Args @>, parseEditor)
    |> function
    | [] -> None
    | lst -> Some(lst |> List.last)

let resetEditor (config: ConfigurationV1) (configPath: string) =
    result {
        let newConfig = { config with Editor = None }

        return!
            saveConfig newConfig configPath
            |> Result.map (fun _ -> handleOutput "Editor reset successfully")
    }

let extractEditorConfig (args: ParseResults<ConfigureArgs>) (defaultCmd: EditorCommandV1 option) =
    let editorWithArgsParser (cmd, arguments) = { Cmd = cmd; Arguments = arguments }
    let editorParser cmd = { Cmd = cmd; Arguments = [] }

    let editorWithoutArgs () =
        args.TryGetResult Editor |> Option.map editorParser

    extractEditorWithArgs args
    |> Option.map editorWithArgsParser
    |> Option.orElse (editorWithoutArgs ())
    |> Option.orElse defaultCmd

let checkArgsValidity (args: ParseResults<ConfigureArgs>) =
    match (args.Contains Reset_Editor, args.Contains Editor_With_Args, args.Contains Editor) with
    | true, true, _
    | true, _, true ->
        CliArgumentsError "Providing reset editor with configure editor in the same invocation is forbidden."
        |> Error
    | false, true, true ->
        CliArgumentsError "Providing two different editor configurations in the same invocation is forbidden."
        |> Error
    | _, _, _ -> Ok()

let updateConfig (args: ParseResults<ConfigureArgs>) (config: ConfigurationV1) (configPath: string) =
    result {
        do! checkArgsValidity args
        let lc = config.Log
        let logFile = args.GetResult(Log_File, defaultValue = lc.File)
        let logLevel = args.GetResult(Log_Level, defaultValue = lc.Level)
        let newLc = { File = logFile; Level = logLevel }
        let newEc = extractEditorConfig args config.Editor
        let newConfig = { Editor = newEc; Log = newLc }

        if config = newConfig then
            infoMessage "No configuration changes made as new values match the current ones."
        else
            do!
                saveConfig newConfig configPath
                |> Result.map (fun _ -> handleOutput "Configuration saved successfully")

    }

let handleCmd (args: ParseResults<ConfigureArgs>) (config: ConfigurationV1) (configPath: string) =
    (if args.Contains Show then
         printfn "%A" config |> Ok
     elif args.Contains Reset_Editor then
         resetEditor config configPath
     else
         updateConfig args config configPath)
    |> Result.mapError (fun err -> handleErrors [ cliError2string err ])

let makeConfigureCmd (args: ParseResults<ConfigureArgs>) (config: ConfigurationV1) (configPath: string) =
    { new ISubCommand with
        member this.Run() = handleCmd args config configPath

        member this.LongHelp() =
            [ Markup("help wanted on [italic]configure[/]") ] }

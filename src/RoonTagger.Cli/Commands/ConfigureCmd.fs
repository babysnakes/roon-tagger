module RoonTagger.Cli.Commands.Configure

open Argu
open FsToolkit.ErrorHandling
open RoonTagger.Cli.Arguments
open RoonTagger.Cli.Configuration
open RoonTagger.Cli.Info
open RoonTagger.Cli.Output

let extractEditorConfig (args: ParseResults<ConfigureArgs>) (defaultCmd: EditorCommandV1 option) =
    if args.Contains Editor_Command then
        args.TryGetResult Editor_Command
        |> Option.map (fun (cmd, arguments) -> { Cmd = cmd; Arguments = arguments })
    else
        defaultCmd

let updateConfig (args: ParseResults<ConfigureArgs>) (configPath: string) =
    result {
        let! config = loadConfigWithDefault configPath
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

let showConfig configPath =
    result {
        let! config = loadConfigWithDefault configPath
        let! savedConfig = loadConfig configPath

        if savedConfig |> Option.isNone then
            infoMessage "No configuration file found, showing default configuration.\n"

        printfn "%A" config
    }

let handleCmd (args: ParseResults<ConfigureArgs>) (configPath: string) =
    (if args.Contains Show then
         showConfig configPath
     else
         updateConfig args configPath)
    |> Result.mapError (fun err -> handleErrors [ cliError2string err ])

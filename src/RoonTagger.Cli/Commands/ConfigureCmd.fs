module RoonTagger.Cli.Commands.Configure

open Argu
open FsToolkit.ErrorHandling
open RoonTagger.Cli.Arguments
open RoonTagger.Cli.Configuration
open RoonTagger.Cli.Info
open RoonTagger.Cli.Output

let extractEditorConfig (args: ParseResults<ConfigureArgs>) (defaultCmd: EditorCommandV1 option) =
    args.TryGetResult Editor_Command
    |> Option.map (fun (cmd, arguments) -> { Cmd = cmd; Arguments = arguments })
    |> Option.orElse defaultCmd

let updateConfig (args: ParseResults<ConfigureArgs>) (config: ConfigurationV1) (configPath: string) =
    result {
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
     else
         updateConfig args config configPath)
    |> Result.mapError (fun err -> handleErrors [ cliError2string err ])

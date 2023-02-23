module RoonTagger.Cli.Configuration

open System
open System.IO
open FSharp.Json
open FsToolkit.ErrorHandling
open RoonTagger.Cli
open RoonTagger.Cli.Models

let log = Serilog.Log.Logger

type LogLevel =
    | None = 0
    | Info = 1
    | Debug = 2
    | Trace = 3

type EditorCommandV1 = { Cmd: string; Arguments: string list }
type LogConfigV1 = { File: string; Level: LogLevel }

type ConfigurationV1 =
    { Editor: EditorCommandV1 option
      Log: LogConfigV1 }

type ConfigurationVersion =
    | V1 = 0

/// Get the local directory to save configurations to.
let getConfigDirectory () =
    // Todo: handle exceptions
    let localAppData =
        Environment.GetFolderPath Environment.SpecialFolder.LocalApplicationData

    Path.Join(localAppData, Info.Name)

/// Returns the full path of config file based on dir, base name and version.
let getConfigFilePath (dir: string) (baseName: string) (version: ConfigurationVersion) =
    let vName = version.ToString().ToLower()
    Path.Join(dir, $"{baseName}_{vName}.json")

/// Loads the configuration files if exists.
let loadConfig (path: string) : Result<ConfigurationV1 option, CliErrors> =
    let configFile = if File.Exists path then Some path else None

    try
        configFile
        |> Option.map File.ReadAllText
        |> Option.map (fun j -> Json.deserialize<ConfigurationV1> j)
        |> Ok
    with
    | :? JsonDeserializationError as ex ->
        log.Error("Loading configuration: {Ex}", ex)
        Error(ConfigurationParseError ex.Message)
    | ex ->
        log.Error("Loading configuration: {Ex}", ex)
        Error(CliIOError $"Error reading configuration file: {ex.Message}")

/// Returns saved config or default config.
let loadConfigWithDefault (path: string) : Result<ConfigurationV1, CliErrors> =
    let defaultConfig =
        { Editor = None
          Log =
            { File = $"{Info.Name}.log"
              Level = LogLevel.None } }

    loadConfig path |> Result.map (Option.defaultValue defaultConfig)

/// Serialize the configuration to the specified path with one backup.
let saveConfig (config: ConfigurationV1) (path: string) : Result<unit, CliErrors> =
    result {
        let contents = Json.serialize config
        let dirName = Path.GetDirectoryName path

        do!
            try
                Directory.CreateDirectory dirName |> ignore |> Ok
            with ex ->
                log.Error("Create configuration directory: {Ex}", ex)
                Error(CliIOError $"Error creating config directory: {ex.Message}")

        do!
            try
                if File.Exists path then
                    File.Copy(path, path + ".bak", true) |> Ok
                else
                    Ok()
            with ex ->
                log.Error("Backing up configuration: {Ex}", ex)
                Error(CliIOError $"Error backing up old config: {ex.Message}")

        do!
            try
                File.WriteAllText(path, contents) |> Ok
            with ex ->
                log.Error("Saving configuration: {Ex}", ex)
                Error(CliIOError $"Error saving configuration: {ex.Message}")
    }

module RoonTagger.Cli.Commands.Main

open System
open Argu
open FsToolkit.ErrorHandling
open Serilog
open RoonTagger.Cli
open RoonTagger.Cli.Arguments
open RoonTagger.Cli.Commands
open RoonTagger.Cli.Configuration
open RoonTagger.Cli.Output

let (|VersionCmd|_|) (opts: ParseResults<MainArgs>) = opts.TryGetResult Version
let (|SetTagsCmd|_|) (opts: ParseResults<MainArgs>) = opts.TryGetResult Set_Tags
let (|EditTitlesCmd|_|) (opts: ParseResults<MainArgs>) = opts.TryGetResult Edit_Titles
let (|ViewCmd|_|) (opts: ParseResults<MainArgs>) = opts.TryGetResult View
let (|CreditsCmd|_|) (opts: ParseResults<MainArgs>) = opts.TryGetResult Credits
let (|ConfigureCmd|_|) (opts: ParseResults<MainArgs>) = opts.TryGetResult Configure

let setupLogger (lc: LogConfigV1) (overrides: int) =
    let level =
        match overrides with
        | 0 -> lc.Level
        | 1 -> LogLevel.Info
        | 2 -> LogLevel.Debug
        | 3
        | _ -> LogLevel.Trace

    let f () =
        LoggerConfiguration().WriteTo.File(lc.File)

    match level with
    | LogLevel.None -> None
    | LogLevel.Info -> Some(f().MinimumLevel.Information().CreateLogger())
    | LogLevel.Debug -> Some(f().MinimumLevel.Debug().CreateLogger())
    | LogLevel.Trace
    | _ -> Some(f().MinimumLevel.Verbose().CreateLogger())

let handleCmd (opts: ParseResults<MainArgs>) =
    result {
        let appDir = getConfigDirectory ()
        let configFile = getConfigFilePath appDir "config" ConfigurationVersion.V1
        let! config = loadConfigWithDefault configFile
        let verbosity = opts.GetResults Verbose |> List.length

        setupLogger config.Log verbosity
        |> Option.map
            (fun logger ->
                Console.WriteLine ""
                infoMessage $"Logs will be written to '{config.Log.File}'"
                Log.Logger <- logger)
        |> ignore

        Console.WriteLine("")

        match opts with
        | VersionCmd _ -> return infoMessage $"{Info.Name}: {Info.Version}" |> Ok
        | SetTagsCmd args -> return SetTags.handleCmd args
        | EditTitlesCmd args -> return EditTitles.handleCmd args config
        | ViewCmd args -> return View.handleCmd args
        | CreditsCmd args -> return Credits.handleCmd args
        | ConfigureCmd args -> return Configure.handleCmd args configFile
        | _ ->
            return
                handleOutput "Move along, nothing to see here..."
                |> Ok
    }

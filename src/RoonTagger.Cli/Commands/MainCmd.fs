module RoonTagger.Cli.Commands.Main

open System
open Argu
open FsToolkit.ErrorHandling
open Serilog
open Spectre.Console
open Spectre.Console.Rendering
open RoonTagger.Cli
open RoonTagger.Cli.Arguments
open RoonTagger.Cli.Commands
open RoonTagger.Cli.Configuration
open RoonTagger.Cli.Help.LongHelp
open RoonTagger.Cli.Models
open RoonTagger.Cli.Output

let writeLine (r: IRenderable) =
    AnsiConsole.Write(r)
    AnsiConsole.MarkupLine("")

let (|SetTagsCmd|_|) (opts: ParseResults<MainArgs>) = opts.TryGetResult Set_Tags
let (|EditTitlesCmd|_|) (opts: ParseResults<MainArgs>) = opts.TryGetResult Edit_Titles
let (|ViewCmd|_|) (opts: ParseResults<MainArgs>) = opts.TryGetResult View
let (|CreditsCmd|_|) (opts: ParseResults<MainArgs>) = opts.TryGetResult Credits
let (|ConfigureCmd|_|) (opts: ParseResults<MainArgs>) = opts.TryGetResult Configure
let (|ExtractWorksCmd|_|) (opts: ParseResults<MainArgs>) = opts.TryGetResult Extract_Works

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

let handleCmd (_: ParseResults<MainArgs>) : Result<unit, unit> = mainLH |> print |> Ok

let makeMainCmd (args: ParseResults<MainArgs>) =
    { new ISubCommand with
        member this.Run() = handleCmd args
        member this.LongHelp() = mainLH }

let runMain (opts: ParseResults<MainArgs>) =
    result {
        let appDir = getConfigDirectory ()
        let configFile = getConfigFilePath appDir "config" ConfigurationVersion.V1
        let! config = loadConfigWithDefault configFile
        let verbosity = opts.GetResults Verbose |> List.length

        setupLogger config.Log verbosity
        |> Option.map (fun logger ->
            Console.WriteLine ""
            infoMessage $"Logs will be written to '{config.Log.File}'"
            Log.Logger <- logger)
        |> ignore

        Console.WriteLine("")

        if opts.Contains Version then
            return infoMessage $"{Info.Name}: {Info.Version}" |> Ok
        else
            let subCommand =
                match opts with
                | SetTagsCmd args -> SetTags.makeSetArgsCmd args
                | EditTitlesCmd args -> EditTitles.makeEditTitlesCmd args config
                | ViewCmd args -> View.makeViewCmd args
                | CreditsCmd args -> Credits.makeCreditsCmd args
                | ConfigureCmd args -> Configure.makeConfigureCmd args config configFile
                | ExtractWorksCmd args -> ExtractWorks.makeExtractWorkCommand args config
                | _ -> makeMainCmd opts

            if opts.Contains Long_Help then
                return subCommand.LongHelp() |> print |> Ok
            else
                return subCommand.Run()
    }

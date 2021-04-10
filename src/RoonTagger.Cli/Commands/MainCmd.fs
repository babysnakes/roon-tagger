module RoonTagger.Cli.Commands.Main

open System
open Argu
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

let handleCmd (opts: ParseResults<MainArgs>) =
    let appDir = getConfigDirectory ()
    let configFile = getConfigFilePath appDir "config" ConfigurationVersion.V1

    Console.WriteLine("")

    if (opts.Contains Verbose) then
        Console.WriteLine($"DEBUG: args: {opts}")
        Console.WriteLine("")


    match opts with
    | VersionCmd _ -> infoMessage $"{Info.Name}: {Info.Version}" |> Ok
    | SetTagsCmd args -> SetTags.handleCmd args
    | EditTitlesCmd args -> EditTitles.handleCmd args
    | ViewCmd args -> View.handleCmd args
    | CreditsCmd args -> Credits.handleCmd args
    | ConfigureCmd args -> Configure.handleCmd args configFile
    | _ ->
        handleOutput "Move along, nothing to see here..."
        |> Ok

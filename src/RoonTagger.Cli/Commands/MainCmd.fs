module RoonTagger.Cli.Commands.Main

open Argu
open RoonTagger.Cli.Arguments
open RoonTagger.Cli.Commands
open RoonTagger.Cli.Output

let (|VersionCmd|SetTagsCmd|NoCmd|) (opts: ParseResults<MainArgs>) =
    if (opts.Contains Version) then
        VersionCmd
    elif (opts.Contains Set_Tags) then
        (SetTagsCmd(opts.GetResult Set_Tags))
    else
        NoCmd

let handleCmd (opts: ParseResults<MainArgs>) =
    match opts with
    | VersionCmd -> handleOutput "Version ..." |> Ok
    | SetTagsCmd args -> SetTags.handleCmd args
    | NoCmd -> handleOutput "Move along, nothing to see here..." |> Ok

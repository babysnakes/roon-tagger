module RoonTagger.Cli.Commands.Main

open Argu
open RoonTagger.Cli.Arguments
open RoonTagger.Cli.Commands

let (|VersionCmd|SetTagsCmd|NoCmd|) (opts: ParseResults<MainArgs>) =
    if (opts.Contains Version) then
        VersionCmd
    elif (opts.Contains Set_Tags) then
        (SetTagsCmd(opts.GetResult Set_Tags))
    else
        NoCmd

let handleCmd (opts: ParseResults<MainArgs>) =
    match opts with
    | VersionCmd -> Ok [ "Version ..." ]
    | SetTagsCmd args -> SetTags.handleCmd args
    | NoCmd -> Ok [ "Move along, nothing to see here..." ]
    |> Result.map (fun strings -> strings |> List.iter System.Console.WriteLine)
    |> Result.mapError (fun strings -> strings |> List.iter System.Console.WriteLine)

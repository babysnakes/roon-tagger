module RoonTagger.Cli.Commands.Main

open Argu
open RoonTagger.Cli
open RoonTagger.Cli.Arguments
open RoonTagger.Cli.Commands
open RoonTagger.Cli.Output

let (|VersionCmd|SetTagsCmd|EditTitlesCmd|ViewCmd|NoCmd|) (opts: ParseResults<MainArgs>) =
    if (opts.Contains Version) then
        VersionCmd
    elif (opts.Contains Set_Tags) then
        SetTagsCmd(opts.GetResult Set_Tags)
    elif (opts.Contains Edit_Titles) then
        EditTitlesCmd(opts.GetResult Edit_Titles)
    elif (opts.Contains View) then
        ViewCmd(opts.GetResult View)
    else
        NoCmd

let handleCmd (opts: ParseResults<MainArgs>) =
    match opts with
    | VersionCmd -> infoMessage $"{Info.Name}: {Info.Version}" |> Ok
    | SetTagsCmd args -> SetTags.handleCmd args
    | EditTitlesCmd args -> EditTitles.handleCmd args
    | ViewCmd args -> View.handleCmd args
    | NoCmd ->
        handleOutput "Move along, nothing to see here..."
        |> Ok

module RoonTagger.Cli.Help.LongHelp

open Spectre.Console
open System

let logger = Serilog.Log.Logger

let print (desc: string, uri: Uri) =
    AnsiConsole.MarkupLine desc
    AnsiConsole.WriteLine ""
    AnsiConsole.MarkupLine "Full description with example can be found in our reference documentation:"

    if AnsiConsole.Profile.Capabilities.Links then
        AnsiConsole.MarkupLine $"    [link={uri.AbsoluteUri}]{uri.AbsoluteUri}[/]"
    else
        logger.Information "Console does not support links"
        AnsiConsole.WriteLine $"    {uri.AbsoluteUri}"

let private mkUri (relative: string) : Uri =
    let rootUri = Uri("https://babysnakes.github.io/roon-tagger/")
    let processedRelative = $"{relative}.html"
    Uri(rootUri, processedRelative)

let editTitlesLH =
    ("Edit the titles of the Provided tracks in a text editor.", mkUri "commands/edit-titles")

let viewLH =
    ("View Roon specific metadata from file. Especially convenient for viewing personnel credits.",
     mkUri "commands/view")

let setTagsLH =
    ("Set roon specific tags. Completely replacing the contents of the tag unless specified otherwise.",
     mkUri "commands/set-tags")

let extractWorksLH =
    ("Extract (mostly classical music) work and movements from existing album metadata.", mkUri "commands/extract-works")

let mainLH =
    ("Roon Tagger - A tool for setting Roon specific metadata. Run with -h for usage.", mkUri "main")

let creditsLH = ("Add/Remove personnel credits for track.", mkUri "commands/credits")
let configureLH = ("Configure CLI behavior.", mkUri "commands/configure")

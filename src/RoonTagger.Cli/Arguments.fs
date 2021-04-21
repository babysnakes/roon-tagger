module RoonTagger.Cli.Arguments

open System
open Argu

type SetTagsArgs =
    | [<Unique>] Title of title: string
    | [<AltCommandLine("-I")>] Import_Date of date: string
    | [<AltCommandLine("-R")>] Release_Date of date: string
    | Year of int
    | [<MainCommand; ExactlyOnce; Last; Mandatory>] Files of file: string list

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Title _ -> "The title of the song."
            | Import_Date _ -> "The import date into roon (yyyy-mm-dd)."
            | Release_Date _ -> "The original release date of the album (yyyy-mm-dd)."
            | Year _ -> "The year the album was released (yyyy)."
            | Files _ -> "files to edit."

type EditTitlesArgs =
    | [<MainCommand; ExactlyOnce; Last; Mandatory>] Files of file: string list

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Files _ -> "Files to edit"

type ViewArgs =
    | Raw_Credits
    | [<MainCommand; ExactlyOnce; Mandatory>] File of file: string

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Raw_Credits -> "Print un-formatted credits (good for selecting credit for deletion)"
            | File _ -> "File to view"

type CreditsArgs =
    | Add of name: string * roles: string
    | Del of credit: string
    | [<MainCommand; ExactlyOnce; Last; Mandatory>] Files of file: string list

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Add _ ->
                "Adds the provided credit. A credit consists of a name and a comma separated list of roles. If required specify multiple times."
            | Del _ -> "Deletes the provided credit. Fails if credit does not exist (use multiple times if needed)"
            | Files _ -> "The files to apply the credits to"

type ConfigureArgs =
    | Log_File of logFile: string
    | Log_Level of Configuration.LogLevel
    | Editor of cmd: string
    | Editor_With_Args of cmd: string * args: string
    | Reset_Editor
    | Show

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Log_File _ -> "Customize output log file."
            | Log_Level _ -> "Customize default log level."
            | Editor _ ->
                "Configure an editor for editing titles file (executable that accepts the file as it's single argument). Editor should run in the foreground."
            | Editor_With_Args _ ->
                "Configure an editor for editing titles file (as a pair of executable and args-as-string). Editor must run in the foreground and must accept file to edit as last argument (example parameters: code '-n -w')."
            | Reset_Editor -> "Delete the editor configuration."
            | Show -> "Show current configuration and exit."

[<HelpFlags([| "-h"; "--help" |])>]
type MainArgs =
    | Version
    | [<AltCommandLine("-v"); Inherit>] Verbose
    | [<CliPrefix(CliPrefix.None)>] Set_Tags of ParseResults<SetTagsArgs>
    | [<CliPrefix(CliPrefix.None)>] Edit_Titles of ParseResults<EditTitlesArgs>
    | [<CliPrefix(CliPrefix.None)>] Credits of ParseResults<CreditsArgs>
    | [<CliPrefix(CliPrefix.None)>] Configure of ParseResults<ConfigureArgs>
    | [<CliPrefix(CliPrefix.None)>] View of ParseResults<ViewArgs>

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Version -> "print version and exit."
            | Verbose -> "Print some debug data (use multiple times for more verbosity)."
            | Set_Tags _ -> "set tags"
            | Edit_Titles _ -> "Edit the titles of the provided files as a text file"
            | Credits _ -> "Add/Delete credit entries"
            | Configure _ -> $"Configure {Info.Name}"
            | View _ -> "View metadata of the provided file"

let parseFiles files =
    match files with
    | "file" :: rest -> failwith "invalid file name"
    | _ -> files

let parseDate dateString =
    try
        DateTime.Parse dateString
    with _ -> failwith "Not a valid date format"

module RoonTagger.Cli.Arguments

open System
open Argu
open Models

type SetTagsArgs =
    // fsharplint:disable unionCasesNames
    | [<Unique>] Title of title: string
    | [<AltCommandLine("-I")>] Import_Date of date: string
    | [<AltCommandLine("-R")>] Release_Date of date: string
    | Year of int
    | [<MainCommand; ExactlyOnce; Last; Mandatory>] Files of file: string list
    // fsharplint:enable unionCasesNames

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Title _ -> "The title of the song."
            | Import_Date _ -> "The import date into roon (yyyy-mm-dd, also accepts 'today')."
            | Release_Date _ -> "The original release date of the album (yyyy-mm-dd)."
            | Year _ -> "The year the album was released (yyyy)."
            | Files _ -> "files to edit."

type EditTitlesArgs =
    | [<MainCommand; ExactlyOnce; Last; Mandatory>] Files of file: string list

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Files _ -> "Files to edit"

type ExtractWorksArgs =
    // fsharplint:disable unionCasesNames
    | [<AltCommandLine("-R")>] Add_Roman_Numerals
    | [<MainCommand; ExactlyOnce; Last; Mandatory>] Files of file: string list
    // fsharplint:enable unionCasesNames

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Add_Roman_Numerals ->
                "Starts each movement with it's corresponding roman numeral (e.g. movement 'Prelude' will become 'I. Prelude')."
            | Files _ -> "Files to extract works from"

type ViewArgs =
    // fsharplint:disable unionCasesNames
    | Raw_Credits
    | [<MainCommand; ExactlyOnce; Mandatory>] File of file: string
    // fsharplint:enable unionCasesNames

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Raw_Credits -> "Print un-formatted credits (good for selecting credit for deletion)"
            | File _ -> "File to view"

type CreditsArgs =
    // fsharplint:disable unionCasesNames
    | Add of name: string * roles: string
    | Del of credit: string
    | Skip_Validation
    | [<MainCommand; ExactlyOnce; Last; Mandatory>] Files of file: string list
    // fsharplint:enable unionCasesNames

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Add _ ->
                "Adds the provided credit. A credit consists of a name and a comma separated list of roles. If required specify multiple times."
            | Del _ -> "Deletes the provided credit. Fails if credit does not exist (use multiple times if needed)"
            | Skip_Validation ->
                "Do not perform role validation. Note that this might not show in Roon if the role is invalid."
            | Files _ -> "The files to apply the credits to"

type ConfigureArgs =
    // fsharplint:disable unionCasesNames
    | Log_File of logFile: string
    | Log_Level of Configuration.LogLevel
    | Editor of cmd: string
    | Editor_With_Args of cmd: string * args: string
    | Reset_Editor
    | Show
    // fsharplint:enable unionCasesNames

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Log_File _ -> "Customize output log file."
            | Log_Level _ -> "Customize default log level."
            | Editor _ ->
                "Configure an editor for editing tags file (executable that accepts the file as it's single argument). Editor should run in the foreground."
            | Editor_With_Args _ ->
                "Configure an editor for editing tags file as a pair of executable and args (separated by comma, surrounded with quotes). Editor must run in the foreground and must accept file to edit as last argument (example: code.com '-n,-w')."
            | Reset_Editor -> "Delete the editor configuration."
            | Show -> "Show current configuration and exit."

type CompletionsArgs =
    | [<MainCommand; ExactlyOnce>] Shell of SupportedShells

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Shell _ -> "The shell to generate completion for"

[<HelpFlags([| "-h"; "--help" |])>]
type MainArgs =
    // fsharplint:disable unionCasesNames
    | Version
    | [<AltCommandLine("-v"); Inherit>] Verbose
    | [<Inherit>] Long_Help
    | [<CliPrefix(CliPrefix.None)>] Set_Tags of ParseResults<SetTagsArgs>
    | [<CliPrefix(CliPrefix.None)>] Edit_Titles of ParseResults<EditTitlesArgs>
    | [<CliPrefix(CliPrefix.None)>] Credits of ParseResults<CreditsArgs>
    | [<CliPrefix(CliPrefix.None)>] Configure of ParseResults<ConfigureArgs>
    | [<CliPrefix(CliPrefix.None)>] View of ParseResults<ViewArgs>
    | [<CliPrefix(CliPrefix.None)>] Extract_Works of ParseResults<ExtractWorksArgs>
    | [<CliPrefix(CliPrefix.None)>] Completions of ParseResults<CompletionsArgs>
    // fsharplint:enable unionCasesNames

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Version -> "print version and exit."
            | Verbose -> "Print some debug data (use multiple times for more verbosity)."
            | Long_Help -> "Print detailed help message."
            | Set_Tags _ -> "set tags"
            | Edit_Titles _ -> "Edit the titles of the provided files as a text file"
            | Credits _ -> "Add/Delete credit entries"
            | Configure _ -> $"Configure {Info.Name}"
            | View _ -> "View metadata of the provided file"
            | Extract_Works _ -> "Try to identify and save work/movements from the provided files."
            | Completions _ -> "Generate tab completion script for supported shells."

let parseDate dateString =
    try
        DateTime.Parse dateString
    with
    | _ -> failwith "Not a valid date format"

let parseTodayOrDate =
    function
    | "today" -> DateTime.Today
    | other -> parseDate other

module RoonTagger.Cli.Arguments

open System
open Argu

type SetTagsArgs =
    | [<Unique>] Title of title: string
    | [<AltCommandLine("-I")>] Import_Date of date: string
    | [<AltCommandLine("-R")>] Release_Date of date: string
    | Year of int
    | [<MainCommand; ExactlyOnce; Last; Mandatory>] Files of file:string list

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Title _ -> "The title of the song."
            | Import_Date _ -> "The import date into roon (yyyy-mm-dd)."
            | Release_Date _ -> "The original release date of the album (yyyy-mm-dd)."
            | Year _ -> "The year the album was released (yyyy)."
            | Files _ -> "files to edit."

[<HelpFlags([|"-h"; "--help"|])>]
type MainArgs =
    | Version
    | [<CliPrefix(CliPrefix.None)>] Set_Tags of ParseResults<SetTagsArgs>

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Version -> "print version and exit."
            | Set_Tags _ -> "set tags"

let parseFiles files =
    match files with
    | "file" :: rest -> failwith "invalid file name"
    | _ -> files

let parseDate dateString =
    try DateTime.Parse dateString
    with
        | _ -> failwith "Not a valid date format"
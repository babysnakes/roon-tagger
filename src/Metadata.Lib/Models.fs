namespace RoonTagger.Metadata

open System
open FlacLibSharp

type TrackFormat = Flac of FlacFile

type Personnel = Personnel of String

type RoonTag =
    | Title of string
    | Work of string
    | Movement of string
    | Section of string
    | MovementIndex of int
    | MovementCount of int
    | ImportDate of DateTime
    | OriginalReleaseDate of DateTime
    | Year of int // Roon's "Released" field
    | Composer of string list
    | Credit of Personnel

type TagName =
    | TitleTag
    | AlbumTag
    | ArtistTag
    | WorkTag
    | MovementTag
    | SectionTag
    | MovementIndexTag
    | MovementCountTag
    | ImportDateTag
    | OriginalReleaseDateTag
    | YearTag
    | ComposerTag
    | CreditTag
    | TrackNumberTag
    | DiscNumberTag

type TagValue = private { Value: string list }

type TagsMap = Map<TagName, TagValue>

type AudioTrack =
    { Path: string
      Track: TrackFormat
      Original: TagsMap
      Current: TagsMap }

type MetadataErrors =
    | FileDoesNotExist of string
    | InvalidFileFormat of string
    | UnexpectedError of string
    | UnsupportedTagOperation of string
    | UnsupportedTagForFormat
    | DeletingNonExistingPersonnel of track: AudioTrack * value: string
    | FileSaveError of string
    | MissingOrInvalidTag of TagName
    | UnsupportedRole of string
    | DuplicateTrackNumberForDisc
    | NonConsecutiveTracks

module TagHelpers =

    let allTagNames =
        [ TitleTag
          AlbumTag
          ArtistTag
          WorkTag
          MovementTag
          SectionTag
          MovementIndexTag
          MovementCountTag
          ImportDateTag
          OriginalReleaseDateTag
          YearTag
          ComposerTag
          CreditTag
          TrackNumberTag
          DiscNumberTag ]

[<RequireQualifiedAccess>]
module TagValue =
    /// Checks whether the value is empty
    let isEmpty v : bool =
        match v.Value with
        | [] -> true
        | _ -> false

    /// Creates TagValue from string
    let ofString (s: string) : TagValue = { Value = [ s ] }

    /// Creates TagValue from list of strings
    let ofList (ss: string list) : TagValue = { Value = ss }

    /// Creates TagValue from int
    let ofInt (n: int) : TagValue = { Value = [ $"%02i{n}" ] }

    /// Creates TagValue from date
    let ofDate (date: DateTime) : TagValue =
        { Value = [ date.ToString("yyyy-MM-dd") ] }

    /// Extract value as string if exists (in tags that makes sense only as single value - e.g. title)
    let toString v : string option =
        match v.Value with
        | "" :: _ -> None
        | s :: _ -> Some s
        | _ -> None

    /// Extract value as list if any
    let toList v : string list option =
        if v.Value |> List.isEmpty then None else Some v.Value

    /// Extract value as int if any
    let toInt v : int option =
        match v.Value with
        | [] -> None
        | s :: _ ->
            match Int32.TryParse s with
            | true, n -> Some n
            | false, _ -> None

    /// Extract value as date
    let toDate v : DateTime option =
        match v.Value with
        | [] -> None
        | s :: _ ->
            match DateTime.TryParse s with
            | true, date -> Some date
            | false, _ -> None

[<RequireQualifiedAccess>]
module TagsMap =
    let private mergeSingleTag (tag: TagName) (orig: TagValue option) (update: TagValue option) : TagValue option =
        let joinListValue fst snd =
            let f = TagValue.toList fst |> Option.defaultValue []
            let s = TagValue.toList snd |> Option.defaultValue []
            List.append f s |> TagValue.ofList

        match orig, update with
        | None, None -> None
        | value, None -> value
        | None, value -> value
        | Some orig, Some up ->
            match tag with
            | TitleTag -> Some up
            | AlbumTag -> Some up
            | ArtistTag -> joinListValue up orig |> Some
            | WorkTag -> Some up
            | MovementTag -> Some up
            | SectionTag -> Some up
            | MovementIndexTag -> Some up
            | MovementCountTag -> Some up
            | ImportDateTag -> Some up
            | OriginalReleaseDateTag -> Some up
            | YearTag -> Some up
            | ComposerTag -> joinListValue up orig |> Some
            | CreditTag -> joinListValue up orig |> Some
            | TrackNumberTag -> Some up
            | DiscNumberTag -> Some up

    let merge (original: TagsMap) (updates: TagsMap) : TagsMap =
        let folder (state: TagsMap) (k: TagName) : TagsMap =
            state
            |> Map.change k (function
                | None -> Map.tryFind k updates
                | value -> mergeSingleTag k value (Map.tryFind k updates))

        updates |> Map.keys |> List.ofSeq |> List.fold folder original

    let deleteTag (key: TagName) (m: TagsMap) : TagsMap = m |> Map.remove key

    let deleteTagValue (key: TagName) value (m: TagsMap) : TagsMap =
        m
        |> Map.change key (fun tv ->
            tv
            |> Option.bind TagValue.toList
            |> Option.map (List.filter (fun item -> item <> value))
            |> Option.map TagValue.ofList)

module RoonTagger.Metadata.Formats.Flac

open FlacLibSharp
open RoonTagger.Metadata
open RoonTagger.Metadata.Utils

let OriginalReleaseDateTag = "ORIGINALRELEASEDATE"
let ImportDateTag = "IMPORTDATE"
let YearTag = "YEAR"
let WorkTag = "WORK"
let MovementTag = "PART"
let SectionTag = "SECTION"
let CreditTag = "PERSONNEL"

let setTag (track: FlacFile) (tag: RoonTag) =
    let comment = track.VorbisComment

    let replace key (value: string) =
        comment.Replace(key, VorbisCommentValues value)

    match tag with
    | Title title -> Ok(comment.Title <- VorbisCommentValues title)
    | Work work -> Ok(replace WorkTag work)
    | Movement mvmt -> Ok(replace MovementTag mvmt)
    | Section section -> Ok(replace SectionTag section)
    | ImportDate date -> Ok(replace ImportDateTag (formatDate date))
    | OriginalReleaseDate date -> Ok(replace OriginalReleaseDateTag (formatDate date))
    | Year year -> Ok(replace YearTag $"%d{year}")
    | MovementIndex _
    | MovementCount _ -> Error UnsupportedTagForFormat
    | Credit credit -> Error(UnsupportedTagOperation "Credit tag does not support *set* operation, only add/delete.")

let getTagStringValue (track: FlacFile) (tag: TagName) =
    let comment = track.VorbisComment

    match tag with
    | TagName.Title -> comment.Title
    | TagName.Work -> comment.[WorkTag]
    | TagName.Movement -> comment.[MovementTag]
    | TagName.Section -> comment.[SectionTag]
    | TagName.ImportDate -> comment.[ImportDateTag]
    | TagName.OriginalReleaseDate -> comment.[OriginalReleaseDateTag]
    | TagName.Year -> comment.[YearTag]
    | TagName.Credit -> comment.[CreditTag]
    | _ -> VorbisCommentValues()
    |> List.ofSeq

let applyChanges (track: FlacFile) =
    try track.Save() |> Ok
    with
        ex -> Error (FileSaveError ex.Message)
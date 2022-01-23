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
let DiskNumberTag = "DISCNUMBER"

let log = Serilog.Log.Logger

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
    | Credit _ -> Error(UnsupportedTagOperation "Credit tag does not support *set* operation, only add/delete.")

let setRaw (track: FlacFile) key (values: string list) =
    let comment = track.VorbisComment
    comment.Replace(key, VorbisCommentValues values)

let getTagStringValue (track: FlacFile) (tag: TagName) =
    let comment = track.VorbisComment

    match tag with
    | TitleTag -> comment.Title
    | AlbumTag -> comment.Album
    | ArtistTag -> comment.Artist
    | WorkTag -> comment[WorkTag]
    | MovementTag -> comment[MovementTag]
    | SectionTag -> comment[SectionTag]
    | ImportDateTag -> comment[ImportDateTag]
    | OriginalReleaseDateTag -> comment[OriginalReleaseDateTag]
    | YearTag -> comment[YearTag]
    | CreditTag -> comment[CreditTag]
    | TrackNumberTag -> comment.TrackNumber
    | DiscNumberTag -> comment[DiskNumberTag]
    | MovementIndexTag
    | MovementCountTag -> VorbisCommentValues()
    |> List.ofSeq

let applyChanges (track: FlacFile) =
    try
        track.Save() |> Ok
    with
    | ex ->
        log.Error("Saving track: {Ex}", ex)
        Error [ FileSaveError ex.Message ]

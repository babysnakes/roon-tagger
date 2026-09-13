module RoonTagger.Metadata.Formats.Flac

open FlacLibSharp
open RoonTagger.Metadata
open RoonTagger.Metadata.Utils

[<Literal>]
let AlbumTagName = "ALBUM"

[<Literal>]
let TitleTagName = "TITLE"

[<Literal>]
let ArtistTagName = "ARTIST"

[<Literal>]
let OriginalReleaseDateTagName = "ORIGINALRELEASEDATE"

[<Literal>]
let ImportDateTagName = "IMPORTDATE"

[<Literal>]
let YearTagName = "YEAR"

[<Literal>]
let WorkTagName = "WORK"

[<Literal>]
let MovementTagName = "PART"

[<Literal>]
let SectionTagName = "SECTION"

[<Literal>]
let CreditTagName = "PERSONNEL"

[<Literal>]
let ComposerTagName = "COMPOSER"

[<Literal>]
let DiscNumberTagName = "DISCNUMBER"

[<Literal>]
let TrackNumberTagName = "TRACKNUMBER"

let log = Serilog.Log.Logger

let private applyTag (file: FlacFile) (tag: TagName) (value: TagValue) : unit =
    let comment = file.VorbisComment

    let tagName =
        match tag with
        | TitleTag -> TitleTagName
        | AlbumTag -> AlbumTagName
        | ArtistTag -> ArtistTagName
        | WorkTag -> WorkTagName
        | MovementTag -> MovementTagName
        | SectionTag -> SectionTagName
        | ImportDateTag -> ImportDateTagName
        | OriginalReleaseDateTag -> OriginalReleaseDateTagName
        | YearTag -> YearTagName
        | ComposerTag -> ComposerTagName
        | CreditTag -> CreditTagName
        | TrackNumberTag -> TrackNumberTagName
        | DiscNumberTag -> DiscNumberTagName
        | UnHandled tag -> tag

    value
    |> TagValue.toList
    |> Option.map (fun v -> comment.Replace(tagName, v))
    |> ignore

let private extractTagValue (comment: VorbisComment) (tag: string) : TagName * TagValue =
    match tag.ToUpper() with
    | TitleTagName -> TitleTag, comment.Title
    | AlbumTagName -> AlbumTag, comment.Album
    | ArtistTagName -> ArtistTag, comment.Artist
    | WorkTagName -> WorkTag, comment[WorkTagName]
    | MovementTagName -> MovementTag, comment[MovementTagName]
    | SectionTagName -> SectionTag, comment[SectionTagName]
    | ImportDateTagName -> ImportDateTag, comment[ImportDateTagName]
    | OriginalReleaseDateTagName -> OriginalReleaseDateTag, comment[OriginalReleaseDateTagName]
    | YearTagName -> YearTag, comment[YearTagName]
    | CreditTagName -> CreditTag, comment[CreditTagName]
    | TrackNumberTagName -> TrackNumberTag, comment.TrackNumber
    | DiscNumberTagName -> DiscNumberTag, comment[DiscNumberTagName]
    | ComposerTagName -> ComposerTag, comment[ComposerTagName]
    | tag -> UnHandled tag, comment[tag]
    ||> fun t v -> (t, TagValue.ofSeq v)

let private loadTrackMetadata (track: FlacFile) : TagsMap =
    let comment = track.VorbisComment

    comment
    |> List.ofSeq
    |> List.map (_.Key >> extractTagValue comment)
    |> Map

let load (fileName: string) : Result<FlacFile * TagsMap, MetadataErrors> =
    try
        // fsharplint:disable-next-line redundantNewKeyword // it's IDisposable
        let track = new FlacFile(fileName)
        Ok(track, loadTrackMetadata track)
    with
    | :? System.IO.FileNotFoundException as err ->
        log.Error("Loading track: {Err}", err)
        Error(FileDoesNotExist err.Message)
    | :? Exceptions.FlacLibSharpInvalidFormatException as err ->
        log.Error("Loading track '{FileName}': {Err}", fileName, err)
        Error($"Not a valid FLAC file: '%s{fileName}'" |> InvalidFileFormat)
    | err ->
        log.Error("Loading track '{FileName}': {Err}", fileName, err)
        Error(UnexpectedError err.Message)

let validateTags (tags: TagsMap) : Result<TagsMap, MetadataErrors list> = Ok tags

let setTag (track: FlacFile) (tag: RoonTag) =
    let comment = track.VorbisComment

    let replace key (value: string) =
        comment.Replace(key, VorbisCommentValues value)

    match tag with
    | Title title -> Ok(comment.Title <- VorbisCommentValues title)
    | Work work -> Ok(replace WorkTagName work)
    | Movement mvmt -> Ok(replace MovementTagName mvmt)
    | Section section -> Ok(replace SectionTagName section)
    | ImportDate date -> Ok(replace ImportDateTagName (formatDate date))
    | OriginalReleaseDate date -> Ok(replace OriginalReleaseDateTagName (formatDate date))
    | Year year -> Ok(replace YearTagName $"%d{year}")
    | Composer composers -> Ok(comment.Replace(ComposerTagName, VorbisCommentValues composers))
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
    | WorkTag -> comment[WorkTagName]
    | MovementTag -> comment[MovementTagName]
    | SectionTag -> comment[SectionTagName]
    | ImportDateTag -> comment[ImportDateTagName]
    | OriginalReleaseDateTag -> comment[OriginalReleaseDateTagName]
    | YearTag -> comment[YearTagName]
    | CreditTag -> comment[CreditTagName]
    | TrackNumberTag -> comment.TrackNumber
    | DiscNumberTag -> comment[DiscNumberTagName]
    | ComposerTag -> comment[ComposerTagName]
    | UnHandled tag -> comment[tag]
    |> List.ofSeq

let applyTags (track: FlacFile) (original: TagsMap) (updates: TagsMap) : Result<unit, MetadataErrors list> = Ok(())

let saveChanges (track: FlacFile) =
    try
        track.Save() |> Ok
    with ex ->
        log.Error("Saving track: {Ex}", ex)
        Error [ FileSaveError ex.Message ]

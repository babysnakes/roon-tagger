module RoonTagger.Metadata.Formats.Flac

open FlacLibSharp
open RoonTagger.Metadata
open RoonTagger.Metadata.Utils

[<Literal>]
let OriginalReleaseDateTAG = "ORIGINALRELEASEDATE"

[<Literal>]
let ImportDateTAG = "IMPORTDATE"

[<Literal>]
let YearTAG = "YEAR"

[<Literal>]
let WorkTAG = "WORK"

[<Literal>]
let MovementTAG = "PART"

[<Literal>]
let SectionTAG = "SECTION"

[<Literal>]
let CreditTAG = "PERSONNEL"

[<Literal>]
let ComposerTAG = "COMPOSER"

[<Literal>]
let DiscNumberTAG = "DISCNUMBER"

let log = Serilog.Log.Logger

let private applyTag (file: FlacFile) (tag: TagName) (value: TagValue) : unit =
    let comment = file.VorbisComment

    match tag with
    | TitleTag ->
        value
        |> TagValue.toString
        |> Option.map (fun s -> comment.Title <- VorbisCommentValues s)
    | AlbumTag -> failwith "todo"
    | ArtistTag -> failwith "todo"
    | WorkTag -> failwith "todo"
    | MovementTag -> failwith "todo"
    | SectionTag -> failwith "todo"
    | ImportDateTag -> failwith "todo"
    | OriginalReleaseDateTag -> failwith "todo"
    | YearTag -> failwith "todo"
    | ComposerTag -> failwith "todo"
    | CreditTag -> failwith "todo"
    | TrackNumberTag -> failwith "todo"
    | DiscNumberTag -> failwith "todo"
    | UnHandled _ -> failwith "todo"
    |> ignore

let private extractTagValue (comment: VorbisComment) (tag: string) : TagName * TagValue =
    match tag.ToUpper() with
    | "TITLE" -> TitleTag, comment.Title
    | "ALBUM" -> AlbumTag, comment.Album
    | "ARTIST" -> ArtistTag, comment.Artist
    | WorkTAG -> WorkTag, comment[WorkTAG]
    | MovementTAG -> MovementTag, comment[MovementTAG]
    | SectionTAG -> SectionTag, comment[SectionTAG]
    | ImportDateTAG -> ImportDateTag, comment[ImportDateTAG]
    | OriginalReleaseDateTAG -> OriginalReleaseDateTag, comment[OriginalReleaseDateTAG]
    | YearTAG -> YearTag, comment[YearTAG]
    | CreditTAG -> CreditTag, comment[CreditTAG]
    | "TRACKNUMBER" -> TrackNumberTag, comment.TrackNumber
    | DiscNumberTAG -> DiscNumberTag, comment[DiscNumberTAG]
    | ComposerTAG -> ComposerTag, comment[ComposerTAG]
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
    | Work work -> Ok(replace WorkTAG work)
    | Movement mvmt -> Ok(replace MovementTAG mvmt)
    | Section section -> Ok(replace SectionTAG section)
    | ImportDate date -> Ok(replace ImportDateTAG (formatDate date))
    | OriginalReleaseDate date -> Ok(replace OriginalReleaseDateTAG (formatDate date))
    | Year year -> Ok(replace YearTAG $"%d{year}")
    | Composer composers -> Ok(comment.Replace(ComposerTAG, VorbisCommentValues composers))
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
    | WorkTag -> comment[WorkTAG]
    | MovementTag -> comment[MovementTAG]
    | SectionTag -> comment[SectionTAG]
    | ImportDateTag -> comment[ImportDateTAG]
    | OriginalReleaseDateTag -> comment[OriginalReleaseDateTAG]
    | YearTag -> comment[YearTAG]
    | CreditTag -> comment[CreditTAG]
    | TrackNumberTag -> comment.TrackNumber
    | DiscNumberTag -> comment[DiscNumberTAG]
    | ComposerTag -> comment[ComposerTAG]
    | UnHandled tag -> comment[tag]
    |> List.ofSeq

let applyTags (track: FlacFile) (original: TagsMap) (updates: TagsMap) : Result<unit, MetadataErrors list> = Ok(())

let saveChanges (track: FlacFile) =
    try
        track.Save() |> Ok
    with ex ->
        log.Error("Saving track: {Ex}", ex)
        Error [ FileSaveError ex.Message ]

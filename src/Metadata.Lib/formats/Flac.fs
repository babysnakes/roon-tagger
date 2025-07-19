module RoonTagger.Metadata.Formats.Flac

open FlacLibSharp
open RoonTagger.Metadata
open RoonTagger.Metadata.Utils

let originalReleaseDateTag = "ORIGINALRELEASEDATE"
let importDateTag = "IMPORTDATE"
let yearTag = "YEAR"
let workTag = "WORK"
let movementTag = "PART"
let sectionTag = "SECTION"
let creditTag = "PERSONNEL"
let composerTag = "COMPOSER"
let diskNumberTag = "DISCNUMBER"

let log = Serilog.Log.Logger

let load (fileName: string) : Result<FlacFile, MetadataErrors> =
    try
        // fsharplint:disable-next-line redundantNewKeyword // it's IDisposable
        new FlacFile(fileName) |> Ok
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

let setTag (track: FlacFile) (tag: RoonTag) =
    let comment = track.VorbisComment

    let replace key (value: string) =
        comment.Replace(key, VorbisCommentValues value)

    match tag with
    | Title title -> Ok(comment.Title <- VorbisCommentValues title)
    | Work work -> Ok(replace workTag work)
    | Movement mvmt -> Ok(replace movementTag mvmt)
    | Section section -> Ok(replace sectionTag section)
    | ImportDate date -> Ok(replace importDateTag (formatDate date))
    | OriginalReleaseDate date -> Ok(replace originalReleaseDateTag (formatDate date))
    | Year year -> Ok(replace yearTag $"%d{year}")
    | Composer composers -> Ok(comment.Replace(composerTag, VorbisCommentValues composers))
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
    | WorkTag -> comment[workTag]
    | MovementTag -> comment[movementTag]
    | SectionTag -> comment[sectionTag]
    | ImportDateTag -> comment[importDateTag]
    | OriginalReleaseDateTag -> comment[originalReleaseDateTag]
    | YearTag -> comment[yearTag]
    | CreditTag -> comment[creditTag]
    | TrackNumberTag -> comment.TrackNumber
    | DiscNumberTag -> comment[diskNumberTag]
    | MovementIndexTag
    | MovementCountTag -> VorbisCommentValues()
    | ComposerTag -> comment[composerTag]
    |> List.ofSeq

let applyChanges (track: FlacFile) =
    try
        track.Save() |> Ok
    with ex ->
        log.Error("Saving track: {Ex}", ex)
        Error [ FileSaveError ex.Message ]

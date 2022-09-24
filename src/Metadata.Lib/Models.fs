namespace RoonTagger.Metadata

open System
open FlacLibSharp

type TrackFormat = Flac of FlacFile

type AudioTrack = { Path: string; Track: TrackFormat }

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

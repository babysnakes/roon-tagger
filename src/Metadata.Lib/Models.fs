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
    | Credit of Personnel

type TagName =
    | Title = 0
    | Album = 1
    | Artist = 2
    | Work = 3
    | Movement = 4
    | Section = 5
    | MovementIndex = 6
    | MovementCount = 7
    | ImportDate = 8
    | OriginalReleaseDate = 9
    | Year = 10
    | Credit = 11
    | TrackNumber = 12

type MetadataErrors =
    | FileDoesNotExist of string
    | InvalidFileFormat of string
    | UnexpectedError of string
    | UnsupportedTagOperation of string
    | UnsupportedTagForFormat
    | DeletingNonExistingPersonnel of track: AudioTrack * value: string
    | FileSaveError of string

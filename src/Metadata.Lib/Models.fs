namespace RoonTagger.Metadata

open System
open FlacLibSharp

type TrackFormat = | Flac of FlacFile

type AudioTrack = { Path: string; Track: TrackFormat }

type Personnel =
    | Custom of string
    | Guitar of string
    | Cello of string
    | Violin of string
    | Viola of string
    | DoubleBass of string
    | ElectricBass of string
    | Orchestra of string
    // ...

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
    | Work = 1
    | Movement = 2
    | Section = 3
    | MovementIndex = 4
    | MovementCount = 5
    | ImportDate = 6
    | OriginalReleaseDate = 7
    | Year = 8
    | Credit = 9

type MetadataErrors =
    | FileDoesNotExist of string
    | InvalidFileFormat of string
    | UnexpectedError of string
    | UnsupportedTagOperation of string
    | UnsupportedTagForFormat

namespace RoonTagger.Metadata

open System
open FlacLibSharp

type TrackFormat = | Flac of FlacFile

type AudioTrack = { Path: string; Track: TrackFormat }

type Personnel =
    | Custom of string
    | Guitar of string
    | Cello of string
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

type MetadataErrors =
    | FileDoesNotExist of string
    | InvalidFileFormat of string
    | UnexpectedError of string
    | UnsupportedTagForFormat
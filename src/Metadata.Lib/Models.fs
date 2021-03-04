namespace RoonTagger.Metadata

open System

type Track = | Flac

type AudioTrack = { Path: string; Track: Track }

type RoonTags =
    | Title of string
    | Work of string
    | Movement of string
    | MovementIndex of int
    | MovementCount of int
    | ImportDate of DateTime
    | OriginalReleaseDate of DateTime
    | Year of int // Roon's "Released" field

type MetadataErrors =
    | UnsupportedFileFormat of string

namespace RoonTagger.Metadata

open ATL

type TrackFormat =
    | MP4 of Track
    | Flac of Track

type AudioTrack = { Path: string; Track: TrackFormat }

type RoonTags =
    | Title of string
    | Work of string
    | Movement of string
    | MovementIndex of int
    | MovementCount of int

type MetadataErrors =
    | UnsupportedFileFormat of string
    | FileDoesNotExist
    | MetadataError
    | UnsupportedTagPerFormat of string
    | UnknownError of string

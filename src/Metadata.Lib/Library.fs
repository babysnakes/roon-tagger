namespace RoonTagger.Metadata

open System.IO
open ATL

type TrackFormat =
    | MP4 of Track
    | Flac of Track

type AudioTrack = { Path: string; Track: TrackFormat }

type MetadataErrors =
    | UnsupportedFileFormat of string
    | FileDoesNotExist
    | MetadataError
    | UnknownError of string

module TrackOps =

    /// Validates that the metadata in the track is not the result of dummy
    /// metadata (in case *atldotnet* have error reading the metadata).
    let private validateMetadata (track: Track) =
        match (track.Duration, track.Bitrate) with
        | (0, 0) -> Error MetadataError
        | (_, _) -> Ok track

    /// Validates that the path exists and readable. *atldotnet* just prints
    /// exception and returns dummy metadata when file does not exist and we want
    /// to avoid this console clutter.
    let private validatePath path =
        match (File.Exists path) with
        | true -> Ok path
        | false -> Error FileDoesNotExist

    /// Loads the metadata from audio track.
    let load (path: string): Result<AudioTrack, MetadataErrors> =
        // IMPORTANT: This function is doing all kind of checks around
        // *atldotnet* tendency of not throwing exceptions but logging them.

        let mkAudioTrack (track: Track) =
            match track.AudioFormat with
            | f when f.IsValidExtension ".mp4" -> Ok { Path = path; Track = MP4 track }
            | f when f.IsValidExtension ".flac" -> Ok { Path = path; Track = Flac track }
            | f -> Error(UnsupportedFileFormat $"Unsupported format: {f.Name}")

        path
        |> validatePath
        |> Result.map Track
        |> Result.bind validateMetadata
        |> Result.bind mkAudioTrack

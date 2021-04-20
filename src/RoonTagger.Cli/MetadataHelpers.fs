module RoonTagger.Cli.MetadataHelpers

open FsToolkit.ErrorHandling
open RoonTagger.Cli.Models
open RoonTagger.Metadata

let extractTrackNumber (track: AudioTrack) : Result<int, CliErrors> =
    try
        Track.safeGetTagStringValue track TrackNumberTag
        |> List.head
        |> int
        |> Ok
    with :? System.FormatException -> Error(MissingOrInvalidTag TrackNumberTag |> MError)

let extractDiscNumberWithDefault (track: AudioTrack) : Result<int, CliErrors> =
    try
        Track.getTagStringValue track DiscNumberTag
        |> function
        | [] -> 0 |> Ok
        | "" :: tail -> 0 |> Ok
        | head :: tail -> head |> int |> Ok
    with :? System.FormatException -> Error(MissingOrInvalidTag DiscNumberTag |> MError)

let extractDiscAndTrackNumbers (track: AudioTrack) =
    result {
        let! dn = extractDiscNumberWithDefault track
        let! tn = extractTrackNumber track
        return (track, (dn, tn))
    }

let sortTracks (tracks: AudioTrack list) =
    tracks
    |> List.traverseResultA extractDiscAndTrackNumbers
    |> Result.map (List.sortBy snd)
    |> Result.map (List.map fst)

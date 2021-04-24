module RoonTagger.Metadata.TrackHelpers

open FsToolkit.ErrorHandling
open RoonTagger.Metadata

let extractTrackNumber (track: AudioTrack) : Result<int, MetadataErrors> =
    try
        Track.safeGetTagStringValue track TrackNumberTag
        |> List.head
        |> int
        |> Ok
    with :? System.FormatException -> MissingOrInvalidTag TrackNumberTag |> Error

let extractDiscNumberWithDefault (track: AudioTrack) : Result<int, MetadataErrors> =
    try
        Track.getTagStringValue track DiscNumberTag
        |> function
        | [] -> 0 |> Ok
        | "" :: tail -> 0 |> Ok
        | head :: tail -> head |> int |> Ok
    with :? System.FormatException -> MissingOrInvalidTag DiscNumberTag |> Error

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

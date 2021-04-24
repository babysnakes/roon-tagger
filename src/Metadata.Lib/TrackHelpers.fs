module RoonTagger.Metadata.TrackHelpers

open FsToolkit.ErrorHandling
open RoonTagger.Metadata
open RoonTagger.Metadata.Utils

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

let private extractDiscAndTrackNumbers (track: AudioTrack) =
    result {
        let! dn = extractDiscNumberWithDefault track
        let! tn = extractTrackNumber track
        return (track, (dn, tn))
    }

let private validateDuplicateTrackNumber (tracksWithDnTn: (AudioTrack * (int * int)) list) =
    let ts = tracksWithDnTn |> List.map snd
    let uniqueLength = ts |> List.distinct |> List.length

    if (ts |> List.length) = uniqueLength then
        Ok tracksWithDnTn
    else
        Error [ DuplicateTrackNumberForDisc ]

let sortTracks (tracks: AudioTrack list) =
    tracks
    |> List.traverseResultA extractDiscAndTrackNumbers
    |> Result.bind validateDuplicateTrackNumber
    |> Result.map (List.sortBy snd)
    |> Result.map (List.map fst)

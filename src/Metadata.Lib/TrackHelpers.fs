module RoonTagger.Metadata.TrackHelpers

open FsToolkit.ErrorHandling
open RoonTagger.Metadata
open RoonTagger.Metadata.Utils

let log = Serilog.Log.Logger

let extractTrackNumber (track: AudioTrack) : Result<int, MetadataErrors> =
    try
        Track.safeGetTagStringValue track TrackNumberTag
        |> List.head
        |> int
        |> Ok
    with :? System.FormatException ->
        MissingOrInvalidTag TrackNumberTag |> Error

let extractDiscNumberWithDefault (track: AudioTrack) : Result<int, MetadataErrors> =
    try
        Track.getTagStringValue track DiscNumberTag
        |> function
            | [] -> 0 |> Ok
            | "" :: _tail -> 0 |> Ok
            | head :: _tail -> head |> int |> Ok
    with :? System.FormatException ->
        MissingOrInvalidTag DiscNumberTag |> Error

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

/// Consecutive Audio Tracks - ordered by disc number and track number without gaps.
/// Does not guarantee that tracks are not missing at the edges.
type ConsecutiveTracks =
    | ConsecutiveTracks of AudioTrack list

    /// Sorts the tracks and guarantees that it produces legal ConsecutiveTracks.
    static member Create(tracks: AudioTrack list) : Result<ConsecutiveTracks, MetadataErrors list> =
        let consecutive (t: int * int, tNext: int * int) =
            match t, tNext with
            | (discNum, trackNum), (discNum', trackNum') when discNum = discNum' && trackNum' - trackNum = 1 -> true
            | (discNum, _), (discNum', trackNum) when trackNum = 1 && discNum' - discNum = 1 -> true
            | _ -> false

        result {
            log.Information("Creating consecutive tracks from {Tracks}", tracks)

            let! sorted = tracks |> sortTracks
            log.Debug("Consecutive tracks: after sorting: {Sorted}", sorted)

            let! tnDns =
                sorted
                |> List.traverseResultA extractDiscAndTrackNumbers
                |> Result.map (List.map snd)

            log.Debug("disc number * track number of requested tracks: {TnDns}", tnDns)
            let pairs = tnDns |> List.windowed 2 |> List.map (fun lst -> (lst[0], lst[1]))
            log.Debug("Pairs for consecutive tracks: {Pairs}", pairs)
            let isConsecutive = pairs |> List.map consecutive |> List.forall id

            return!
                if isConsecutive then
                    log.Debug("Tracks are consecutive")
                    ConsecutiveTracks sorted |> Ok
                else
                    log.Debug("Tracks are NOT consecutive")
                    Error [ NonConsecutiveTracks ]
        }

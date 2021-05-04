namespace Metadata.Test

open NUnit.Framework
open FsUnit
open RoonTagger.Metadata
open RoonTagger.Metadata.TrackHelpers
open RoonTagger.Metadata.WorkMovement
open TestsUtils

module ``Work and Movements Tests`` =

    let loadTrackSuccess = getResourcePath >> Track.load >> Result.unwrap

    let mkTrackWithTitle (file: string) (title: string) =
        let track = loadTrackSuccess file
        Track.setTag track (Title title) |> Result.unwrap

    let mkDefaultTrackWithTitle = mkTrackWithTitle "empty.flac"

    let extractMovement (track: AudioTrack) =
        Track.safeGetTagStringValue track MovementTag |> List.head

    [<Test>]
    let ``workFromTitle should extract work`` () =
        mkDefaultTrackWithTitle "A title : movement"
        |> workFromTitle
        |> Option.get
        |> should equal "A title"

    [<Test>]
    let ``workFromTitle should return None if title can not be split`` () =
        mkDefaultTrackWithTitle "A title without colon"
        |> workFromTitle
        |> Option.isNone
        |> should be True

    [<Test>]
    let ``Work.Create should select work in trivial cases`` () =
        let data =
            [ mkTrackWithTitle "track1.flac" "Some Track Title"
              mkTrackWithTitle "track2.flac" "My work name: First movement"
              mkTrackWithTitle "track3.flac" "My work name: Second movement"
              mkTrackWithTitle "track4.flac" "My work name: Third movement" ]

        let rslt = ConsecutiveTracks.Create data |> Result.bind extractWorks |> Result.unwrap

        rslt |> should haveLength 1
        let (Work (w, ts)) = rslt |> List.head
        w |> should equal "My work name"
        ts |> should haveLength 3
        ts |> List.head |> extractMovement |> should equal "First movement"

    [<Test>]
    let ``Should process same work spread across multiple discs as one work`` () =
        let workTitle = "My work title"

        let data =
            [ mkTrackWithTitle "disc1track1.flac" "Some title"
              mkTrackWithTitle "disc1track2.flac" $"{workTitle}: First movement"
              mkTrackWithTitle "disc1track3.flac" $"{workTitle}: Second movement"
              mkTrackWithTitle "disc2track1.flac" $"{workTitle}: Third movement"
              mkTrackWithTitle "disc2track2.flac" $"{workTitle}: Fourth movement" ]

        let rslt = ConsecutiveTracks.Create data |> Result.bind extractWorks |> Result.unwrap

        rslt |> should haveLength 1
        let (Work (w, ts)) = rslt |> List.head
        w |> should equal workTitle
        ts |> should haveLength 4

    [<Test>]
    let ``Work that is separated by non work track should be treated as two works even with the same work title`` () =
        let workTitle = "My work title"

        let data =
            [ mkTrackWithTitle "disc1track1.flac" $"{workTitle}: First movement"
              mkTrackWithTitle "disc1track2.flac" $"{workTitle}: Second movement"
              mkTrackWithTitle "disc1track3.flac" "Some title"
              mkTrackWithTitle "disc2track1.flac" $"{workTitle}: Third movement"
              mkTrackWithTitle "disc2track2.flac" $"{workTitle}: Fourth movement" ]

        let rslt = ConsecutiveTracks.Create data |> Result.bind extractWorks |> Result.unwrap

        rslt |> should haveLength 2
        let (Work (w1, ts1)) = rslt.[0]
        let (Work (w2, ts2)) = rslt.[1]
        w1 |> should equal workTitle
        w2 |> should equal workTitle
        ts1 |> should haveLength 2
        ts2 |> should haveLength 2

    [<Test>]
    let ``Work.Create should parse movement names with consistent num prefix`` () =
        let workTitle = "My work title"

        let data =
            [ mkTrackWithTitle "disc1track1.flac" $"{workTitle}: I. First movement"
              mkTrackWithTitle "disc1track2.flac" $"{workTitle}: II. Second movement" ]

        let (Work (w, ts)) =
            ConsecutiveTracks.Create data
            |> Result.bind (Work.Create workTitle)
            |> Result.unwrap

        w |> should equal workTitle
        ts.[0] |> extractMovement |> should equal "First movement"
        ts.[1] |> extractMovement |> should equal "Second movement"

    [<Test>]
    let ``Work.Create should not extract num prefix from title if layout is not consistent`` () =
        let workTitle = "My work title"

        let data =
            [ mkTrackWithTitle "disc1track1.flac" $"{workTitle}: No 1. First movement"
              mkTrackWithTitle "disc1track2.flac" $"{workTitle}: Second movement" ]

        let (Work (w, ts)) =
            ConsecutiveTracks.Create data
            |> Result.bind (Work.Create workTitle)
            |> Result.unwrap

        w |> should equal workTitle
        ts.[0] |> extractMovement |> should equal "No 1. First movement"
        ts.[1] |> extractMovement |> should equal "Second movement"

namespace RoonTagger.Cli.Test

open RoonTagger.Metadata
open RoonTagger.Metadata.TrackHelpers
open TestsUtils
open NUnit.Framework
open FsUnit

module MetadataHelpersTests =

    let loadTrackSuccess = getResourcePath >> Track.load >> Result.unwrap
    let createOrderedTracks = List.map loadTrackSuccess >> OrderedTracks.Create

    [<Test>]
    let ``sort tracks by tracks and cd number`` () =
        let before =
            [ "disc1track3.flac"
              "disc2track2.flac"
              "disc1track2.flac"
              "disc2track1.flac"
              "disc1track1.flac" ]

        let expected =
            [ "disc1track1.flac"
              "disc1track2.flac"
              "disc1track3.flac"
              "disc2track1.flac"
              "disc2track2.flac" ]
            |> List.map loadTrackSuccess
            |> List.map (fun i -> i.Path)

        before
        |> List.map loadTrackSuccess
        |> sortTracks
        |> Result.unwrap
        |> List.map (fun i -> i.Path)
        |> should equal expected

    [<Test>]
    let ``sort tracks by tracks and cd number if cd is empty`` () =
        let before =
            [ "track3.flac"
              "track1.flac"
              "track2.flac" ]

        let expected =
            [ "track1.flac"
              "track2.flac"
              "track3.flac" ]
            |> List.map loadTrackSuccess
            |> List.map (fun i -> i.Path)

        before
        |> List.map loadTrackSuccess
        |> sortTracks
        |> Result.unwrap
        |> List.map (fun i -> i.Path)
        |> should equal expected

    [<Test>]
    let ``sort tracks fails if there are duplicate track and disc numbers`` () =
        let tracks =
            [ "track2.flac"
              "another-track1.flac"
              "track1.flac" ]

        tracks
        |> List.map loadTrackSuccess
        |> sortTracks
        |> Result.unwrapError
        |> should contain DuplicateTrackNumberForDisc

    [<Test>]
    let ``Ordered tracks when missing file in a sequence should fail`` () =
        let result = [ "track1.flac"; "track3.flac" ] |> createOrderedTracks
        result |> Result.unwrapError |> should equal [ UnOrderedTracks ]

    [<Test>]
    let ``Ordered tracks fails on duplicate track numbers`` () =
        let result = [ "track1.flac"; "track1.flac" ] |> createOrderedTracks

        result
        |> Result.unwrapError
        |> should equal [ DuplicateTrackNumberForDisc ]

    [<Test>]
    let ``Ordered tracks when missing missing first file on second disc should fail`` () =
        let result =
            [ "disc1track1.flac"
              "disc1track2.flac"
              "disc1track3.flac"
              "disc2track2.flac" ]
            |> createOrderedTracks

        result |> Result.unwrapError |> should equal [ UnOrderedTracks ]

    [<Test>]
    let ``Ordered tracks when provided single track is valid`` () =
        let result = [ "track3.flac" ] |> createOrderedTracks
        result |> Result.unwrap |> should be (ofCase <@ OrderedTracks @>)

    let ``Ordered tracks sorts the provided tracks`` () =
        let result =
            [ "disc1track2.flac"
              "disc1track1.flac"
              "disc1track3.flac" ]
            |> createOrderedTracks
            |> Result.unwrap

        let (OrderedTracks tracks) = result
        tracks.[0].Path |> should endWith "disc1track1.flac"
        tracks.[1].Path |> should endWith "disc1track2.flac"
        tracks.[2].Path |> should endWith "disc1track3.flac"

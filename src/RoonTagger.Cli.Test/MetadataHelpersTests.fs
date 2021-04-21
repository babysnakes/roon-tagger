namespace RoonTagger.Cli.Test

open RoonTagger.Cli.MetadataHelpers
open RoonTagger.Metadata
open TestsUtils
open NUnit.Framework
open FsUnit

module MetadataHelpersTests =

    let loadTrackSuccess = getResourcePath >> Track.load >> Result.unwrap

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

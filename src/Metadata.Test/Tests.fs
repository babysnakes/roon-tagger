module Tests

open NUnit.Framework
open FsUnit
open RoonTagger.Metadata
open TestsUtils

module ``Loading Tracks`` =

    let loadTrack =
        getResourcePath
        >> TrackOps.load
        >> Result.map (fun t -> t.Track)

    [<Test>]
    let ``Loading unsupported Format should return unsupported file format error`` () =
        "empty.mp3"
        |> loadTrack
        |> Result.unwrapError
        |> should be (ofCase <@ UnsupportedFileFormat @>)

    [<Test>]
    let ``Loading m4a file should result in MP4 track`` () =
        "empty.m4a"
        |> loadTrack
        |> Result.unwrap
        |> should be (ofCase <@ MP4 @>)

    [<Test>]
    let ``Loading flac file should result in flac track`` () =
        "empty.flac"
        |> loadTrack
        |> Result.unwrap
        |> should be (ofCase<@ Flac @>)

    [<Test>]
    let ``Loading invalid File (not audio format) returns metadata error`` () =
        "non-audio.flac"
        |> loadTrack
        |> Result.unwrapError
        |> should be (ofCase<@ MetadataError @>)

    [<Test>]
    let ``Loading non-existing file returns file not found error`` () =
        "no-such-file.flac"
        |> loadTrack
        |> Result.unwrapError
        |> should be (ofCase <@ FileDoesNotExist @>)


module ``Apply tags tests`` =

    let loadTrack =
        getResourcePath >> TrackOps.load >> Result.unwrap

    [<Test>]
    let ``apply title on m4a file should succeed`` () =
        let test fileName =
            let track = loadTrack fileName

            TrackOps.applyTags track [ Title "my title" ]
            |> ignore

            (TrackOps.getTrack track).Title
            |> should equal "my title"

        [ "empty.m4a"; "empty.flac" ] |> List.iter test

module Tests

open NUnit.Framework
open RoonTagger.Metadata
open TestsUtils

module ``Loading Tracks`` =

    let testLoadTrack fileName testF =
        fileName
        |> getResourcePath
        |> TrackOps.load
        |> Result.map (fun t -> t.Track)
        |> testF

    [<Test>]
    let ``Loading unsupported Format should return unsupported file format error`` () =
        function
        | Error (UnsupportedFileFormat _) -> Assert.Pass "Ok"
        | Error err -> Assert.Fail $"Result: Error {err}"
        | Ok t -> Assert.Fail $"Result: Ok: {t}"
        |> testLoadTrack "empty.mp3"

    [<Test>]
    let ``Loading m4a file should result in MP4 track`` () =
        function
        | Ok (MP4 _) -> Assert.Pass "Ok"
        | Ok t -> Assert.Fail $"Result wrong track type: {t}"
        | Error err -> Assert.Fail $"Result: Error: {err}"
        |> testLoadTrack "empty.m4a"

    [<Test>]
    let ``Loading flac file should result in flac track`` () =
        function
        | Ok (Flac _) -> Assert.Pass "Ok"
        | Ok t -> Assert.Fail $"Result: Ok: {t}"
        | Error err -> Assert.Fail "Result: Error {err}"
        |> testLoadTrack "empty.flac"

    [<Test>]
    let ``Loading invalid File (not audio format) returns metadata error`` () =
        function
        | Error MetadataError -> Assert.Pass "Ok"
        | Error err -> Assert.Fail $"Result: Error {err}"
        | Ok _ -> Assert.Fail "Result: track"
        |> testLoadTrack "non-audio.flac"

    [<Test>]
    let ``Loading non-existing file returns file not found error`` () =
        function
        | Error FileDoesNotExist -> Assert.Pass "Ok"
        | Error err -> Assert.Fail $"Result: Error {err}"
        | Ok _ -> Assert.Fail "Result: track"
        |> testLoadTrack "no-such-file.flac"

module Tests

open System
open NUnit.Framework
open FsUnit
open RoonTagger.Metadata
open RoonTagger.Metadata.Formats
open TestsUtils

module ``Track operations`` =

    let loadTrackSuccess =
        getResourcePath >> Track.load >> Result.unwrap

    let loadTrackError =
        getResourcePath
        >> Track.load
        >> Result.unwrapError

    [<Test>]
    let ``Loading missing file return FileDoesNotExist error`` () =
        "does-not-exist.flac"
        |> Track.load
        |> Result.unwrapError
        |> should be (ofCase <@ FileDoesNotExist @>)

    [<Test>]
    let ``Loading invalid type returns InvalidFileFormat error`` () =
        "not-flac.flac"
        |> loadTrackError
        |> should be (ofCase <@ InvalidFileFormat @>)

    [<Test>]
    let ``Setting dates should result in correct format`` () =
        let sampleDate = DateTime(2021, 5, 21)
        let track = "empty.flac" |> loadTrackSuccess
        let import = ImportDate sampleDate
        let originalRelease = OriginalReleaseDate sampleDate

        Track.setTags track [ import; originalRelease ]
        |> Result.unwrap
        |> ignore

        // TODO: should be replaced with general get value instead of flac specific
        let flac = track |> extractFlac

        flac.VorbisComment.[Flac.ImportDateTag].[0]
        |> should equal "2021-05-21"

        flac.VorbisComment.[Flac.OriginalReleaseDateTag].[0]
        |> should equal "2021-05-21"

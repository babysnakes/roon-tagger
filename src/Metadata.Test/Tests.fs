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

        (Track.getTagStringValue track TagName.ImportDate).[0]
        |> should equal "2021-05-21"

        (Track.getTagStringValue track TagName.OriginalReleaseDate).[0]
        |> should equal "2021-05-21"

    [<Test>]
    let ``Setting year should be parsed correctly`` () =
        let track = "empty.flac" |> loadTrackSuccess

        Track.setTags track [ Year 2012 ]
        |> Result.unwrap
        |> ignore

        (Track.getTagStringValue track TagName.Year).[0]
        |> should equal "2012"

    [<Test>]
    let ``Setting credits is forbidden`` () =
        let track = "empty.flac" |> loadTrackSuccess

        Track.setTag track (Credit(Custom "custom - Flute"))
        |> Result.unwrapError
        |> should be (ofCase <@ UnsupportedTagOperation @>)

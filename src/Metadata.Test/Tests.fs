module Tests

open System
open NUnit.Framework
open FsUnit
open RoonTagger.Metadata
open RoonTagger.Metadata.Formats
open TestsUtils

module ``Track operations`` =

    let loadTrackSuccess = getResourcePath >> Track.load >> Result.unwrap
    let loadTrackError = getResourcePath >> Track.load >> Result.unwrapError

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
        Track.setTags track [ import; originalRelease ] |> ignore

        (Track.getTagStringValue track ImportDateTag).[0]
        |> should equal "2021-05-21"

        (Track.getTagStringValue track OriginalReleaseDateTag).[0]
        |> should equal "2021-05-21"

    [<Test>]
    let ``Setting year should be parsed correctly`` () =
        let track = "empty.flac" |> loadTrackSuccess

        Track.setTags track [ Year 2012 ] |> ignore

        (Track.getTagStringValue track YearTag).[0]
        |> should equal "2012"

    [<Test>]
    let ``Setting credits is forbidden`` () =
        let track = "empty.flac" |> loadTrackSuccess

        Track.setTag track (Credit(Personnel "custom - Flute"))
        |> Result.unwrapError
        |> should be (ofCase <@ UnsupportedTagOperation @>)

    [<Test>]
    let ``getTagStringValue returns empty list on nonexisting tags`` () =
        let track = "empty.flac" |> loadTrackSuccess
        Track.getTagStringValue track MovementTag |> should haveLength 0

    [<Test>]
    let ``safeGetTagStringValue will return at least one empty element in nonexisting tags`` () =
        let track = "empty.flac" |> loadTrackSuccess

        Track.safeGetTagStringValue track MovementTag
        |> should equal [ "" ]

    [<Test>]
    let ``mkPersonnel should correctly convert Personnel to strings`` () =
        let testData =
            [ ("Musician A", [ "Guitar"; "Voice" ])
              ("Musician B", [ "Violin" ]) ]

        let res = Track.mkPersonnel testData |> Result.unwrap
        res |> should haveLength 3
        res |> should contain (Personnel "Musician A - Guitar")
        res |> should contain (Personnel "Musician A - Voice")
        res |> should contain (Personnel "Musician B - Violin")

    [<Test>]
    let ``addCredits should append provided credits to existing ones`` () =
        // sample track contains 3 credit entries.
        let track = "with-credits.flac" |> loadTrackSuccess

        Track.addCredits
            track
            [ Personnel "First Last - Cello"
              Personnel "The Orchestra - Orchestra" ]
        |> ignore

        let p = Track.getTagStringValue track CreditTag
        p |> should contain "First Last - Cello"
        p |> should contain "The Orchestra - Orchestra"
        p |> should haveLength 5

    [<Test>]
    let ``addCredits does not produce duplicate entries`` () =
        let track = "with-credits.flac" |> loadTrackSuccess
        let duplicate = Personnel "Musician A - Guitar"

        Track.addCredits
            track
            [ duplicate
              Personnel "Musician C - Double Bass" ]
        |> ignore

        let p = Track.getTagStringValue track CreditTag
        p |> should haveLength 4
        p |> should contain "Musician A - Guitar"
        p |> should contain "Musician C - Double Bass"

    [<Test>]
    let ``delCredit deletes requested credits`` () =
        let track = "with-credits.flac" |> loadTrackSuccess
        let existingValue1 = "Musician A - Guitar"
        let existingValue2 = "Musician B - Vocals"

        Track.deleteCredits
            track
            [ Personnel existingValue1
              Personnel existingValue2 ]
        |> ignore

        Track.getTagStringValue track CreditTag
        |> should equal [ "Musician B - Viola" ]





    [<Test>]
    let ``delCredit fails with errors if provided credits does not exist`` () =
        let track = "with-credits.flac" |> loadTrackSuccess
        let existingValue = "Musician A - Guitar"
        let nonExistingValue1 = "Non-existing Value - Vocals"
        let nonExistingValue2 = "Non-existing Value - Guitar"

        let resultErrors =
            Track.deleteCredits
                track
                [ Personnel existingValue
                  Personnel nonExistingValue1
                  Personnel nonExistingValue2 ]
            |> Result.unwrapError

        resultErrors |> should haveLength 2

        resultErrors.[0]
        |> should be (ofCase <@ DeletingNonExistingPersonnel @>)

        resultErrors.[1]
        |> should be (ofCase <@ DeletingNonExistingPersonnel @>)

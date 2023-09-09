module Metadata.Test.TagsHandlingTests

open System
open NUnit.Framework
open RoonTagger.Metadata
open FsUnit

module ``TagsMap Manipulation`` =
    type MergeTestData =
        { Description: string
          Original: TagsMap
          Updates: TagsMap
          Expected: TagsMap }

    type DeleteTagsValueData =
        { Description: string
          Original: TagsMap
          Expected: TagsMap
          Key: TagName
          Value: string }

    let mergeTestsData () =
        [ { Description = "title should be replaced"
            Original = Map [ (TitleTag, TagValue.ofString "old title") ]
            Updates = Map [ (TitleTag, TagValue.ofString "new title") ]
            Expected = Map [ (TitleTag, TagValue.ofString "new title") ] }
          { Description = "tags not in updates should remain as they are"
            Original =
              Map
                  [ (TitleTag, TagValue.ofString "old title")
                    (AlbumTag, TagValue.ofString "My Album") ]
            Updates = Map [ (TitleTag, TagValue.ofString "new title") ]
            Expected =
              Map
                  [ (TitleTag, TagValue.ofString "new title")
                    (AlbumTag, TagValue.ofString "My Album") ] }
          { Description = "track and disc number should update"
            Original = Map [ (TrackNumberTag, TagValue.ofInt 1); (DiscNumberTag, TagValue.ofInt 1) ]
            Updates = Map [ (TrackNumberTag, TagValue.ofInt 3); (DiscNumberTag, TagValue.ofInt 2) ]
            Expected = Map [ (TrackNumberTag, TagValue.ofInt 3); (DiscNumberTag, TagValue.ofInt 2) ] }
          { Description = "artists should be merged"
            Original = Map [ (ArtistTag, TagValue.ofList [ "John Coltrane"; "Archie Shepp" ]) ]
            Updates = Map [ (ArtistTag, TagValue.ofString "Miles Davis") ]
            Expected = Map [ (ArtistTag, TagValue.ofList [ "Miles Davis"; "John Coltrane"; "Archie Shepp" ]) ] }
          { Description = "work/movement tags should be overwritten"
            Original =
              Map
                  [ (WorkTag, TagValue.ofString "original work")
                    (MovementTag, TagValue.ofString "original movement")
                    (SectionTag, TagValue.ofString "original section")
                    (MovementIndexTag, TagValue.ofInt 1)
                    (MovementCountTag, TagValue.ofInt 1) ]
            Updates =
              Map
                  [ (WorkTag, TagValue.ofString "new work")
                    (MovementTag, TagValue.ofString "new movement")
                    (SectionTag, TagValue.ofString "new section")
                    (MovementIndexTag, TagValue.ofInt 2)
                    (MovementCountTag, TagValue.ofInt 2) ]
            Expected =
              Map
                  [ (WorkTag, TagValue.ofString "new work")
                    (MovementTag, TagValue.ofString "new movement")
                    (SectionTag, TagValue.ofString "new section")
                    (MovementIndexTag, TagValue.ofInt 2)
                    (MovementCountTag, TagValue.ofInt 2) ] }
          { Description = "more tags to be replaced"
            Original =
              Map
                  [ (ImportDateTag, TagValue.ofDate (DateTime(2022, 2, 2)))
                    (OriginalReleaseDateTag, TagValue.ofDate (DateTime(2022, 2, 2)))
                    (YearTag, TagValue.ofInt 2023) ]
            Updates =
              Map
                  [ (ImportDateTag, TagValue.ofDate (DateTime(2023, 2, 2)))
                    (OriginalReleaseDateTag, TagValue.ofDate (DateTime(2023, 2, 2)))
                    (YearTag, TagValue.ofInt 2022) ]
            Expected =
              Map
                  [ (ImportDateTag, TagValue.ofDate (DateTime(2023, 2, 2)))
                    (OriginalReleaseDateTag, TagValue.ofDate (DateTime(2023, 2, 2)))
                    (YearTag, TagValue.ofInt 2022) ] }
          { Description = "composer and credit should be merged"
            Original =
              Map
                  [ (ComposerTag, TagValue.ofList [ "Bach" ])
                    (CreditTag, TagValue.ofList [ "Alicia Olatuja - Voice"; "Dan Tepfer - Piano" ]) ]
            Updates =
              Map
                  [ (ComposerTag, TagValue.ofList [ "Beethoven" ])
                    (CreditTag, TagValue.ofList [ "Sean Smith - Bass" ]) ]
            Expected =
              Map
                  [ (ComposerTag, TagValue.ofList [ "Beethoven"; "Bach" ])
                    (CreditTag, TagValue.ofList [ "Sean Smith - Bass"; "Alicia Olatuja - Voice"; "Dan Tepfer - Piano" ]) ] } ]

    let deleteTagsValueData () =
        [ { Description = "invalid tagName should be ignored"
            Original = Map [ (ArtistTag, TagValue.ofString "Some Artist") ]
            Expected = Map [ (ArtistTag, TagValue.ofString "Some Artist") ]
            Key = CreditTag
            Value = "some credit" }
          { Description = "non-existing value should be ignored"
            Original = Map [ (ArtistTag, TagValue.ofString "Some Artist") ]
            Expected = Map [ (ArtistTag, TagValue.ofString "Some Artist") ]
            Key = ArtistTag
            Value = "Other Artist" }
          { Description = "all occurrences of value should be removed"
            Original = Map [ (ArtistTag, TagValue.ofList [ "Another Artist"; "Some Artist"; "Another Artist" ]) ]
            Expected = Map [ (ArtistTag, TagValue.ofString "Some Artist") ]
            Key = ArtistTag
            Value = "Another Artist" } ]

    [<TestCaseSource(nameof mergeTestsData)>]
    let ``merge two tags-map should yield correct results`` (testData: MergeTestData) =
        let result = TagsMap.merge testData.Original testData.Updates
        Assert.AreEqual(testData.Expected, result, testData.Description)

    [<TestCaseSource(nameof deleteTagsValueData)>]
    let ``deleting tags value should handle correctly`` (testData: DeleteTagsValueData) =
        let result = TagsMap.deleteTagValue testData.Key testData.Value testData.Original
        Assert.AreEqual(testData.Expected, result, testData.Description)

    [<Test>]
    let ``remove tag with tag that does not exist in the TagsMap should return the same TagsMap`` () =
        let tags = Map [ (ArtistTag, TagValue.ofList [ "Artist 1"; "artist 2" ]) ]
        tags |> TagsMap.deleteTag AlbumTag |> should equal tags

module ``TagNamesHelpers`` =
    open Microsoft.FSharp.Reflection

    /// Get a seq of all DU cases.
    let GetAllUnionCases<'T> () =
        FSharpType.GetUnionCases(typeof<'T>)
        |> Seq.map (fun x -> FSharpValue.MakeUnion(x, Array.zeroCreate (x.GetFields().Length)) :?> 'T)

    [<Test>]
    let ``validate allTagNames is in sync with TagName cases`` () =
        let expected = GetAllUnionCases<TagName>() |> Set.ofSeq
        TagHelpers.allTagNames |> Set.ofSeq |> should equal expected

module Metadata.Test.TagsHandlingTests

open System
open NUnit.Framework
open RoonTagger.Metadata
open FsUnit

module ``TagsMap Manipulation`` =
    type MergeTestData =
        { description: string
          original: TagsMap
          updates: TagsMap
          expected: TagsMap }

    type DeleteTagsValueData =
        { description: string
          original: TagsMap
          expected: TagsMap
          key: TagName
          value: string }

    let mergeTestsData () =
        [ { description = "title should be replaced"
            original = Map [ (TitleTag, TagValue.ofString "old title") ]
            updates = Map [ (TitleTag, TagValue.ofString "new title") ]
            expected = Map [ (TitleTag, TagValue.ofString "new title") ] }
          { description = "tags not in updates should remain as they are"
            original =
              Map
                  [ (TitleTag, TagValue.ofString "old title")
                    (AlbumTag, TagValue.ofString "My Album") ]
            updates = Map [ (TitleTag, TagValue.ofString "new title") ]
            expected =
              Map
                  [ (TitleTag, TagValue.ofString "new title")
                    (AlbumTag, TagValue.ofString "My Album") ] }
          { description = "track and disc number should update"
            original = Map [ (TrackNumberTag, TagValue.ofInt 1); (DiscNumberTag, TagValue.ofInt 1) ]
            updates = Map [ (TrackNumberTag, TagValue.ofInt 3); (DiscNumberTag, TagValue.ofInt 2) ]
            expected = Map [ (TrackNumberTag, TagValue.ofInt 3); (DiscNumberTag, TagValue.ofInt 2) ] }
          { description = "artists should be merged"
            original = Map [ (ArtistTag, TagValue.ofList [ "John Coltrane"; "Archie Shepp" ]) ]
            updates = Map [ (ArtistTag, TagValue.ofString "Miles Davis") ]
            expected = Map [ (ArtistTag, TagValue.ofList [ "Miles Davis"; "John Coltrane"; "Archie Shepp" ]) ] }
          { description = "work/movement tags should be overwritten"
            original =
              Map
                  [ (WorkTag, TagValue.ofString "original work")
                    (MovementTag, TagValue.ofString "original movement")
                    (SectionTag, TagValue.ofString "original section")
                    (MovementIndexTag, TagValue.ofInt 1)
                    (MovementCountTag, TagValue.ofInt 1) ]
            updates =
              Map
                  [ (WorkTag, TagValue.ofString "new work")
                    (MovementTag, TagValue.ofString "new movement")
                    (SectionTag, TagValue.ofString "new section")
                    (MovementIndexTag, TagValue.ofInt 2)
                    (MovementCountTag, TagValue.ofInt 2) ]
            expected =
              Map
                  [ (WorkTag, TagValue.ofString "new work")
                    (MovementTag, TagValue.ofString "new movement")
                    (SectionTag, TagValue.ofString "new section")
                    (MovementIndexTag, TagValue.ofInt 2)
                    (MovementCountTag, TagValue.ofInt 2) ] }
          { description = "more tags to be replaced"
            original =
              Map
                  [ (ImportDateTag, TagValue.ofDate (DateTime(2022, 2, 2)))
                    (OriginalReleaseDateTag, TagValue.ofDate (DateTime(2022, 2, 2)))
                    (YearTag, TagValue.ofInt 2023) ]
            updates =
              Map
                  [ (ImportDateTag, TagValue.ofDate (DateTime(2023, 2, 2)))
                    (OriginalReleaseDateTag, TagValue.ofDate (DateTime(2023, 2, 2)))
                    (YearTag, TagValue.ofInt 2022) ]
            expected =
              Map
                  [ (ImportDateTag, TagValue.ofDate (DateTime(2023, 2, 2)))
                    (OriginalReleaseDateTag, TagValue.ofDate (DateTime(2023, 2, 2)))
                    (YearTag, TagValue.ofInt 2022) ] }
          { description = "composer and credit should be merged"
            original =
              Map
                  [ (ComposerTag, TagValue.ofList [ "Bach" ])
                    (CreditTag, TagValue.ofList [ "Alicia Olatuja - Voice"; "Dan Tepfer - Piano" ]) ]
            updates =
              Map
                  [ (ComposerTag, TagValue.ofList [ "Beethoven" ])
                    (CreditTag, TagValue.ofList [ "Sean Smith - Bass" ]) ]
            expected =
              Map
                  [ (ComposerTag, TagValue.ofList [ "Beethoven"; "Bach" ])
                    (CreditTag, TagValue.ofList [ "Sean Smith - Bass"; "Alicia Olatuja - Voice"; "Dan Tepfer - Piano" ]) ] } ]

    let deleteTagsValueData () =
        [ { description = "invalid tagName should be ignored"
            original = Map [ (ArtistTag, TagValue.ofString "Some Artist") ]
            expected = Map [ (ArtistTag, TagValue.ofString "Some Artist") ]
            key = CreditTag
            value = "some credit" }
          { description = "non-existing value should be ignored"
            original = Map [ (ArtistTag, TagValue.ofString "Some Artist") ]
            expected = Map [ (ArtistTag, TagValue.ofString "Some Artist") ]
            key = ArtistTag
            value = "Other Artist" }
          { description = "all occurrences of value should be removed"
            original = Map [ (ArtistTag, TagValue.ofList [ "Another Artist"; "Some Artist"; "Another Artist" ]) ]
            expected = Map [ (ArtistTag, TagValue.ofString "Some Artist") ]
            key = ArtistTag
            value = "Another Artist" } ]

    [<TestCaseSource(nameof mergeTestsData)>]
    let ``merge two tags-map should yield correct results`` (testData: MergeTestData) =
        let result = TagsMap.merge testData.original testData.updates
        Assert.AreEqual(testData.expected, result, testData.description)

    [<TestCaseSource(nameof deleteTagsValueData)>]
    let ``deleting tags value should handle correctly`` (testData: DeleteTagsValueData) =
        let result = TagsMap.deleteTagValue testData.key testData.value testData.original
        Assert.AreEqual(testData.expected, result, testData.description)

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

module Metadata.Test.FlacTests

open FsUnitTyped
open NUnit.Framework
open FsUnit
open TestsUtils
open RoonTagger.Metadata

module ``loading flac file`` =

    [<Test>]
    let ``load flac file reads all metadata`` () =
        // fsharplint:disable-next-line redundantNewKeyword // it's IDisposable
        use tmp = new CopiedFile("with-metadata.flac")
        let path = tmp.Path

        // Note: The file itself contains much more metadata - these are the ones we are loading...
        let expected =
            Map
                [ (AlbumTag, TagValue.ofString "The Parsonage")
                  (TitleTag, TagValue.ofString "Sailing To The Sunday School Picnic (Composed by Regina Carter)")
                  (ArtistTag, TagValue.ofList [ "Theo Bleckmann"; "Alicia Olatuja"; "Dan Tepfer"; "David Hajdu" ])
                  (UnHandled "ALBUMARTIST",
                   TagValue.ofList [ "Theo Bleckmann"; "Alicia Olatuja"; "Dan Tepfer"; "David Hajdu" ])
                  (UnHandled "COMMENT",
                   TagValue.ofString "visit: https://sunnysiderecords.bandcamp.com/album/the-parsonage")
                  (UnHandled "DATE", TagValue.ofString "2023")
                  (UnHandled "ISRC", TagValue.ofString "US2W62368501")
                  (CreditTag,
                   TagValue.ofList
                       [ "Theo Bleckmann - Voice"
                         "Alicia Olatuja - Voice"
                         "Dan Tepfer - Piano"
                         "Erik Friedlander - Cello"
                         "Carl Maraghi - Bass Clarinet"
                         "Sean Smith - Bass"
                         "David Hajdu - Words By"
                         "Regina Carter - Composer" ])
                  (ComposerTag, TagValue.ofString "Regina Carter")
                  (TrackNumberTag, TagValue.ofString "01") ]

        let _, meta = Formats.Flac.load path |> Result.unwrap
        meta |> should equal expected

    [<Test>]
    let ``extracted metadata looks for all defined tag names`` () =
        // each time a new supported tag is added, A matching tag/value should be added to the flac file loaded below!
        use tmp = new CopiedFile("all-identified-tags.flac")
        let path = tmp.Path
        let _, meta = Formats.Flac.load path |> Result.unwrap

        let tagNames =
            GetAllUnionCases<TagName>()
            |> Set.ofSeq
            |> Set.remove (UnHandled null) // cannot search for 'Unhandled null'

        let result = tagNames |> Set.filter (fun t -> Map.containsKey t meta |> not)
        result |> shouldBeEmpty

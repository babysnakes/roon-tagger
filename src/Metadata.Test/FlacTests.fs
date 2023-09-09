module Metadata.Test.FlacTests

open NUnit.Framework
open FsUnit
open TestsUtils
open RoonTagger.Metadata

module ``loading flac file`` =

    [<Test>]
    let ``load flac file reads all metadata`` () =
        use tmp = new CopiedFile("with-metadata.flac")
        let path = tmp.Path

        // Note: The file itself contains much more metadata - these are the ones we are loading...
        let expected =
            Map
                [ (AlbumTag, TagValue.ofString "The Parsonage")
                  (TitleTag, TagValue.ofString "Sailing To The Sunday School Picnic (Composed by Regina Carter)")
                  (ArtistTag, TagValue.ofList [ "Theo Bleckmann"; "Alicia Olatuja"; "Dan Tepfer"; "David Hajdu" ])
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

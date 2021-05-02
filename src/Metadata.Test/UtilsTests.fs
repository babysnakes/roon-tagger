namespace Metadata.Test

open FsUnit
open NUnit.Framework
open RoonTagger.Metadata.Utils

module ListTests =

    [<Test>]
    let ``groupByConsecutively should group things in order`` () =
        let data =
            [ (1, "one")
              (1, "two")
              (1, "three")
              (2, "one") ]

        let expected =
            [ (1, [ (1, "one"); (1, "two"); (1, "three") ])
              (2, [ (2, "one") ]) ]

        List.groupByConsecutively fst data |> should equal expected

    [<Test>]
    let ``groupByConsecutively should separate items with same key if not consecutive`` () =
        let data =
            [ (1, "one")
              (1, "two")
              (2, "one")
              (1, "three") ]

        let expected =
            [ (1, [ (1, "one"); (1, "two") ])
              (2, [ (2, "one") ])
              (1, [ (1, "three") ]) ]

        List.groupByConsecutively fst data |> should equal expected

namespace Metadata.Test

open NUnit.Framework
open FsUnit
open FParsec

open TestsUtils
open RoonTagger.Metadata.WorkMovement.MovementParser

[<TestFixture>]
type MovementParserTests() =

    static member SimpleParsingData() =
        [ (suffix, ".", DOT)
          (suffix, ":", COLON)
          (roman, "xii", ROMAN)
          (roman, "XVI", ROMAN)
          (no, "NO", NO)
          (no, "No ", NO)
          (idx, "4", IDX) ]

    static member MatchingTitlesTestData() =
        [ ("Obvious match", "No. 1: title one", "No. 2: title two", true)
          ("'No' case is not enforced", "no. 1: title one", "No. 2: title two", true)
          ("Roman case doesn't matter", "II: title one", "xi: title two", true)
          ("Don't break parser with partial prefixes", "1I title", "The second title", true)
          ("Dot following 'no' should be consistent", "no 1: title one", "no. 2: title two", false)
          ("colon/dot should be consistent", "I. title one", "I: title two", false) ]

    static member ExtractTitleTestData() =
        [ ("II: The title", "The title")
          ("no index title", "no index title")
          ("2 digit as first char in title", "2 digit as first char in title")
          ("2. digit as index", "digit as index")
          ("No. 2 in B-flat minor: Andante", "No. 2 in B-flat minor: Andante")
          // partial prefixes should not fail the parsing
          ("1I title one", "1I title one")
          ("I1 title one", "I1 title one") ]

    [<TestCaseSource(nameof (MovementParserTests.SimpleParsingData))>]
    member this.``Simple parsing should apply``((testParser, data, expected)) =
        run testParser data
        |> function
        | Success (res, _, _) when res = expected -> ()
        | x -> Assert.Fail($"Unexpected result '%A{x}', expected '{expected}'")

    [<TestCaseSource(nameof (MovementParserTests.MatchingTitlesTestData))>]
    member this.``Matching layout tests``((_description, title1, title2, shouldMatch)) =
        let r1 = run titleLayout title1 |> unwrapParserResult
        let r2 = run titleLayout title2 |> unwrapParserResult

        if shouldMatch then
            r1 |> should equal r2
        else
            r1 |> should not' (equal r2)

    [<TestCaseSource(nameof (MovementParserTests.ExtractTitleTestData))>]
    member this.``Extract title test``((testData, expected)) =
        let result = run title testData |> unwrapParserResult
        result |> should equal expected

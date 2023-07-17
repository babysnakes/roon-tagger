namespace RoonTagger.Cli.Test

open Argu
open FsUnit
open NUnit.Framework
open RoonTagger.Cli.Arguments
open RoonTagger.Cli.Commands.SetTags
open RoonTagger.Metadata
open TestsUtils

[<TestFixture>]
module SetTagsArgumentsTests =

    let parser = ArgumentParser.Create<SetTagsArgs>()

    [<Test>]
    let ``--import-date should accept "today" as date`` () =
        let argsWithToday = [| "-I"; "today"; "some-file" |]
        let parsed = parser.Parse argsWithToday
        extractTags parsed |> ignore // should throw if not working.

[<TestFixture>]
module SetArgsIntegrationTests =

    let parser = ArgumentParser.Create<SetTagsArgs>()

    [<Test>]
    let ``--composer sets both composer tag and credits`` () =
        // fsharplint:disable-next-line redundantNewKeyword // it's disposable
        use tmp = new CopiedFile("empty.flac")
        let path = tmp.Path
        let args = parser.Parse [| "--composer"; "Composer A,Composer B"; path |]

        handleCmd args
        |> function
            | Ok _ -> ()
            | Error _ -> Assert.Fail "Setting contents returned an error"

        let track = Track.load path |> Result.unwrap
        let composers = Track.getTagStringValue track ComposerTag
        composers |> should contain "Composer A"
        composers |> should contain "Composer B"
        let credits = Track.getTagStringValue track CreditTag
        credits |> should contain "Composer A - Composer"
        credits |> should contain "Composer B - Composer"

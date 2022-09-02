namespace RoonTagger.Cli.Test

open Argu
open NUnit.Framework
open RoonTagger.Cli.Arguments
open RoonTagger.Cli.Commands.SetTags

[<TestFixture>]
module SetTagsArgumentsTests =

    let parser = ArgumentParser.Create<SetTagsArgs>()

    [<Test>]
    let ``--import-date should accept "today" as date`` () =
        let argsWithToday = [| "-I"; "today"; "some-file" |]
        let parsed = parser.Parse argsWithToday
        extractTags parsed |> ignore // should throw if not working.

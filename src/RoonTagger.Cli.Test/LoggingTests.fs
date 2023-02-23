namespace Metadata.Test

open NUnit.Framework
open FsUnit
open Serilog.Events
open RoonTagger.Cli.Commands.Main
open RoonTagger.Cli.Configuration

[<TestFixture>]
type LoggingTests() =

    static member LoggingEnabledInput() =
        [ let defaultLC =
              { File = "file.log"
                Level = LogLevel.None }

          (defaultLC, 1, [ (LogEventLevel.Information, true); (LogEventLevel.Debug, false) ])

          ({ defaultLC with Level = LogLevel.Info },
           0,
           [ (LogEventLevel.Information, true); (LogEventLevel.Debug, false) ])

          (defaultLC, 2, [ (LogEventLevel.Debug, true); (LogEventLevel.Verbose, false) ])

          ({ defaultLC with
              Level = LogLevel.Debug },
           0,
           [ (LogEventLevel.Debug, true); (LogEventLevel.Verbose, false) ])

          (defaultLC, 3, [ (LogEventLevel.Verbose, true) ])
          (defaultLC, 5, [ (LogEventLevel.Verbose, true) ]) ]

    [<TestCaseSource(nameof LoggingTests.LoggingEnabledInput)>]
    member this.``Test log level calculation with logging enabled``
        ((lc: LogConfigV1, overrides: int, testCases: (LogEventLevel * bool) list))
        =
        let logger = setupLogger lc overrides |> Option.get

        for case in testCases do
            let level, enabled = case
            logger.IsEnabled level |> should equal enabled

    [<Test>]
    member this.``Test log level calculation with logging disabled``() =
        setupLogger
            { File = "file.log"
              Level = LogLevel.None }
            0
        |> should equal None

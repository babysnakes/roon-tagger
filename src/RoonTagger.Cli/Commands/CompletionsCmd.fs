module RoonTagger.Cli.Commands.Completions

open Argu
open RoonTagger.Cli.Arguments
open RoonTagger.Cli.Models
open RoonTagger.Cli.Help.LongHelp
open FSharp.Data.LiteralProviders
open System

[<Literal>]
let PowershellCompletion =
    TextFile.Resources.Completions.``roon-tagger-completion.ps1``.Text

let handleCmd (_: ParseResults<CompletionsArgs>) =
    // Currently there's no need to check anything
    Console.WriteLine PowershellCompletion
    Ok(())

let makeCompletionsCmd (args: ParseResults<CompletionsArgs>) =
    { new ISubCommand with
        member this.Run() = handleCmd args
        member this.LongHelp() = completionsLH }

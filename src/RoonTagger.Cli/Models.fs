module RoonTagger.Cli.Models

open Spectre.Console.Rendering
open RoonTagger.Metadata
open System

type ISubCommand =
    /// The main execution logic of the sub-command.
    abstract member Run : unit -> Result<unit, unit>

    /// Prints long help for the sub-command.
    abstract member LongHelp : unit -> string * Uri

type CliErrors =
    | MError of MetadataErrors
    | CliIOError of string
    | TagsCountError
    | ConfigurationParseError of string
    | CliArgumentsError of string

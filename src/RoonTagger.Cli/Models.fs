module RoonTagger.Cli.Models

open Spectre.Console.Rendering
open RoonTagger.Metadata

type ISubCommand =
    /// The main execution logic of the sub-command.
    abstract member Run : unit -> Result<unit, unit>

    /// Prints long help for the sub-command.
    abstract member LongHelp : unit -> IRenderable list

type CliErrors =
    | MError of MetadataErrors
    | CliIOError of string
    | TagsCountError
    | ConfigurationParseError of string
    | CliArgumentsError of string

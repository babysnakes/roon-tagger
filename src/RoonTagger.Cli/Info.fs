namespace RoonTagger.Cli

open RoonTagger.Metadata

module Info =
    let Name = "roon-tagger"
    let Version = "0.1.0-alpha2"

module Models =

    type CliErrors =
        | MError of MetadataErrors
        | CliIOError of string
        | TitlesCountError
        | ConfigurationParseError of string
        | CliArgumentsError of string

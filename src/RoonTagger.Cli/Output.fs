module RoonTagger.Cli.Output

open RoonTagger.Metadata

let handleErrors (errs: MetadataErrors list) : string list =
    [ "errors" ]
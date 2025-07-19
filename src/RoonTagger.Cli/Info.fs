module RoonTagger.Cli.Info

open System.Reflection

let name = "roon-tagger"

let Version () =
    Assembly.GetExecutingAssembly().GetName().Version

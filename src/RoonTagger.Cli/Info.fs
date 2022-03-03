module RoonTagger.Cli.Info

open System.Reflection

let Name = "roon-tagger"

let Version () =
    Assembly.GetExecutingAssembly().GetName().Version

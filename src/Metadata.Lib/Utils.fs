module RoonTagger.Metadata.Utils

open System

let formatDate (date: DateTime) =
    date.ToString("yyyy-MM-dd")

[<RequireQualifiedAccess>]
module Result =

    /// A folder function for folding a collection of results into one result. Returns the first
    /// error or the last Ok.
    let folder state result =
        match state with
        | Ok okResult -> result
        | Error _ -> state

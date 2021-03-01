module RoonTagger.Metadata.Utils

[<RequireQualifiedAccess>]
module Result =

    /// Predicate is result is Ok ...
    let isOk =
        function
        | Ok _ -> true
        | Error _ -> false

    /// A folder function for folding a collection of results into one result. Returns the first
    /// error or the last Ok.
    let folder state result =
        match state with
        | Ok okResult -> result
        | Error _ -> state

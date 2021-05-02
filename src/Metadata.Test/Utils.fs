module TestsUtils

open FParsec.CharParsers
open System
open RoonTagger.Metadata

/// Get the path for a file in the project 'Resources' directory.
let getResourcePath fileName =
    [| Environment.CurrentDirectory
       "Resources"
       fileName |]
    |> IO.Path.Combine

/// Force extract flac file from `AudioTrack`. Throw exception if not flac.
let extractFlac (file: AudioTrack) =
    match file.Track with
    | Flac file -> file

/// Extract the match value of ParserResult
let inline unwrapParserResult (pr: ParserResult<_, _>) =
    match pr with
    | Success (result, _, _) -> result
    | Failure (msg, _, _) -> failwith $"Trying to unwrap failure: {msg}"

[<RequireQualifiedAccess>]
module Result =
    /// After Rust's unwrap. Can throw `System.Exception`
    let unwrap =
        function
        | Ok value -> value
        | Error err -> failwith $"Called unwrap on Error {err}"

    /// See unwrap ...
    let unwrapError =
        function
        | Ok value -> failwith $"Called unwrapError on Ok {value}"
        | Error err -> err

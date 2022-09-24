module TestsUtils

open FParsec.CharParsers
open System
open System.IO
open RoonTagger.Metadata

/// Get the path for a file in the project 'Resources' directory.
let getResourcePath fileName =
    [| Environment.CurrentDirectory
       "Resources"
       fileName |]
    |> Path.Combine

/// Force extract flac file from `AudioTrack`. Throw exception if not flac.
let extractFlac (file: AudioTrack) =
    match file.Track with
    | Flac file -> file

/// Extract the match value of ParserResult
let inline unwrapParserResult (pr: ParserResult<_, _>) =
    match pr with
    | Success (result, _, _) -> result
    | Failure (msg, _, _) -> failwith $"Trying to unwrap failure: {msg}"

/// A "use"able temporary flac file copier from the provided flac file name in the tests _Resource_ directory.
type CopiedFile(fileName: string) =
    let origPath = getResourcePath fileName
    let flacFileName = Path.GetRandomFileName() + ".flac"
    let targetPath = Path.Combine(Path.GetTempPath(), flacFileName)
    do File.Copy(origPath, targetPath)
    member x.Path = targetPath

    interface IDisposable with
        member x.Dispose() =
            try
                File.Delete(targetPath)
            with
            // Dispose should be idempotent
            | _ -> ()

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

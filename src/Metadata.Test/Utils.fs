module TestsUtils

open System

/// Get the path for a file in the project 'Resources' directory.
let getResourcePath fileName =
    [|
        Environment.CurrentDirectory
        "Resources"
        fileName
    |]
    |> IO.Path.Combine


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
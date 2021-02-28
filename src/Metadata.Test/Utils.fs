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
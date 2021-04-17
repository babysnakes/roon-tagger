module RoonTagger.Cli.ProcessRunner

open System
open FsToolkit.ErrorHandling

let log = Serilog.Log.Logger

/// Tries to search cmd in path.
let searchInPath (cmd: string) =
    log.Debug("Searching for '{Cmd}' in PATH ...", cmd)

    match (Environment.GetEnvironmentVariable "PATH" |> Option.ofObj) with
    | None -> cmd
    | Some path ->
        path
        |> fun s -> s.Split ";"
        |> Array.map (fun d -> IO.Path.Combine(d, cmd))
        |> Array.tryFind IO.File.Exists
        |> function
        | Some exe -> exe
        | None -> cmd
        |> fun exe ->
            log.Debug("Search in path result: {Exe}", exe)
            exe

let runCmd (cmd: string) (args: string) =
    let proc = Diagnostics.Process.Start(cmd, args)
    proc.WaitForExit()

module RoonTagger.Cli.Commands.SetTags

open Argu
open FsToolkit.ErrorHandling
open RoonTagger
open RoonTagger.Cli.Arguments
open RoonTagger.Cli.Output
open RoonTagger.Metadata

let extractTags (opts: ParseResults<SetTagsArgs>) =
    let titleTag =
        opts.TryGetResult SetTagsArgs.Title
        |> Option.map Metadata.Title

    let importDateTag =
        if (opts.Contains Import_Date) then
            opts.PostProcessResult(<@ Import_Date @>, parseDate)
            |> Metadata.ImportDate
            |> Some
        else
            None

    let ordTag =
        if (opts.Contains Release_Date) then
            opts.PostProcessResult(<@ Release_Date @>, parseDate)
            |> Metadata.OriginalReleaseDate
            |> Some
        else
            None

    let yearTag =
        opts.TryGetResult SetTagsArgs.Year |> Option.map Metadata.Year

    [ titleTag
      importDateTag
      ordTag
      yearTag ]
    |> List.filter Option.isSome
    |> List.map Option.get // safe as we filtered out the None tags.

let handleCmd (opts: ParseResults<SetTagsArgs>) =

    printfn "DEBUG: opts: %A" opts

    let tags = extractTags opts

    opts.GetResult Files
    |> List.traverseResultA Track.load
    |> Result.bind (List.traverseResultM (fun f -> Track.setTags f tags))
    |> Result.map List.concat
    |> Result.bind (List.traverseResultM Track.applyTags)
    |> Result.map (fun _ -> [ "Operation finished successfully" ])
    |> Result.mapError (fun errs -> handleErrors errs)

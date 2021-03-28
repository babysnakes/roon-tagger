module RoonTagger.Cli.Commands.SetTags

open Argu
open FsToolkit.ErrorHandling
open FsToolkit.ErrorHandling.Operator.Result
open RoonTagger
open RoonTagger.Metadata
open RoonTagger.Cli.Arguments
open RoonTagger.Cli.Output

let extractTags (opts: ParseResults<SetTagsArgs>) =
    let titleTag =
        opts.TryGetResult Title
        |> Option.map Metadata.Title

    let importDateTag =
        if (opts.Contains Import_Date) then
            opts.PostProcessResult(<@ Import_Date @>, parseDate)
            |> ImportDate
            |> Some
        else
            None

    let ordTag =
        if (opts.Contains Release_Date) then
            opts.PostProcessResult(<@ Release_Date @>, parseDate)
            |> OriginalReleaseDate
            |> Some
        else
            None

    let yearTag =
        opts.TryGetResult Year |> Option.map Metadata.Year

    [ titleTag
      importDateTag
      ordTag
      yearTag ]
    |> List.filter Option.isSome
    |> List.map Option.get // safe as we filtered out the None tags.

let handleCmd (opts: ParseResults<SetTagsArgs>) =

    let tags = extractTags opts

    opts.GetResult SetTagsArgs.Files
    |> List.traverseResultA Track.load
    >>= List.traverseResultM (fun f -> Track.setTags f tags)
    |> Result.map List.concat
    >>= List.traverseResultM Track.applyTags
    |> Result.map (fun _ -> handleOutput "Operation finished successfully")
    |> Result.mapError (List.map error2String >> handleErrors)
    

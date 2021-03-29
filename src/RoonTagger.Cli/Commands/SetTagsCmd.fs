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
    result {
        let tags = extractTags opts
        let files = opts.GetResult SetTagsArgs.Files
        let! tracks = List.traverseResultA Track.load files

        do!
            List.traverseResultA (fun t -> Track.setTags t tags) tracks
            |> Result.map ignore
            |> Result.mapError List.concat

        return!
            List.traverseResultM (fun t -> Track.applyTags t) tracks
            |> Result.map (fun _ -> handleOutput "Operation handled successfully")
    }
    |> Result.mapError (List.map error2String >> handleErrors)

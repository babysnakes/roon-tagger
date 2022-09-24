module RoonTagger.Cli.Commands.SetTags

open System
open Argu
open FsToolkit.ErrorHandling
open FsToolkit.ErrorHandling.Operator.Result
open RoonTagger
open RoonTagger.Metadata
open RoonTagger.Cli.Arguments
open RoonTagger.Cli.Help.LongHelp
open RoonTagger.Cli.Models
open RoonTagger.Cli.Output

let parseComposers (composers: string) =
    let result = composers.Split(",") |> List.ofArray |> List.map (fun s -> s.Trim())

    if List.exists String.IsNullOrWhiteSpace result then
        failwith $"composers contains empty or whitespace-only composer '{composers}'"

    result

let extractTags (opts: ParseResults<SetTagsArgs>) =
    let titleTag = opts.TryGetResult Title |> Option.map Metadata.Title

    let composerTag =
        if (opts.Contains Composer) then
            opts.PostProcessResult(<@ Composer @>, parseComposers)
            |> Metadata.Composer
            |> Some
        else
            None

    let importDateTag =
        if (opts.Contains Import_Date) then
            opts.PostProcessResult(<@ Import_Date @>, parseTodayOrDate)
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

    let yearTag = opts.TryGetResult Year |> Option.map Metadata.Year

    [ titleTag
      composerTag
      importDateTag
      ordTag
      yearTag ]
    |> List.filter Option.isSome
    |> List.map Option.get // safe as we filtered out the None tags.

let matchComposer =
    function
    | Metadata.Composer _ -> true
    | _ -> false

let convertComposersToCredits composerMaybe =
    match composerMaybe with
    | Metadata.Composer composers -> List.map (fun c -> Personnel $"{c} - Composer") composers |> Some
    | _ -> None

let handleCmd (opts: ParseResults<SetTagsArgs>) =
    result {
        let tags = extractTags opts
        let files = opts.GetResult SetTagsArgs.Files
        let! tracks = List.traverseResultA Track.load files

        List.tryFind matchComposer tags
        |> Option.bind convertComposersToCredits
        |> function
            | None -> ()
            | Some composers -> do tracks |> List.map (fun t -> Track.addCredits t composers) |> ignore

        do!
            List.traverseResultA (fun t -> Track.setTags t tags) tracks
            |> Result.map ignore
            |> Result.mapError List.concat

        return!
            List.traverseResultM Track.applyTags tracks
            |> Result.map (fun _ -> handleOutput "Operation handled successfully")
    }
    |> Result.mapError (List.map error2String >> handleErrors)

let makeSetArgsCmd (args: ParseResults<SetTagsArgs>) =
    { new ISubCommand with
        member this.Run() = handleCmd args
        member this.LongHelp() = setTagsLH }

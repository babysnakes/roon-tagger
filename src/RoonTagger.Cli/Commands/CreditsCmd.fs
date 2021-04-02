module RoonTagger.Cli.Commands.Credits

open System
open Argu
open FsToolkit.ErrorHandling
open RoonTagger.Cli.Arguments
open RoonTagger.Cli.Output
open RoonTagger.Metadata

let extractAddCredits (opts: ParseResults<CreditsArgs>) =

    let parseCredit (name: string, rolesAsString: string) =
        let roles = rolesAsString.Split(",") |> List.ofArray

        if List.exists String.IsNullOrWhiteSpace roles then
            failwith $"roles contains empty or whitespace-only roles ({rolesAsString})"

        (name, roles)

    opts.PostProcessResults(<@ Add @>, parseCredit)


let handleCmd (args: ParseResults<CreditsArgs>) : Result<unit, unit> =
    result {
        let files = args.GetResult CreditsArgs.Files
        let! tracks = List.traverseResultA Track.load files
        let delCredits = args.GetResults Del |> List.map Personnel
        let! addCredits = extractAddCredits args |> Track.mkPersonnel

        do!
            tracks
            |> List.traverseResultA (fun t -> Track.deleteCredits t delCredits)
            |> Result.map ignore
            |> Result.mapError List.concat

        do
            tracks
            |> List.map (fun t -> Track.addCredits t addCredits)
            |> ignore

        do!
            tracks
            |> List.traverseResultM Track.applyTags
            |> Result.map (fun _ -> handleOutput "Operation handled successfully")
    }
    |> Result.mapError (List.map error2String >> handleErrors)

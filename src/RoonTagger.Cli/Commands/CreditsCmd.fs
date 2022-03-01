module RoonTagger.Cli.Commands.Credits

open System
open Argu
open FsToolkit.ErrorHandling
open Pluralize.NET
open RoonTagger.Cli.Arguments
open RoonTagger.Cli.Help.LongHelp
open RoonTagger.Cli.Models
open RoonTagger.Cli.Output
open RoonTagger.Metadata
open RoonTagger.Metadata.Utils

let extractAddCredits (opts: ParseResults<CreditsArgs>) =

    let parseCredit (name: string, rolesAsString: string) =
        let roles = rolesAsString.Split(",") |> List.ofArray

        if List.exists String.IsNullOrWhiteSpace roles then
            failwith $"roles contains empty or whitespace-only roles ({rolesAsString})"

        (name, roles)

    opts.PostProcessResults(<@ Add @>, parseCredit)


let handleCmd (args: ParseResults<CreditsArgs>) : Result<unit, unit> =
    result {
        let validator =
            if args.Contains Skip_Validation then
                Roles None
            else
                getSupportedRoles() |> Some |> Roles

        let files = args.GetResult CreditsArgs.Files
        let! tracks = List.traverseResultA Track.load files
        let delCredits = args.GetResults Del |> List.map Personnel
        let! addCredits = extractAddCredits args |> Track.mkPersonnel validator

        do!
            tracks
            |> List.traverseResultA (fun t -> Track.deleteCredits t delCredits)
            |> Result.map ignore
            |> Result.mapError List.concat

        do tracks |> List.map (fun t -> Track.addCredits t addCredits) |> ignore

        let p = Pluralizer()
        let addPlural = p.Format("credit", addCredits |> List.length, true)
        let delPlural = p.Format("credit", delCredits |> List.length, true)

        do!
            tracks
            |> List.traverseResultM Track.applyTags
            |> Result.map (fun _ -> handleOutput $"Successfully added {addPlural} and deleted {delPlural}")
    }
    |> Result.mapError (List.map error2String >> handleErrors)

let makeCreditsCmd (args: ParseResults<CreditsArgs>) =
    { new ISubCommand with
        member this.Run() = handleCmd args
        member this.LongHelp() = creditsLH }

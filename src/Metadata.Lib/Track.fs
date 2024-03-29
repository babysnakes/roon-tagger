namespace RoonTagger.Metadata

open Serilog
open FlacLibSharp
open FsToolkit.ErrorHandling
open RoonTagger.Metadata.Formats
open RoonTagger.Metadata.Utils

module Track =

    let log = Log.Logger

    let load (fileName: string) : Result<AudioTrack, MetadataErrors> =
        try
            // fsharplint:disable-next-line redundantNewKeyword // it's IDisposable
            let track = new FlacFile(fileName)
            Ok { Path = fileName; Track = Flac track }
        with
        | :? System.IO.FileNotFoundException as err ->
            log.Error("Loading track: {Err}", err)
            Error(FileDoesNotExist err.Message)
        | :? Exceptions.FlacLibSharpInvalidFormatException as err ->
            log.Error("Loading track '{FileName}': {Err}", fileName, err)
            Error($"Not a valid FLAC file: '%s{fileName}'" |> InvalidFileFormat)
        | err ->
            log.Error("Loading track '{FileName}': {Err}", fileName, err)
            Error(UnexpectedError err.Message)

    /// Sets (replaces if needed) the tag in the track.
    let setTag (track: AudioTrack) (tag: RoonTag) : Result<AudioTrack, MetadataErrors> =
        match track.Track with
        | Flac file -> Flac.setTag file tag
        |> Result.map (fun _ -> track)

    /// Sets (replaces if needed) the tags in the track.
    let setTags (track: AudioTrack) (tags: RoonTag list) : Result<AudioTrack, MetadataErrors list> =
        tags
        |> List.traverseResultA (setTag track)
        |> Result.map (fun _ -> track)

    let applyTags (track: AudioTrack) =
        match track.Track with
        | Flac file -> Flac.applyChanges file


    /// Extracts the requested tag from the track and returns it as a list of
    /// strings. Note that if the tag is not set it will return empty list.
    let getTagStringValue (track: AudioTrack) (tagName: TagName) : string list =
        match track.Track with
        | Flac file -> Flac.getTagStringValue file tagName

    /// Extracts the requested tag from the track and returns it as a list of
    /// strings. If the tag is not set it will return a list of single empty
    /// string (hence "safe")
    let safeGetTagStringValue track tagName =
        getTagStringValue track tagName
        |> function
            | [] -> [ "" ]
            | l -> l

    /// Converts "shortcut" list of name and list of roles to correctly
    /// formatted `Personnel` tags. Fails if any of the roles fail validation.
    let mkPersonnel (validator: RoleValidator) (credits: (string * string list) list) =
        let mkSingle credit : Result<Personnel list, MetadataErrors list> =
            result {
                let name, roles = credit
                let make r = $"{name} - {r}"
                do! validator.ValidateMany roles |> Result.map ignore
                return roles |> List.map (make >> Personnel)
            }

        credits
        |> List.traverseResultA mkSingle
        |> Result.map List.concat
        |> Result.mapError List.concat

    /// Adds the provided credits to the existing ones in the track. Merges
    /// multiple entries. Note, it *does not* check the validity of Personnel.
    let addCredits (track: AudioTrack) (credits: Personnel list) =
        let current = getTagStringValue track CreditTag
        let toAdd = List.map (fun (Personnel p) -> p) credits
        let newValue = List.append current toAdd |> List.distinct

        match track.Track with
        | Flac file -> Flac.setRaw file Flac.CreditTag newValue

    let deleteCredits (track: AudioTrack) (credits: Personnel list) : Result<unit, MetadataErrors list> =
        let current = getTagStringValue track CreditTag
        let toDelete = List.map (fun (Personnel p) -> p) credits
        let invalidDeletes = List.filter (fun s -> List.contains s current |> not) toDelete

        if List.isEmpty invalidDeletes then
            let calculated = List.removeByValues toDelete current

            match track.Track with
            | Flac file -> Flac.setRaw file Flac.CreditTag calculated
            |> Ok
        else
            invalidDeletes
            |> List.map (fun s -> DeletingNonExistingPersonnel(track, s))
            |> Error

namespace RoonTagger.Metadata

open FlacLibSharp
open FsToolkit.ErrorHandling
open RoonTagger.Metadata.Formats

module Track =

    let load (fileName: string) : Result<AudioTrack, MetadataErrors> =
        try
            let track = new FlacFile(fileName)
            Ok { Path = fileName; Track = Flac track }
        with
        | :? System.IO.FileNotFoundException as err -> Error(FileDoesNotExist err.Message)
        | :? Exceptions.FlacLibSharpInvalidFormatException ->
            Error(
                sprintf "Not a valid FLAC file: '%s'" fileName
                |> InvalidFileFormat
            )
        | err -> Error(UnexpectedError err.Message)

    /// Sets (replaces if needed) the tag in the track.
    let setTag (track: AudioTrack) (tag: RoonTag) : Result<AudioTrack, MetadataErrors> =
        match track.Track with
        | Flac file -> Flac.setTag file tag
        |> Result.map (fun _ -> track)

    /// Sets (replaces if needed) the tags in the track.
    let setTags (track: AudioTrack) (tags: RoonTag list) =
        tags |> List.traverseResultA (setTag track)

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

namespace RoonTagger.Metadata

open FlacLibSharp
open RoonTagger.Metadata.Formats
open RoonTagger.Metadata.Utils

module Track =

    let load (fileName: string) : Result<AudioTrack, MetadataErrors> =
        try
            let track = new FlacFile(fileName)
            Ok { Path = fileName; Track = Flac track }
        with
        | :? System.IO.FileNotFoundException as err -> Error(FileDoesNotExist err.Message)
        | :? Exceptions.FlacLibSharpInvalidFormatException as err -> Error(InvalidFileFormat err.Message)
        | err -> Error(UnexpectedError err.Message)

    /// Sets (replaces if needed) the tag in the track.
    let setTag (track: AudioTrack) (tag: RoonTag) : Result<unit, MetadataErrors> =
        match track.Track with
        | Flac file -> Flac.setTag file tag

    let setTags (track: AudioTrack) (tags: RoonTag list) =
        tags
        |> List.map (fun t -> setTag track t)
        |> List.fold Result.folder (Ok())

    let getTagStringValue (track: AudioTrack) (tagName: TagName) =
        match track.Track with
        | Flac file -> Flac.getTagStringValue file tagName

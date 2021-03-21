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
        | :? Exceptions.FlacLibSharpInvalidFormatException as err -> Error(InvalidFileFormat err.Message)
        | err -> Error(UnexpectedError err.Message)

    /// Sets (replaces if needed) the tag in the track.
    let setTag (track: AudioTrack) (tag: RoonTag) : Result<AudioTrack, MetadataErrors> =
        match track.Track with
        | Flac file -> Flac.setTag file tag
        |> Result.map (fun _ -> track)

    /// Sets (replaces if needed) the tags in the track.
    let setTags (track: AudioTrack) (tags: RoonTag list) =
        tags
        |> List.traverseResultA (setTag track)
    
    let applyTags (track: AudioTrack) =
        match track.Track with
        | Flac file -> Flac.applyChanges file


    let getTagStringValue (track: AudioTrack) (tagName: TagName) =
        match track.Track with
        | Flac file -> Flac.getTagStringValue file tagName

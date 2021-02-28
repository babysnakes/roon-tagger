module Metadata.Formats.Flac

open ATL
open RoonTagger.Metadata
open RoonTagger.Metadata.Utils

let applyTags (track: Track) (tags: RoonTags list) =
    let applyTag =
        function
        | Title title -> 
            track.Title <- title
            Ok track
        | _ -> Error(UnsupportedTagPerFormat "not implemented")

    tags
    |> List.map applyTag
    |> List.fold Result.folder (Ok track)

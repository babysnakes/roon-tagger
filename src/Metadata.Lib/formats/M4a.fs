module Metadata.Formats.M4a

open ATL
open RoonTagger.Metadata
open RoonTagger.Metadata.Utils

let applyTag (track: Track) (tag: RoonTags) =
    match tag with
    | Title title ->
        track.Title <- title
        Ok track
    | _ -> Error(UnsupportedTagPerFormat "not implemented")

module Metadata.Formats.Flac

open ATL
open RoonTagger.Metadata

let applyTag (track: Track) (tag: RoonTags) =
    match tag with
    | Title title -> 
        track.Title <- title
        Ok track
    | _ -> Error(UnsupportedTagPerFormat "not implemented")

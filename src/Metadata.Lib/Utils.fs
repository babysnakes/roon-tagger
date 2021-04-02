module RoonTagger.Metadata.Utils

open System

let formatDate (date: DateTime) = date.ToString("yyyy-MM-dd")

[<RequireQualifiedAccess>]
module List =
    // Remove all occurrences of every item in values from the list.
    let removeByValues (values: 'T list) (lst: 'T list) : 'T list =
        let folder (item: 'T) (acc: 'T list) =
            if List.contains item values then
                acc
            else
                item :: acc

        List.foldBack folder lst []

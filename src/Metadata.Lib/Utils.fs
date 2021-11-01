module RoonTagger.Metadata.Utils

open System
open System.IO
open RoonTagger.Metadata

let log = Serilog.Log.Logger

let formatDate (date: DateTime) = date.ToString("yyyy-MM-dd")

let loadSupportedRoles () =
    let rolesPath =
        Path.Join(
            [| AppContext.BaseDirectory
               "Resources"
               "roles.txt" |]
        )

    try
        File.ReadAllLines(rolesPath) |> List.ofSeq |> Ok
    with
    | ex ->
        log.Error("Loading roles from file: {Ex}", ex)
        UnexpectedError ex.Message |> Error

[<RequireQualifiedAccess>]
module List =
    /// Remove all occurrences of every item in values from the list.
    let removeByValues (values: 'T list) (lst: 'T list) : 'T list =
        let folder (item: 'T) (acc: 'T list) =
            if List.contains item values then
                acc
            else
                item :: acc

        List.foldBack folder lst []

    let ofItem (value: 'T) : 'T list = [ value ]

    /// Map the items in a list for side effect and return original items
    let tee (f: 'T -> unit) (lst: 'T list) : 'T list =
        let fn item =
            f item
            item

        lst |> List.map fn


    let groupByConsecutively (fKey: 'T -> 'key) (lst: 'T list) : ('key * 'T list) list =
        let loop (item: 'T) (state: ('key * 'T list) list) : ('key * 'T list) list =
            let key = fKey item

            match state with
            | (k, items) :: rest when k = key -> (k, (item :: items)) :: rest
            | items -> (key, [ item ]) :: items

        List.foldBack loop lst []

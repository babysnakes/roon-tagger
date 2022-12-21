module RoonTagger.Metadata.Utils

open System
open FSharp.Data.LiteralProviders

let log = Serilog.Log.Logger

let formatDate (date: DateTime) = date.ToString("yyyy-MM-dd")

[<Literal>]
let SupportedRoles = TextFile.Resources.``roles.txt``.Text

let getSupportedRoles () =
    SupportedRoles.Split(Environment.NewLine) |> List.ofArray

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


    let groupByConsecutively (fKey: 'T -> 'Key) (lst: 'T list) : ('Key * 'T list) list =
        let loop (item: 'T) (state: ('Key * 'T list) list) : ('Key * 'T list) list =
            let key = fKey item

            match state with
            | (k, items) :: rest when k = key -> (k, (item :: items)) :: rest
            | items -> (key, [ item ]) :: items

        List.foldBack loop lst []

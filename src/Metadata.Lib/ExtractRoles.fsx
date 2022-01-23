// This documents the process of extracting the supported roles of the Roon
// wiki.

// The first step is to point hour browser to:
// https://help.roonlabs.com/portal/en/kb/articles/credit-roles
//
// This might take long to load. Open your browser's developer tools in the
// *Network* tab and select 'xhr'. than save it locally under "bin\roles.json"
// (relative to this file).

// Send the file to FSI and then run: `run()` - This should update the saved
// copy of `roles.txt`.

#r "nuget: FSharp.Data, 4.1.0"

open System.IO
open FSharp.Data

let basePath = __SOURCE_DIRECTORY__

let rolesJson = Path.Join([| basePath; "bin"; "roles.json" |])

let resources = Path.Join([| __SOURCE_DIRECTORY__; "Resources" |])

let outputFile = Path.Join([| resources; "roles.txt" |])

let run () =
    let json = JsonValue.Load(rolesJson)
    let rolesData = json.GetProperty "answer"
    let html = HtmlDocument.Parse(rolesData.AsString())
    let table = html.Descendants [ "table" ] |> Seq.head

    let roles =
        [
          // First table row is titles
          for row in table.Descendants [ "tr" ] |> Seq.tail do
              for td in row.Descendants [ "td" ] do
                  let t = td.InnerText().Trim()
                  if t <> "" then yield t ]
        |> List.sort

    Directory.CreateDirectory resources |> ignore
    File.WriteAllLines(outputFile, roles)

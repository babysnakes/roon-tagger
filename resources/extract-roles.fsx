// This documents the process of extracting the supported roles of the Roon
// wiki.

// The first step is to point hour browser to:
// https://help.roonlabs.com/portal/en/kb/articles/credit-roles
//
// This might take a while to load. Open your browser's developer tools in the
// *Network* tab and select 'xhr'.  It could be a bit tricky to find the right file,
// easiest way is to use the development tools *find* button and enter a known credit.
// Than save it locally (optionally as resources/roles.json).

// Run (inside the resource directory):
//
//   dotnet fsi extract-roles.fsx <path to input.json> roles.txt

#r "nuget: FSharp.Data, 4.1.0"

open System.IO
open FSharp.Data

let run (rolesJson: string) (outputFile: string) =
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

    Path.GetDirectoryName(Path.GetFullPath outputFile)
    |> Directory.CreateDirectory
    |> ignore

    File.WriteAllText(outputFile, roles |> String.concat "\n")

match fsi.CommandLineArgs with
// FSharp.Data resolves relative paths against the FSI runtime directory, not
// the current directory, so make them absolute first.
| [| _; input; output |] -> run (Path.GetFullPath input) (Path.GetFullPath output)
| _ -> eprintfn "Usage: dotnet fsi ExtractRoles.fsx <input.json> <output.txt>"

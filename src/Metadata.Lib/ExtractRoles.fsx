(*
    This documents the process of extracting the supported roles of the Roon wiki.

    Note, This wiki occasionally changes it's technology. Currently the list of roles are passed as large markdown
    section which is being parsed in browser rendering. Trying to parse the page directly via this script proved to be a
    little challenging and since this process should be performed only once in a while it makes more sense to let the
    browser render the page, than save it locally and then process it.

    The first step is to point your browser to:
    https://kb.roonlabs.com/Roon_Credit_Roles

    Now you should save the web page as `page.html` under './bin` relative to this script's location. Open the file in
    browser or text viewer to validate that the roles section is a rendered HTML and not large text block. Also validate
    that there are exactly 6 categories under *Role Categories* at the top of the page.

    From the root of the repository run:
   
    $ dotnet paket generate-load-scripts -g Dev
    $ dotnet fsi ./src/Metadata.Lib/ExtractRoles.fsx
    
    It should overwrite `src/Metadata.Lib/Resources/roles.txt`.
*)

#load "../../.paket/load/net6.0/Dev/dev.group.fsx"

open System.IO
open FSharp.Data

[<Literal>]
let htmlPath = __SOURCE_DIRECTORY__ + @"\bin\page.html"

let outputFile = Path.Join([| __SOURCE_DIRECTORY__; "Resources"; "roles.txt" |])

let p = new HtmlProvider<htmlPath>()
let a1 = p.Tables.``Composer Roles``.Rows |> Array.map (fun r -> r.Role)
let a2 = p.Tables.``Conductor Roles``.Rows |> Array.map (fun r -> r.Role)
let a3 = p.Tables.``Ensemble Roles``.Rows |> Array.map (fun r -> r.Role)
let a4 = p.Tables.``Performer Roles``.Rows |> Array.map (fun r -> r.Role)
let a5 = p.Tables.``Production Roles``.Rows |> Array.map (fun r -> r.Role)
let a6 = p.Tables.``Main Performer Roles``.Rows |> Array.map (fun r -> r.Role)

let roles =
    [| a1; a2; a3; a4; a5; a6 |]
    |> Array.concat
    |> (Set.ofArray >> Set.toArray) // make it unique
    |> Array.sort

File.WriteAllLines(outputFile, roles)

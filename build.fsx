#r "paket:
nuget Fake.Api.GitHub
nuget Fake.Core.UserInput
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.IO.Zip
nuget Fake.Core.Process
nuget Fake.Core.Target
nuget Fake.Tools.Git
nuget FSharpx.Extras 3.1.0 //"
#load ".fake/build.fsx/intellisense.fsx"

open Fake.Api
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators
open FSharpx.Option

let args = Target.getArguments ()
let repoRoot = __SOURCE_DIRECTORY__
let outputDir = repoRoot @@ "output"
let buildDir = outputDir @@ "build"
let distDir = outputDir @@ "dist"
let srcDir = repoRoot @@ "src"
let mainSln = repoRoot @@ "RoonTagger.sln"

let cliProj =
    srcDir
    @@ "RoonTagger.Cli" @@ "RoonTagger.Cli.fsproj"

[<AutoOpen>]
module Helpers =
    open System.Text.RegularExpressions

    let log msg = Trace.log $"===> {msg}"

    let parseVersion =
        function
        | Some version -> version
        | None -> failwith "No Version - either HEAD should be a tag or version override must be specified"

    // Normalizes versions to be valid dotnet version (e.g. v1.2.3 -> 1.2.3).
    let normalizeVersion (version: string) =
        let rgx = Regex "^[a-z,A-Z]+"
        rgx.Replace(version, "")

[<AutoOpen>]
module Outputs =

    type Archive =
        | TarGz
        | Zip

    type Target =
        | NoArch
        | WinX64
        | LinuxX64
        | OsxX64

    type OutputFormat = OutputFormat of Target * Archive

    let toRID target =
        match target with
        | NoArch -> "noarch"
        | WinX64 -> "win-x64"
        | LinuxX64 -> "linux-x64"
        | OsxX64 -> "osx-x64"

[<AutoOpen>]
module GitHelpers =
    open Fake.Tools.Git

    let extractVersionFromTag () =
        let describe = Information.describe repoRoot
        let lastTag = Information.getLastTag ()

        if (describe = lastTag) then
            Some lastTag
        else
            None

    let promptGithubToken () =
        let tokenEnv = "GITHUB_TOKEN"

        let prompt () =
            UserInput.getUserPassword "Github token: "

        Environment.environVarOrNone tokenEnv
        |> Option.defaultWith prompt

[<AutoOpen>]
module Archives =

    let compressZip dirName =
        log $"Creating ZIP archive from '{dirName}'"
        let source = buildDir @@ dirName
        let zipFile = distDir @@ $"{dirName}.zip"
        !! $"{source}/**/*" |> Zip.zip buildDir zipFile

    let compressTarGzip dirName =
        log $"Creating TAR archive from '{dirName}'"
        let tarFile = distDir @@ $"{dirName}.tar.gz"
        let args = [ "czf"; tarFile; dirName ] |> String.concat " "
        let result = Shell.Exec("tar", args, buildDir)

        if result <> 0 then
            failwith $"Failed to create tar from '{dirName}'. Check errors above."

let outputCombinations =
    [ OutputFormat(NoArch, Zip)
      OutputFormat(NoArch, TarGz)
      OutputFormat(WinX64, Zip)
      OutputFormat(OsxX64, Zip)
      OutputFormat(LinuxX64, TarGz) ]

let buildConfig =
    lazy
        match args with
        | Some args ->
            if Seq.contains "--release" args then
                Some DotNet.BuildConfiguration.Release
            else if Seq.contains "--debug" args then
                Some DotNet.BuildConfiguration.Debug
            else
                None
        | None -> None

let versionOverride =
    maybe {
        let! args = args
        let! idx = Seq.tryFindIndex (fun s -> s = "--version") args
        let! version = Seq.tryItem (idx + 1) args

        if System.String.IsNullOrEmpty version then
            return! None
        else
            return version
    }

let version = lazy Option.orElse versionOverride (extractVersionFromTag ())

let publishArch dir rid version =
    let buildConfig =
        Option.defaultValue DotNet.BuildConfiguration.Release buildConfig.Value

    log $"Publishing RoonTagger to runtime {rid}"

    DotNet.publish
        (fun p ->
            { p with
                Runtime = Some rid
                Configuration = buildConfig
                SelfContained = Some true
                OutputPath = Some(buildDir @@ dir)
                MSBuildParams =
                    { p.MSBuildParams with
                        Properties =
                            ("PublishSingleFile", "true")
                            :: ("PublishTrimmed", "true")
                               :: ("PublishReadyToRun", "true")
                                  :: ("Version", version) :: p.MSBuildParams.Properties } })
        cliProj

let publishNoArch dir version =
    let buildConfig =
        Option.defaultValue DotNet.BuildConfiguration.Release buildConfig.Value

    log "Publishing RoonTagger to NoArch"

    DotNet.publish
        (fun p ->
            { p with
                Configuration = buildConfig
                OutputPath = Some(buildDir @@ dir)
                MSBuildParams =
                    { p.MSBuildParams with
                        Properties =
                            // Explicit because  specified as true in the project file.
                            ("PublishTrimmed", "false")
                            :: ("Version", version) :: p.MSBuildParams.Properties } })
        cliProj

Target.create "CodeCheck" ignore
Target.create "Check" ignore

Target.create "Build" (fun _ ->
    let buildConfig =
        Option.defaultValue DotNet.BuildConfiguration.Release buildConfig.Value

    let version = parseVersion version.Value
    let dotNetVersion = normalizeVersion version
    Trace.traceHeader $"Building with '{buildConfig}' profile"

    DotNet.build
        (fun p ->
            { p with
                Configuration = buildConfig
                MSBuildParams =
                    { p.MSBuildParams with
                        Properties =
                            ("Version", dotNetVersion)
                            :: p.MSBuildParams.Properties } })
        mainSln)

Target.create "Clean" (fun _ ->
    Trace.traceHeader "Running dotnet clean"
    let result = DotNet.exec id "clean" mainSln

    if not result.OK then
        failwith $"Error running 'Clean' - check above for errors")

Target.create "FullClean" (fun _ ->
    Trace.traceHeader "Performing Full Clean"

    !! "src/*/obj" ++ "src/*/bin" ++ "output"
    |> File.deleteAll)

Target.create "Lint" (fun _ ->
    Trace.traceHeader "Linting the project"

    let result =
        [ "lint"; mainSln ]
        |> String.concat " "
        |> DotNet.exec id "fsharplint"

    if not result.OK then
        failwith "Linting errors found. See output above")

Target.create "Format" (fun _ ->
    Trace.traceHeader "Check project formatting"

    let result =
        [ repoRoot; "--recurse"; "--check" ]
        |> String.concat " "
        |> DotNet.exec id "fantomas"

    if not result.OK then
        failwith "Formatting errors found. See output above")

Target.create "Test" (fun _ ->
    let buildConfig =
        Option.defaultValue DotNet.BuildConfiguration.Debug buildConfig.Value

    Trace.traceHeader $"Testing with '{buildConfig}' profile"
    DotNet.test (fun s -> { s with Configuration = buildConfig }) mainSln)

Target.create "Scoop" (fun _ ->
    let version = parseVersion version.Value
    let scoopFile = distDir @@ "roon-tagger.json"

    let final =
        $"""{{
  "version": "{version}",
  "description": "A utility to set Roon specific tags in flac files.",
  "homepage": "https://babysnakes.github.io/roon-tagger/",
  "url": "https://github.com/babysnakes/roon-tagger/releases/download/{version}/roon-tagger_{version}_linux-x64.tar.gz",
  "bin": "roon-tagger.exe"
}}
"""

    File.writeString false scoopFile final)

Target.create "Publish" (fun _ ->
    let version = parseVersion version.Value
    let dotNetVersion = normalizeVersion version
    Shell.cleanDir outputDir
    Shell.mkdir buildDir
    Shell.mkdir distDir

    for OutputFormat (target, archive) in outputCombinations do
        let rid = toRID target
        let dir = $"roon-tagger_{version}_{rid}"

        match target with
        | NoArch -> publishNoArch dir dotNetVersion
        | WinX64
        | LinuxX64
        | OsxX64 -> publishArch dir rid dotNetVersion

        match archive with
        | Zip -> compressZip dir
        | TarGz -> compressTarGzip dir)

Target.create "Release" (fun _ ->
    Trace.traceHeader "Creating draft release"

    if version.Value.IsNone then
        failwith "No Version - either HEAD should be a tag or version override must be specified"

    let version = Option.get version.Value

    let files =
        !! "output/dist/*.zip"
        ++ "output/dist/*.tar.gz"
        ++ "output/dist/*.json"

    let contents =
        repoRoot @@ "Resources" @@ "ReleaseTemplate.md"
        |> System.IO.File.ReadLines

    promptGithubToken ()
    |> GitHub.createClientWithToken
    |> GitHub.draftNewRelease "babysnakes" "roon-tagger" version false contents
    |> GitHub.uploadFiles files
    |> Async.RunSynchronously
    |> ignore)

Target.create "Help" (fun _ ->
    let msg =
        """
Fake build for RoonTagger.

Usage:
    * dotnet fake build [ -t task ] [ options ]
    * dotnet fake build --help

tasks:
    * Help      - show help (default task if no task is specified)
    * Test      - run project tests
    * CodeCheck - check linting and formatting
    * Check     - run all project checks
    * Build     - Build the project (by default in Release mode)
    * Publish   - Create archives of roon-tagger for all supported platforms.
    * Release   - Create github draft release (with the published files).

options:
    --version VERSION Override git tag as version.
    --release         Force 'Release' mode on tests.
    --debug           Force 'Debug' mode on Build / Publish / Release.
"""

    Trace.log msg)

// Tasks dependencies
"Clean" ?=> "Build"
"Clean" ?=> "Publish" ==> "Release"
"Clean" ==> "Release"
"Scoop" ==> "Release"
"Format" ==> "Lint" ==> "CodeCheck"
"Test" ==> "Check"
"CodeCheck" ==> "Check"
"Clean" ==> "Check"

Target.runOrDefaultWithArguments "Help"

open Fake.Api
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators
open FSharpx.Option

// Target names - avoid using strings
let targetCodeCheck = "CodeCheck"
let targetCheck = "Check"
let targetBuild = "Build"
let targetClean = "Clean"
let targetFullClean = "FullClean"
let targetRelease = "Release"
let targetScoop = "Scoop"
let targetLint = "Lint"
let targetPublish = "Publish"
let targetFormatCheck = "FormatCheck"
let targetFormat = "Format"
let targetTest = "Test"
let targetHelp = "Help"

// Paths etc
let repoRoot = System.IO.Directory.GetParent(__SOURCE_DIRECTORY__).FullName
let outputDir = repoRoot @@ "output"
let buildDir = outputDir @@ "build"
let distDir = outputDir @@ "dist"
let srcDir = repoRoot @@ "src"
let mainSln = repoRoot @@ "RoonTagger.sln"
let cliProj = srcDir @@ "RoonTagger.Cli" @@ "RoonTagger.Cli.fsproj"

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
        if (describe = lastTag) then Some lastTag else None

    let promptGithubToken () =
        let tokenEnv = "GITHUB_TOKEN"

        let prompt () =
            UserInput.getUserPassword "Github token: "

        Environment.environVarOrNone tokenEnv |> Option.defaultWith prompt

[<AutoOpen>]
module Archives =
    open SharpCompress.Archives
    open SharpCompress.Common
    open SharpCompress.Writers
    open System.IO

    let compressZip dirName =
        log $"Creating ZIP archive from '{dirName}'"
        let source = buildDir @@ dirName
        let zipFile = distDir @@ $"{dirName}.zip"
        !! $"{source}/**/*" |> Zip.zip buildDir zipFile

    let compressTarGzip dirName =
        log $"Creating TAR archive from '{dirName}'"
        let tarFile = distDir @@ $"{dirName}.tar.gz"
        // fsharplint:disable-next-line
        let writerOptions = WriterOptions(CompressionType.GZip, LeaveStreamOpen = true)
        use stream: Stream = File.OpenWrite tarFile
        use writer = WriterFactory.Open(stream, ArchiveType.Tar, writerOptions)
        writer.WriteAll(buildDir, $"{dirName}/*", SearchOption.AllDirectories)

let outputCombinations =
    [ OutputFormat(NoArch, Zip)
      OutputFormat(NoArch, TarGz)
      OutputFormat(WinX64, Zip)
      OutputFormat(OsxX64, Zip)
      OutputFormat(LinuxX64, TarGz) ]

let buildConfig ctx =
    let args = ctx.Context.Arguments

    lazy
        if Seq.contains "--release" args then
            Some DotNet.BuildConfiguration.Release
        else if Seq.contains "--debug" args then
            Some DotNet.BuildConfiguration.Debug
        else
            None

let version ctx =
    let versionOverride =
        maybe {
            let args = ctx.Context.Arguments
            let! idx = Seq.tryFindIndex (fun s -> s = "--version") args
            let! version = Seq.tryItem (idx + 1) args

            if System.String.IsNullOrEmpty version then
                return! None
            else
                return version
        }

    lazy Option.orElse versionOverride (extractVersionFromTag ())

let publishArch dir rid version ctx =
    let buildConfig =
        Option.defaultValue DotNet.BuildConfiguration.Release (buildConfig ctx).Value

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
                        DisableInternalBinLog = true // TODO: see https://github.com/fsprojects/FAKE/issues/2722
                        Properties =
                            ("PublishSingleFile", "true")
                            :: ("PublishTrimmed", "true")
                            :: ("PublishReadyToRun", "true")
                            :: ("Version", version)
                            :: p.MSBuildParams.Properties } })
        cliProj

let publishNoArch dir version ctx =
    let buildConfig =
        Option.defaultValue DotNet.BuildConfiguration.Release (buildConfig ctx).Value

    log "Publishing RoonTagger to NoArch"

    DotNet.publish
        (fun p ->
            { p with
                Configuration = buildConfig
                OutputPath = Some(buildDir @@ dir)
                MSBuildParams =
                    { p.MSBuildParams with
                        DisableInternalBinLog = true // TODO: see https://github.com/fsprojects/FAKE/issues/2722
                        Properties =
                            // Explicit because  specified as true in the project file.
                            ("PublishTrimmed", "false")
                            :: ("Version", version)
                            :: p.MSBuildParams.Properties } })
        cliProj

let runBuild ctx =
    let buildConfig =
        Option.defaultValue DotNet.BuildConfiguration.Release (buildConfig ctx).Value

    let version = parseVersion (version ctx).Value
    let dotNetVersion = normalizeVersion version
    Trace.traceHeader $"Building with '{buildConfig}' profile"

    DotNet.build
        (fun p ->
            { p with
                Configuration = buildConfig
                MSBuildParams =
                    { p.MSBuildParams with
                        DisableInternalBinLog = true // TODO: see https://github.com/fsprojects/FAKE/issues/2722
                        Properties = ("Version", dotNetVersion) :: p.MSBuildParams.Properties } })
        mainSln

let runClean _ =
    Trace.traceHeader "Running dotnet clean"
    let result = DotNet.exec id "clean" mainSln

    if not result.OK then
        failwith $"Error running 'Clean' - check above for errors"

let runFullClean _ =
    Trace.traceHeader "Performing Full Clean"

    !! "src/*/obj" ++ "src/*/bin" ++ "output" |> File.deleteAll

let runLint _ =
    Trace.traceHeader "Linting the project"

    let result =
        [ "lint"; mainSln ]
        |> String.concat " "
        |> DotNet.exec id "fsharplint"

    if not result.OK then
        failwith "Linting errors found. See output above"

let runFormat check =
    Trace.traceHeader "Check project formatting"
    let defaultArgs = if check then [ "--check" ] else []

    let result =
        !! "**/*.fs"
        -- "packages/**/*.fs"
        -- "**/obj/**/*.fs"
        -- "**/bin/**/*.fs"
        |> Seq.toList
        |> List.append defaultArgs
        |> String.concat " "
        |> DotNet.exec id "fantomas"

    if not result.OK then
        failwith "Formatting errors found. See output above"

let runTest ctx =
    let buildConfig =
        Option.defaultValue DotNet.BuildConfiguration.Debug (buildConfig ctx).Value

    Trace.traceHeader $"Testing with '{buildConfig}' profile"

    DotNet.test
        (fun s ->
            { s with
                Configuration = buildConfig
                // TODO: see https://github.com/fsprojects/FAKE/issues/2722
                MSBuildParams =
                    { s.MSBuildParams with
                        DisableInternalBinLog = true } })
        mainSln

let runScoop ctx =
    let version = parseVersion (version ctx).Value
    let scoopFile = distDir @@ "roon-tagger.json"

    let final =
        $"""{{
  "version": "{version}",
  "description": "A utility to set Roon specific tags in flac files.",
  "homepage": "https://babysnakes.github.io/roon-tagger/",
  "architecture": {{
    "64bit": {{
      "url": "https://github.com/babysnakes/roon-tagger/releases/download/{version}/roon-tagger_{version}_win-x64.tar.gz"
    }}
  }},
  "bin": "roon-tagger.exe"
}}
"""

    File.writeString false scoopFile final

let runPublish ctx =
    let version = parseVersion (version ctx).Value
    let dotNetVersion = normalizeVersion version
    Shell.cleanDir outputDir
    Shell.mkdir buildDir
    Shell.mkdir distDir

    for OutputFormat(target, archive) in outputCombinations do
        let rid = toRID target
        let dir = $"roon-tagger_{version}_{rid}"

        match target with
        | NoArch -> publishNoArch dir dotNetVersion ctx
        | WinX64
        | LinuxX64
        | OsxX64 -> publishArch dir rid dotNetVersion ctx

        match archive with
        | Zip -> compressZip dir
        | TarGz -> compressTarGzip dir

let runRelease ctx =
    Trace.traceHeader "Creating draft release"
    let version = version ctx

    if version.Value.IsNone then
        // fsharplint:disable-next-line FailwithBadUsage
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
    |> ignore

let runHelp _ =
    let msg =
        """
Fake build for RoonTagger.

Usage ($BUILD = ./build.sh or .\build.cmd - depending on your OS):
  Show this help:
    $BUILD
  Run task:
    $BUILD -t <task name>  [ options ]
  Get all available targets:
    $BUILD --list

tasks:
    * Help      - show help (default task if no task is specified)
    * Format    - Perform in-place formatting on all F# files in project
    * Test      - run project tests
    * CodeCheck - check linting and formatting
    * Check     - run all project checks
    * Build     - Build the project (by default in Release mode)
    * Publish   - Create archives of roon-tagger for all supported platforms
    * Release   - Create github draft release (with the published files)

options:
    --version VERSION Override git tag as version.
    --release         Force 'Release' mode on tests.
    --debug           Force 'Debug' mode on Build / Publish / Release.
"""

    Trace.traceHeader "Building Project Help"
    Trace.log msg


let initTargets () =

    /// Defines a dependency - y is dependent on x. Finishes the chain.
    let (==>!) x y = x ==> y |> ignore

    /// Defines a soft dependency. x must run before y, if it is present, but y does not require x to be run. Finishes the chain.
    let (?=>!) x y = x ?=> y |> ignore

    Target.create targetCodeCheck ignore
    Target.create targetCheck ignore
    Target.create targetBuild runBuild
    Target.create targetClean runClean
    Target.create targetFullClean runFullClean
    Target.create targetLint runLint
    Target.create targetFormatCheck <| fun _ -> runFormat true
    Target.create targetFormat <| fun _ -> runFormat false
    Target.create targetTest runTest
    Target.create targetScoop runScoop
    Target.create targetPublish runPublish
    Target.create targetRelease runRelease
    Target.create targetHelp runHelp

    // Tasks dependencies
    targetClean ?=>! targetBuild
    targetClean ?=> targetPublish ==>! targetRelease
    targetClean ==>! targetRelease
    targetScoop ==>! targetRelease
    targetFormatCheck ?=> targetLint ==>! targetCodeCheck
    targetFormatCheck ==>! targetCodeCheck
    targetTest ==>! targetCheck
    targetCodeCheck ==>! targetCheck
    targetClean ==>! targetCheck

[<EntryPoint>]
let main argv =
    argv
    |> Array.toList
    |> Context.FakeExecutionContext.Create false "build.fsx"
    |> Context.RuntimeContext.Fake
    |> Context.setExecutionContext

    initTargets ()

    try
        Target.runOrDefaultWithArguments targetHelp
        0 // return an integer exit code
    with ex ->
        Trace.traceError ex.Message
        1

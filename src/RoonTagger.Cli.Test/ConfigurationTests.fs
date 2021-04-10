namespace RoonTagger.Cli.Test

open System
open NUnit.Framework
open FsUnit
open RoonTagger.Cli.Configuration
open TestsUtils

[<TestFixture>]
module ConfigurationTests =

    let tmpDirectory = "test-tmp-dir"
    let configPath = getConfigFilePath tmpDirectory "config" ConfigurationVersion.V1
    let config1 = loadConfigWithDefault "some-file" |> Result.unwrap

    let config2 =
        { Editor = Some { Cmd = "cmd"; Arguments = "arg1 arg2" }
          Log = config1.Log }

    let config3 =
        { Editor = None
          Log = { File = "file"; Level = LogLevel.Info } }

    [<OneTimeSetUp>]
    let setup () =
        IO.Directory.CreateDirectory tmpDirectory
        |> ignore

    [<OneTimeTearDown>]
    let tearDown () =
        IO.Directory.Delete(tmpDirectory, true) |> ignore


    [<Test>]
    let ``getConfigFilePath returns the correct path according to directory and version`` () =
        getConfigFilePath @"c:\some\dir" "config" ConfigurationVersion.V1
        |> should equal @"c:\some\dir\config_v1.json"

    [<Test>]
    let ``Configuration saves and reloads the configuration successfully`` () =
        for c in [ config1; config2 ] do
            saveConfig c configPath |> Result.unwrap

            loadConfig configPath
            |> Result.unwrap
            |> Option.get
            |> should equal c

    [<Test>]
    let ``Config backup is created`` () =
        let path = getConfigFilePath tmpDirectory "c1" ConfigurationVersion.V1
        let backPath = path + ".bak"
        saveConfig config1 path |> Result.unwrap
        saveConfig config2 path |> Result.unwrap

        loadConfig backPath
        |> Result.unwrap
        |> Option.get
        |> should equal config1

    [<Test>]
    let ``Config backups keep overwriting existing backups`` () =
        let path = getConfigFilePath tmpDirectory "c2" ConfigurationVersion.V1
        let backPath = path + ".bak"
        saveConfig config1 path |> Result.unwrap
        saveConfig config2 path |> Result.unwrap
        saveConfig config3 path |> Result.unwrap

        loadConfig backPath
        |> Result.unwrap
        |> Option.get
        |> should equal config2

namespace RoonTagger.Build
{

    using static Help.ConsoleHelpers;
    using Artifacts;
    using Cake.Common.Diagnostics;
    using Cake.Common.IO;
    using Cake.Common.Tools.DotNetCore;
    using Cake.Common.Tools.DotNetCore.Build;
    using Cake.Common.Tools.DotNetCore.Clean;
    using Cake.Common.Tools.DotNetCore.Publish;
    using Cake.Common.Tools.DotNetCore.Test;
    using Cake.Core;
    using Cake.Core.Diagnostics;
    using Cake.Core.IO;
    using Cake.Frosting;
    using System;
    using System.Collections.Generic;

    public static class Program
    {
        public static int Main(string[] args)
        {
            return new CakeHost()
                .UseContext<BuildContext>()
                .Run(args);
        }
    }

    public class BuildContext : FrostingContext
    {
        private const string Solution = "RoonTagger.sln";
        private const string DebugConfig = "Debug";
        private const string ReleaseConfig = "Release";

        public bool CleanRequested { get; init; }
        public string MainSln { get; init; }
        public string CliProj { get; init; }
        public DirectoryPath BuildDir { get; init; }
        public DirectoryPath DistDir { get; init; }
        public DirectoryPath OutputDir { get; init; }
        public string Config { get; init; }
        public string Version { get; init; }
        public List<(Targets, ArchiveTypes)> SupportedArchs { get; init; }

        public BuildContext(ICakeContext context)
            : base(context)
        {
            SupportedArchs = new List<(Targets, ArchiveTypes)> {
                (Targets.NoArch, ArchiveTypes.Zip),
                (Targets.NoArch, ArchiveTypes.TarGz),
                (Targets.Win_X64, ArchiveTypes.Zip),
                (Targets.Osx_X64, ArchiveTypes.Zip),
                (Targets.Linux_X64, ArchiveTypes.TarGz)
            };

            Version = context.Arguments.GetArgument("release-tag"); // WIP
            Config = context.Arguments.HasArgument("release") ? ReleaseConfig : DebugConfig;
            CleanRequested = context.Arguments.HasArgument("clean");
            var projectRootDir = context.Directory("../");
            OutputDir = projectRootDir + context.Directory("output");
            BuildDir = OutputDir + context.Directory("build");
            DistDir = OutputDir + context.Directory("dist");
            var srcDir = projectRootDir + context.Directory("src/");
            MainSln = projectRootDir + context.File(Solution);
            CliProj = srcDir + context.Directory("RoonTagger.Cli") + context.File("RoonTagger.Cli.fsproj");
        }
    }

    [TaskName("Default")]
    [TaskDescription("Detailed build help.")]
    public class DefaultTask : FrostingTask
    {
        public override void Run(ICakeContext context)
        {
            MainHelp();
        }
    }

    [TaskName("Build")]
    [TaskDescription("Builds the main project. Use '--release' if you want to build release executables.")]
    [IsDependentOn(typeof(CleanTask))]
    public sealed class BuildTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var settings = new DotNetCoreBuildSettings
            {
                Configuration = context.Config
            };
            context.Information("Building ({0}) ...\n", context.Config);
            context.DotNetCoreBuild(context.MainSln, settings);
        }
    }

    [TaskName("Check")]
    [TaskDescription("Run various code checks.")]
    [IsDependentOn(typeof(TestTask))]
    public sealed class CheckTask : FrostingTask<BuildContext> { }

    [TaskName("Publish")]
    [TaskDescription("Create compressed distributions for all supported architectures.")]
    [IsDependentOn(typeof(BuildTask))]
    public sealed class PublishTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            if (String.IsNullOrEmpty(context.Version))
            {
                throw new Exception("Version is required for publish!");
            }
            context.CleanDirectory(context.OutputDir);
            context.CreateDirectory(context.DistDir);
            foreach ((Targets target, ArchiveTypes archive) in context.SupportedArchs)
            {
                context.Information($"\nPublishing ({context.Config}) RoonTagger Ver: {context.Version}, Arch: {target.ToRID()}\n");
                var buildDirName = target switch
                {
                    Targets.NoArch => PublishNoArch(context, target),
                    Targets.Linux_X64 or Targets.Osx_X64 or Targets.Win_X64 => PublishArch(context, target),
                    _ => throw new ArgumentOutOfRangeException($"Invalid target: {target}")
                };
                var buildDirPath = context.BuildDir + context.Directory(buildDirName);
                var createArchive = archive.CreateAction();
                var archiveFileName = $"{buildDirName}.{archive.Extension()}";
                var archivePath = context.DistDir + context.Directory(archiveFileName);
                createArchive(buildDirPath, archivePath, context);

                context.CleanDirectory(buildDirPath);
            }
        }

        private string PublishArch(BuildContext context, Targets target)
        {
            var dirName = $"roon-tagger-{context.Version}-{target.ToRID()}";
            var settings = new DotNetCorePublishSettings
            {
                Configuration = context.Config,
                OutputDirectory = context.BuildDir + context.Directory(dirName),
                PublishReadyToRun = target.ReadyToRunSupported(),
                PublishReadyToRunShowWarnings = target.ReadyToRunSupported(),
                SelfContained = true,
                Runtime = target.ToRID()
            };
            context.DotNetCorePublish(context.CliProj, settings);
            return dirName;
        }

        private string PublishNoArch(BuildContext context, Targets target)
        {
            var dirName = $"roon-tagger-{context.Version}-{target.ToRID()}";
            var settings = new DotNetCorePublishSettings
            {
                Configuration = context.Config,
                OutputDirectory = context.BuildDir + context.Directory(dirName)
            };
            context.DotNetCorePublish(context.CliProj, settings);
            return dirName;
        }
    }

    [TaskName("Test")]
    [TaskDescription("Run Tests. Use '--release' if you want to test the release configuration.")]
    [IsDependentOn(typeof(BuildTask))]
    public sealed class TestTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var settings = new DotNetCoreTestSettings
            {
                Configuration = context.Config
            };
            context.Information("Testing ({0}) ...\n", context.Config);
            context.DotNetCoreTest(context.MainSln, settings);
        }
    }

    [TaskName("Clean")]
    [TaskDescription("Cleans previously builds before building. Only runs if '--clean' specified.")]
    public sealed class CleanTask : FrostingTask<BuildContext>
    {
        public override bool ShouldRun(BuildContext context)
        {
            return context.CleanRequested;
        }

        public override void Run(BuildContext context)
        {
            var settings = new DotNetCoreCleanSettings
            {
                Configuration = context.Config
            };
            context.Information("Cleaning ({0}) ...\n", context.Config);
            context.DotNetCoreClean(context.MainSln, settings);
        }
    }
}

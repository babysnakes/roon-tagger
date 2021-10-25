namespace RoonTagger.Build
{

    using static Help.ConsoleHelpers;
    using Cake.Common.Diagnostics;
    using Cake.Common.IO;
    using Cake.Common.Tools.DotNetCore;
    using Cake.Common.Tools.DotNetCore.Build;
    using Cake.Common.Tools.DotNetCore.Clean;
    using Cake.Common.Tools.DotNetCore.Test;
    using Cake.Core;
    using Cake.Core.Diagnostics;
    using Cake.Frosting;

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
        public string SourcesDir { get; init; }
        public string Config { get; init; }

        public BuildContext(ICakeContext context)
            : base(context)
        {
            Config = context.Arguments.HasArgument("release") ? ReleaseConfig : DebugConfig;
            CleanRequested = context.Arguments.HasArgument("clean");
            var projectRootDir = context.Directory("../");
            MainSln = projectRootDir + context.File(Solution);
            SourcesDir = projectRootDir + context.Directory("src");
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

    [TaskName("Check")]
    [TaskDescription("Run various code checks.")]
    [IsDependentOn(typeof(TestTask))]
    public sealed class CheckTask : FrostingTask<BuildContext> { }

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

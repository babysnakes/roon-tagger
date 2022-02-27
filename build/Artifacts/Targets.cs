using System;

namespace RoonTagger.Build.Artifacts
{

    public static class TargetsExtensions
    {
        public static string ToRID(this Targets target) => target switch
        {
            Targets.NoArch => "noarch",
            Targets.Win_X64 => "win-x64",
            Targets.Osx_X64 => "osx-x64",
            Targets.Linux_X64 => "linux-x64",
            _ => throw new ArgumentOutOfRangeException($"Invalid target: {target}")
        };
    }

    public enum Targets
    {
        NoArch,
        Win_X64,
        Linux_X64,
        Osx_X64
    }
}
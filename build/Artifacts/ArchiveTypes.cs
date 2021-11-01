namespace RoonTagger.Build.Artifacts
{

    using System;
    using Cake.Compression;

    public static class ArchiveTypesExtensions
    {

        private static Action<string, string, BuildContext> CreateTarGz = (inputDir, outputFile, context) =>
        {
            context.GZipCompress(inputDir, outputFile);
        };

        private static Action<string, string, BuildContext> CreateZip = (inputDir, outputFile, context) =>
        {
            context.ZipCompress(inputDir, outputFile);
        };

        public static Action<string, string, BuildContext> CreateAction(this ArchiveTypes type) => type switch
        {
            ArchiveTypes.TarGz => CreateTarGz,
            ArchiveTypes.Zip => CreateZip,
            _ => throw new ArgumentOutOfRangeException($"Invalid archive type: {type}")
        };

        public static string Extension(this ArchiveTypes type) => type switch
        {
            ArchiveTypes.TarGz => "tar.gz",
            ArchiveTypes.Zip => "zip",
            _ => throw new ArgumentOutOfRangeException($"Invalid archive type: {type}")
        };

    }

    public enum ArchiveTypes
    {
        TarGz,
        Zip
    }
}
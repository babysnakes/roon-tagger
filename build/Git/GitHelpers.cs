namespace RoonTagger.Build.Git
{

    using LibGit2Sharp;
    using System;
    using System.Linq;

    public static class GitHelpers
    {

        public static string ExtractTagFromHead(string repoPath)
        {
            var repo = new Repository(repoPath);
            var tags = repo.Tags;
            var head = (Commit) repo.Lookup("HEAD");
            try {
                var theTag = tags.First(t => t.IsAnnotated && t.Target.Sha == head.Sha);
                return theTag.FriendlyName;
            }
            catch (ArgumentNullException) {
                throw new Exception("This repo does not seem to have any tags!");
            }
            catch (InvalidOperationException) {
                throw new Exception("No annotated tag seems to point to HEAD!");
            }
        }
    }
}
using System;
using System.IO;
using System.Linq;

namespace Quokka.Rollout
{
    public static class RolloutTools
    {
        static string RecursiveSolutionLocation(string current)
        {
            if (string.IsNullOrWhiteSpace(current))
                return current;

            current = current ?? Directory.GetCurrentDirectory();
            if (Directory.EnumerateFiles(current, "*.sln").Any())
                return current;

            return RecursiveSolutionLocation(Path.GetDirectoryName(current));
        }

        public static string SolutionLocation(string current = null)
        {
            current = current ?? Directory.GetCurrentDirectory();
            var solutionPath = RecursiveSolutionLocation(current);

            return solutionPath ?? throw new Exception($"Solution was not found from location: {current}");
        }
    }
}

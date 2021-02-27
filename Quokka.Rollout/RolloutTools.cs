using System.IO;
using System.Linq;

namespace Quokka.Rollout
{
    public static class RolloutTools
    {
        public static string SolutionLocation(string current = null)
        {
            if (current == "")
                return "";

            current = current ?? Directory.GetCurrentDirectory();
            if (Directory.EnumerateFiles(current, "*.sln").Any())
                return current;

            var parent = Path.GetDirectoryName(current);
            if (parent == current)
                return null;

            return SolutionLocation(Path.GetDirectoryName(current));
        }
    }
}

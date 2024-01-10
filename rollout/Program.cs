using Quokka.Rollout;
using System;
using System.IO;
using System.Linq;

namespace rollout
{
    class Program
    {
        static void Main()
        {
            /*
            ProjectTools.UpdateProjectReferences(
                @"C:\code\qusoc\rtl\rtl.extension\rtl.extension.csproj",
                "Quokka.Extension.Interop",
                "1.0.1.25");
            */
            var solutions = new[]
            {
                "Quokka",
                "Quokka.RTL",
                "Quokka.TCL",
                "Quokka.Extension.Interop"
            };

            RolloutProcess.Run(new RolloutConfig()
            {
                ProjectPath = Path.Combine(RolloutTools.SolutionLocation(), "Quokka.Rollout", "Quokka.Rollout.csproj"),
                LocalPublishLocation = @"c:\code\LocalNuget",
                Nuget = new NugetPushConfig()
                {
                    APIKeyLocation = @"c:\code\LocalNuget\nuget.key.zip",
                    APIKeyLocationRequiredPassword = true
                },
                ReferenceFolders = solutions.Select(s => Path.Combine(@"c:\code", s)).ToList()
            });
        }
    }
}

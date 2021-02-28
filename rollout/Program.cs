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
            var solutions = new[]
            {
                "Quokka",
                "Quokka.RTL",
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

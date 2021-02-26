using Quokka.Rollout;
using System;
using System.IO;

namespace rollout
{
    class Program
    {
        static void Main()
        {
            var projectPath = Path.Combine(RolloutAgent.SolutionLocation(), "Quokka.Rollout", "Quokka.Rollout.csproj");
            var agent = new RolloutAgent(projectPath);
            agent.BuildAndPublishToLocalFolder(@"c:\code\LocalNuget");
        }
    }
}

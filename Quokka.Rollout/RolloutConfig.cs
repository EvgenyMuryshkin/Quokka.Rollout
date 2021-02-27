using System.Collections.Generic;

namespace Quokka.Rollout
{
    public class RolloutConfig
    {
        public List<string> ReferenceFolders { get; set; }
        public List<string> ReferenceProjects { get; set; }
        public string ProjectPath { get; set; }
        public string LocalPublishLocation { get; set; }
        public NugetPushConfig Nuget { get; set; }
    }
}

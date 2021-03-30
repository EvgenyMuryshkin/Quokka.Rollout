using ICSharpCode.SharpZipLib.Zip;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Quokka.Rollout
{
    public static class RolloutProcess
    {
        static NugetPushConfig Validate(NugetPushConfig config)
        {
            if (config == null)
                return null;

            if (!string.IsNullOrWhiteSpace(config.APIKey))
                return config;

            // check if API key is stored in file
            if (File.Exists(config.APIKeyLocation))
            {
                if (config.APIKeyLocationRequiredPassword && string.IsNullOrWhiteSpace(config.APIKeyLocationPassword))
                {
                    config.APIKeyLocationPassword = ConsoleTools.MaskedEntry($"Enter password for {config.APIKeyLocation}:");
                    if (string.IsNullOrWhiteSpace(config.APIKeyLocationPassword))
                        return null;
                }

                var ext = Path.GetExtension(config.APIKeyLocation);
                switch (ext.ToLower())
                {
                    case ".zip":
                    case ".gzip":
                    case ".tar":
                    case ".lzw":
                        using (var fs = File.OpenRead(config.APIKeyLocation))
                        {
                            using (var zip = new ZipInputStream(fs))
                            {
                                zip.Password = config.APIKeyLocationPassword;
                                var apiKeyFile = zip.GetNextEntry();

                                var content = new byte[zip.Length];
                                zip.Read(content, 0, (int)zip.Length);
                                config.APIKey = Encoding.ASCII.GetString(content);
                            }
                        }
                        break;
                    default:
                        // assume plain text
                        config.APIKey = File.ReadAllText(config.APIKeyLocation);
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(config.APIKey))
                config.APIKey = ConsoleTools.MaskedEntry($"Enter API Key:");

            if (string.IsNullOrWhiteSpace(config.APIKey))
                return null;

            return config;
        }

        public static bool Run(RolloutConfig config)
        {
            try
            {
                var nuget = config.Nuget != null ? Validate(config.Nuget) : null;

                var projectPath = config.ProjectPath;
                var agent = new RolloutAgent(projectPath);
                agent.NugetBuild = nuget != null;

                agent.BuildAndPublishToLocalFolder(config.LocalPublishLocation);
                agent.UpgradeReferencesFolders(config.ReferenceFolders);
                agent.UpgradeReferencesProjects(config.ReferenceProjects);

                if (nuget != null)
                    NugetClient.Push(agent.TargetPath, nuget.APIKey);
                else
                    ConsoleTools.Warning("Nuget push skipped");

                return true;
            }
            catch (Exception ex)
            {
                ConsoleTools.Exception(ex);
                return false;
            }
        }
    }
}

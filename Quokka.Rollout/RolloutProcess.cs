using ICSharpCode.SharpZipLib.Zip;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Quokka.Rollout
{
    public static class RolloutProcess
    {
        static string MaskedEntry(string message)
        {
            string result = "";
            Console.WriteLine(message);

            while(true)
            {
                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        Console.WriteLine();
                        return result;
                    case ConsoleKey.Escape:
                        return "";
                    case ConsoleKey.Backspace:
                        if (result.Length > 0)
                        {
                            result = result.Substring(0, result.Length - 1);

                            // clear last *
                            Console.SetCursorPosition(result.Length, Console.CursorTop);
                            Console.Write(" ");
                            Console.SetCursorPosition(result.Length, Console.CursorTop);
                        }
                        break;
                    default:
                        result += key.KeyChar;
                        Console.Write("*");
                        break;
                }
            }
        }

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
                    config.APIKeyLocationPassword = MaskedEntry($"Enter password for {config.APIKeyLocation}:");
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
                config.APIKey = MaskedEntry($"Enter API Key:");

            if (string.IsNullOrWhiteSpace(config.APIKey))
                return null;

            return config;
        }

        public static void Run(RolloutConfig config)
        {
            var projectPath = config.ProjectPath;
            var agent = new RolloutAgent(projectPath);
            agent.BuildAndPublishToLocalFolder(config.LocalPublishLocation);

            agent.UpgradeReferencesFolders(config.ReferenceFolders);
            agent.UpgradeReferencesProjects(config.ReferenceProjects);

            if (config.Nuget != null)
            {
                var nuget = Validate(config.Nuget);
                if (nuget != null)
                    NugetClient.Push(agent.TargetPath, nuget.APIKey);
                else
                    Console.WriteLine("Nuget push skipped");
            }
        }
    }
}

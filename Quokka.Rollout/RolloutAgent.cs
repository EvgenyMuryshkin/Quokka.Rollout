using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Quokka.Rollout
{
    public class RolloutAgent
    {
        readonly string _projectPath;

        public RolloutAgent(string projectPath)
        {
            if (!File.Exists(projectPath))
                throw new FileNotFoundException($"Project file was not found", projectPath);

            _projectPath = projectPath;
        }

        string version = "";
        string ProjectLocation => Path.GetDirectoryName(_projectPath);
        string nupkgName => $"{Path.GetFileNameWithoutExtension(_projectPath)}.{version}.nupkg";
        string nupkgPath => Path.Combine(ProjectLocation, "bin", "Release", nupkgName);

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

        string IncrementVersion()
        {
            var content = File.ReadAllText(_projectPath);
            var xProject = XDocument.Parse(content);
            var currentVersion = xProject.Root
                .Elements("PropertyGroup")
                .SelectMany(g => g.Elements())
                .Where(e => e.Name == "Version")
                .First();

            version = currentVersion.Value;
            var versionParts = version.Split(new[] { '.' });
            versionParts[versionParts.Length - 1] = $"{int.Parse(versionParts.Last()) + 1}";
            version = currentVersion.Value = string.Join(".", versionParts);
            xProject.Save(_projectPath);

            return currentVersion.Value;
        }

        void CopyToPublishLocation(string publishLocation)
        {
            if (!File.Exists(nupkgPath))
                throw new FileNotFoundException(nupkgPath);

            var targetPath = Path.Combine(publishLocation, nupkgName);
            File.Copy(nupkgPath, targetPath);
            Console.WriteLine($"Published to {targetPath}");
        }

        public void BuildAndPublishToLocalFolder(string publishLocation)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            try
            {
                IncrementVersion();

                Directory.SetCurrentDirectory(ProjectLocation);

                var proc = Process.Start(new ProcessStartInfo()
                {
                    FileName = @"dotnet",
                    Arguments = "build -c:Release",
                    UseShellExecute = false
                });
                proc.WaitForExit();
                if (proc.ExitCode != 0)
                    throw new Exception($"Build failed");

                CopyToPublishLocation(publishLocation);
            }
            finally
            {
                Directory.SetCurrentDirectory(currentDirectory);
            }
        }

        public void UpgradeReferencesProjects(List<string> referenceProjects)
        {
            foreach (var proj in referenceProjects)
            {
                if (!File.Exists(proj))
                {
                    Console.WriteLine($"Project file was not found: {proj}");
                    continue;
                }

                var xProj = XDocument.Load(proj);
                var modified = false;
                var itemGroups = xProj.Root.Elements("ItemGroup");
                var packages = itemGroups.SelectMany(g => g.Elements("PackageReference"));
                foreach (var rtl in packages.Where(p => p.Attribute("Include").Value == "Quokka.RTL"))
                {
                    rtl.Attribute("Version").Value = version;
                    modified = true;
                }
                if (modified)
                {
                    Console.WriteLine(proj);
                    xProj.Save(proj);
                }
            }
        }

        public void UpgradeReferencesFolders(List<string> referenceFolders)
        {
            var referenceProjects = referenceFolders.SelectMany(folder =>
            {
                if (!Directory.Exists(folder))
                {
                    Console.WriteLine($"Directory was not found: {folder}");
                    return Enumerable.Empty<string>();
                }

                return Directory.EnumerateFiles(folder, "*.csproj", SearchOption.AllDirectories);
            }).ToList();

            UpgradeReferencesProjects(referenceProjects);
        }
    }
}

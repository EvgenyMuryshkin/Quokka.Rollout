using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
        string nupkgPath
        {
            get
            {
                /*
                if (File.Exists(nuspecPah))
                    return Path.Combine(ProjectLocation, nupkgName);
                */
                return Path.Combine(ProjectLocation, "bin", "Release", nupkgName);
            }
        }

        string nuspecPah => Path.Combine(Path.GetDirectoryName(_projectPath), $"{Path.GetFileNameWithoutExtension(_projectPath)}.nuspec");
        public string TargetPath { get; set; }
        public bool NugetBuild { get; set; }

        string IncrementProjectVersion()
        {
            var content = File.ReadAllText(_projectPath);
            var xProject = XDocument.Parse(content);

            var versionPropNames = new HashSet<string>() { "AssemblyVersion", "FileVersion", "Version" };
            var versionProps = versionPropNames.Select(p => xProject.Root
                .Elements("PropertyGroup")
                .SelectMany(g => g.Elements())
                .Where(e => e.Name == p)
                .FirstOrDefault())
                .Where(p => p != null);                

            // all versions should be the same
            var group = versionProps.GroupBy(p => p.Value);
            if (group.Count() != 1)
            {
                throw new Exception($"Multiple versions are set on assembly: {string.Join(", ", versionProps.Select(v => v.Value))}");
            }

            version = versionProps.First().Value;
            var versionParts = version.Split(new[] { '.' }).Select(s => int.Parse(s)).ToList();

            if (NugetBuild)
                versionParts[versionParts.Count - 2]++;

            versionParts[versionParts.Count - 1]++;

            version = string.Join(".", versionParts);

            foreach (var currentVersion in versionProps)
            {
                currentVersion.Value = version;
            }

            xProject.Save(_projectPath);

            return version;
        }

        void IncrementNuspecVersion()
        {
            if (!File.Exists(nuspecPah))
                return;

            var content = File.ReadAllText(nuspecPah);
            var xProject = XDocument.Parse(content);
            var versionProp = xProject.Root
                .Elements("metadata")
                .SelectMany(g => g.Elements())
                .Where(e => e.Name == "version")
                .First();

            versionProp.Value = version;
            xProject.Save(nuspecPah);
        }

        string IncrementVersion()
        {
            IncrementProjectVersion();
            IncrementNuspecVersion();

            return version;
        }

        void CopyToPublishLocation(string publishLocation)
        {
            if (!File.Exists(nupkgPath))
                throw new FileNotFoundException(nupkgPath);

            if (string.IsNullOrWhiteSpace(TargetPath))
                TargetPath = Path.Combine(publishLocation, nupkgName);

            File.Copy(nupkgPath, TargetPath);
            ConsoleTools.Info($"Published to {TargetPath}");
        }

        public void BuildAndPublishToLocalFolder(string publishLocation)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            try
            {
                IncrementVersion();

                Directory.SetCurrentDirectory(ProjectLocation);

                var buildProc = Process.Start(new ProcessStartInfo()
                {
                    FileName = @"dotnet",
                    Arguments = "build -c:Release",
                    UseShellExecute = false
                });

                buildProc.WaitForExit();
                if (buildProc.ExitCode != 0)
                    throw new Exception($"Build failed");


                var packProc = Process.Start(new ProcessStartInfo()
                {
                    FileName = @"dotnet",
                    Arguments = "pack -c:Release",
                    UseShellExecute = false
                });

                packProc.WaitForExit();
                if (packProc.ExitCode != 0)
                    throw new Exception($"Pack failed");

                CopyToPublishLocation(publishLocation);
            }
            finally
            {
                Directory.SetCurrentDirectory(currentDirectory);
            }
        }

        public void UpgradeReferencesProjects(List<string> referenceProjects)
        {
            if (referenceProjects == null || !referenceProjects.Any())
                return;

            foreach (var proj in referenceProjects)
            {
                if (!File.Exists(proj))
                {
                    ConsoleTools.Warning($"Project file was not found: {proj}");
                    continue;
                }

                var xProj = XDocument.Load(proj);
                var modified = false;
                var itemGroups = xProj.Root.Elements("ItemGroup");
                var packages = itemGroups.SelectMany(g => g.Elements("PackageReference"));
                var packageName = Path.GetFileNameWithoutExtension(_projectPath);
                foreach (var rtl in packages.Where(p => p.Attribute("Include").Value == packageName))
                {
                    rtl.Attribute("Version").Value = version;
                    modified = true;
                }

                if (modified)
                {
                    ConsoleTools.Info($"Project updated: {proj}");
                    xProj.Save(proj);
                }
            }
        }

        public void UpgradeReferencesFolders(List<string> referenceFolders)
        {
            if (referenceFolders == null || !referenceFolders.Any())
                return;

            var referenceProjects = referenceFolders.SelectMany(folder =>
            {
                if (!Directory.Exists(folder))
                {
                    ConsoleTools.Warning($"Directory was not found: {folder}");
                    return Enumerable.Empty<string>();
                }

                return Directory.EnumerateFiles(folder, "*.csproj", SearchOption.AllDirectories);
            }).ToList();

            UpgradeReferencesProjects(referenceProjects);
        }
    }
}

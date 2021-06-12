using System;
using System.Linq;
using System.Xml.Linq;

namespace Quokka.Rollout
{
    public static class ProjectTools
    {
        public static bool UpdateProjectReferences(string projectPath, string packageName, string packageVersion)
        {
            var xProj = XDocument.Load(projectPath);
            Func<string, XName> name = (n) => XName.Get(n, xProj.Root.Name.NamespaceName);

            var modified = false;
            var itemGroups = xProj.Root.Elements(name("ItemGroup"));
            var packages = itemGroups.SelectMany(g => g.Elements(name("PackageReference")));

            var xVersionName = name("Version");
            foreach (var rtl in packages.Where(p => p.Attribute("Include")?.Value == packageName))
            {
                var versionAttr = rtl.Attribute("Version");
                if (versionAttr != null)
                {
                    versionAttr.Value = packageVersion;
                }
                else
                {
                    rtl.RemoveNodes();
                    rtl.Add(new XElement(xVersionName, packageVersion));
                }

                modified = true;
            }

            if (modified)
            {
                xProj.Save(projectPath);
            }

            return modified;
        }
    }
}

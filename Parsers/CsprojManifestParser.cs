using System.Xml.Linq;
using dep_drift_checker.Models;

namespace dep_drift_checker.Parsers;

public class CsprojManifestParser : IManifestParser
{
    public ManifestType ManifestType => ManifestType.NuGet;

    public Task<IReadOnlyList<Dependency>> ParseAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Task.FromResult<IReadOnlyList<Dependency>>(Array.Empty<Dependency>());
        }

        try
        {
            var document = XDocument.Parse(content);
            var dependencies = new List<Dependency>();

            var packageReferences = document.Descendants()
                .Where(e => e.Name.LocalName == "PackageReference");

            foreach (var packageRef in packageReferences)
            {
                var name = packageRef.Attribute("Include")?.Value;
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                var version = GetVersion(packageRef);
                if (string.IsNullOrEmpty(version))
                {
                    continue;
                }

                dependencies.Add(new Dependency
                {
                    Name = name,
                    CurrentVersion = NormalizeVersion(version)
                });
            }

            return Task.FromResult<IReadOnlyList<Dependency>>(dependencies);
        }
        catch (System.Xml.XmlException)
        {
            return Task.FromResult<IReadOnlyList<Dependency>>(Array.Empty<Dependency>());
        }
    }

    private static string? GetVersion(XElement packageRef)
    {
        var versionAttr = packageRef.Attribute("Version")?.Value;
        if (!string.IsNullOrEmpty(versionAttr))
        {
            return versionAttr;
        }

        var versionElement = packageRef.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "Version");
        return versionElement?.Value;
    }

    private static string NormalizeVersion(string version)
    {
        var trimmed = version.Trim();

        if (trimmed.StartsWith('[') || trimmed.StartsWith('('))
        {
            return ExtractVersionFromRange(trimmed);
        }

        return trimmed;
    }

    private static string ExtractVersionFromRange(string range)
    {
        var inner = range.Trim('[', ']', '(', ')');
        var parts = inner.Split(',');

        var firstPart = parts[0].Trim();
        if (!string.IsNullOrEmpty(firstPart))
        {
            return firstPart;
        }

        if (parts.Length > 1)
        {
            return parts[1].Trim();
        }

        return range;
    }
}

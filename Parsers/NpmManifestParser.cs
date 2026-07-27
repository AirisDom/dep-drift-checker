using System.Text.Json;
using dep_drift_checker.Models;

namespace dep_drift_checker.Parsers;

public class NpmManifestParser : IManifestParser
{
    public ManifestType ManifestType => ManifestType.Npm;

    public Task<IReadOnlyList<Dependency>> ParseAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Task.FromResult<IReadOnlyList<Dependency>>(Array.Empty<Dependency>());
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            var dependencies = new List<Dependency>();

            if (root.TryGetProperty("dependencies", out var deps))
            {
                ParseDependencySection(deps, dependencies);
            }

            if (root.TryGetProperty("devDependencies", out var devDeps))
            {
                ParseDependencySection(devDeps, dependencies);
            }

            return Task.FromResult<IReadOnlyList<Dependency>>(dependencies);
        }
        catch (JsonException)
        {
            return Task.FromResult<IReadOnlyList<Dependency>>(Array.Empty<Dependency>());
        }
    }

    private static void ParseDependencySection(JsonElement section, List<Dependency> dependencies)
    {
        if (section.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var property in section.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var version = property.Value.GetString();
            if (string.IsNullOrEmpty(version))
            {
                continue;
            }

            dependencies.Add(new Dependency
            {
                Name = property.Name,
                CurrentVersion = NormalizeVersion(version)
            });
        }
    }

    private static string NormalizeVersion(string version)
    {
        var trimmed = version.Trim();
        if (trimmed.StartsWith('^') || trimmed.StartsWith('~'))
        {
            return trimmed[1..];
        }
        return trimmed;
    }
}

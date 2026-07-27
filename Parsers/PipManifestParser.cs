using dep_drift_checker.Models;

namespace dep_drift_checker.Parsers;

public class PipManifestParser : IManifestParser
{
    public ManifestType ManifestType => ManifestType.Pip;

    public Task<IReadOnlyList<Dependency>> ParseAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Task.FromResult<IReadOnlyList<Dependency>>(Array.Empty<Dependency>());
        }

        var dependencies = new List<Dependency>();
        var lines = content.Split('\n');

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            if (line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith("-r") || line.StartsWith("--requirement"))
            {
                continue;
            }

            if (line.StartsWith("-"))
            {
                continue;
            }

            var dependency = ParseDependencyLine(line);
            if (dependency != null)
            {
                dependencies.Add(dependency);
            }
        }

        return Task.FromResult<IReadOnlyList<Dependency>>(dependencies);
    }

    private static Dependency? ParseDependencyLine(string line)
    {
        var commentIndex = line.IndexOf('#');
        if (commentIndex >= 0)
        {
            line = line[..commentIndex].Trim();
        }

        if (string.IsNullOrEmpty(line))
        {
            return null;
        }

        var (name, version) = ExtractNameAndVersion(line);

        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        return new Dependency
        {
            Name = name,
            CurrentVersion = version ?? string.Empty
        };
    }

    private static (string? name, string? version) ExtractNameAndVersion(string line)
    {
        foreach (var op in new[] { "===", "==", "~=", "!=", "<=", ">=", "<", ">" })
        {
            var opIndex = line.IndexOf(op, StringComparison.Ordinal);
            if (opIndex > 0)
            {
                var name = StripExtras(line[..opIndex].Trim());
                var version = line[(opIndex + op.Length)..].Trim();

                var commaIndex = version.IndexOf(',');
                if (commaIndex >= 0)
                {
                    version = version[..commaIndex].Trim();
                }

                return (NormalizeName(name), version);
            }
        }

        var bracketIndex = line.IndexOf('[');
        if (bracketIndex > 0)
        {
            return (NormalizeName(line[..bracketIndex].Trim()), null);
        }

        return (NormalizeName(line), null);
    }

    private static string StripExtras(string name)
    {
        var bracketIndex = name.IndexOf('[');
        return bracketIndex > 0 ? name[..bracketIndex] : name;
    }

    private static string NormalizeName(string name)
    {
        return name.Replace('_', '-').ToLowerInvariant();
    }
}

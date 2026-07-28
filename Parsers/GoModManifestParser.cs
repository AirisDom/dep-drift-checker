using dep_drift_checker.Models;

namespace dep_drift_checker.Parsers;

public class GoModManifestParser : IManifestParser
{
    public ManifestType ManifestType => ManifestType.Go;

    public Task<IReadOnlyList<Dependency>> ParseAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Task.FromResult<IReadOnlyList<Dependency>>(Array.Empty<Dependency>());
        }

        var dependencies = new List<Dependency>();
        var lines = content.Split('\n');
        var insideRequireBlock = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            if (line.StartsWith("replace") || line.StartsWith("exclude") || line.StartsWith("retract"))
            {
                if (line.Contains('('))
                {
                    SkipToBlockEnd(lines, ref insideRequireBlock);
                }
                continue;
            }

            if (line.StartsWith("require ("))
            {
                insideRequireBlock = true;
                continue;
            }

            if (line.StartsWith("require("))
            {
                insideRequireBlock = true;
                continue;
            }

            if (line == "require (")
            {
                insideRequireBlock = true;
                continue;
            }

            if (insideRequireBlock)
            {
                if (line == ")")
                {
                    insideRequireBlock = false;
                    continue;
                }

                var dependency = ParseRequireLine(line);
                if (dependency != null)
                {
                    dependencies.Add(dependency);
                }
            }
            else if (line.StartsWith("require "))
            {
                var requireContent = line["require ".Length..].Trim();
                var dependency = ParseRequireLine(requireContent);
                if (dependency != null)
                {
                    dependencies.Add(dependency);
                }
            }
        }

        return Task.FromResult<IReadOnlyList<Dependency>>(dependencies);
    }

    private static void SkipToBlockEnd(string[] lines, ref bool insideBlock)
    {
        insideBlock = false;
    }

    private static Dependency? ParseRequireLine(string line)
    {
        var commentIndex = line.IndexOf("//", StringComparison.Ordinal);
        if (commentIndex >= 0)
        {
            line = line[..commentIndex].Trim();
        }

        if (string.IsNullOrEmpty(line))
        {
            return null;
        }

        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return null;
        }

        var modulePath = parts[0];
        var version = NormalizeVersion(parts[1]);

        return new Dependency
        {
            Name = modulePath,
            CurrentVersion = version
        };
    }

    private static string NormalizeVersion(string version)
    {
        var v = version.Trim();

        if (v.StartsWith('v'))
        {
            v = v[1..];
        }

        var plusIndex = v.IndexOf('+');
        if (plusIndex > 0)
        {
            v = v[..plusIndex];
        }

        return v;
    }
}

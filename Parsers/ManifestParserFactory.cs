using dep_drift_checker.Models;

namespace dep_drift_checker.Parsers;

public class ManifestParserFactory
{
    private readonly NpmManifestParser _npmParser = new();
    private readonly CsprojManifestParser _csprojParser = new();
    private readonly PipManifestParser _pipParser = new();
    private readonly GoModManifestParser _goModParser = new();

    public IManifestParser GetParser(ManifestType manifestType)
    {
        return manifestType switch
        {
            ManifestType.Npm => _npmParser,
            ManifestType.NuGet => _csprojParser,
            ManifestType.Pip => _pipParser,
            ManifestType.Go => _goModParser,
            _ => throw new ArgumentException($"Unsupported manifest type: {manifestType}", nameof(manifestType))
        };
    }

    public IManifestParser GetParser(string filename)
    {
        var manifestType = DetectManifestTypeFromFilename(filename);
        return GetParser(manifestType);
    }

    public IManifestParser GetParserFromContent(string content)
    {
        var manifestType = DetectManifestTypeFromContent(content);
        return GetParser(manifestType);
    }

    public ManifestType DetectManifestType(string? filename, string? content)
    {
        if (!string.IsNullOrWhiteSpace(filename))
        {
            var detected = TryDetectManifestTypeFromFilename(filename);
            if (detected.HasValue)
            {
                return detected.Value;
            }
        }

        if (!string.IsNullOrWhiteSpace(content))
        {
            var detected = TryDetectManifestTypeFromContent(content);
            if (detected.HasValue)
            {
                return detected.Value;
            }
        }

        throw new InvalidOperationException("Unable to detect manifest type from filename or content");
    }

    public ManifestType DetectManifestTypeFromFilename(string filename)
    {
        var detected = TryDetectManifestTypeFromFilename(filename);
        if (detected.HasValue)
        {
            return detected.Value;
        }

        throw new ArgumentException($"Unable to detect manifest type from filename: {filename}", nameof(filename));
    }

    public ManifestType DetectManifestTypeFromContent(string content)
    {
        var detected = TryDetectManifestTypeFromContent(content);
        if (detected.HasValue)
        {
            return detected.Value;
        }

        throw new InvalidOperationException("Unable to detect manifest type from content");
    }

    private static ManifestType? TryDetectManifestTypeFromFilename(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return null;
        }

        var normalized = filename.Replace('\\', '/');
        var name = Path.GetFileName(normalized).ToLowerInvariant();

        if (name == "package.json")
        {
            return ManifestType.Npm;
        }

        if (name.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return ManifestType.NuGet;
        }

        if (name == "requirements.txt" ||
            name.StartsWith("requirements-", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("requirements_", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            return ManifestType.Pip;
        }

        if (name == "go.mod")
        {
            return ManifestType.Go;
        }

        return null;
    }

    private static ManifestType? TryDetectManifestTypeFromContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var trimmed = content.TrimStart();

        if (trimmed.StartsWith('{'))
        {
            if (content.Contains("\"dependencies\"") ||
                content.Contains("\"devDependencies\"") ||
                content.Contains("\"name\"") && content.Contains("\"version\""))
            {
                return ManifestType.Npm;
            }
        }

        if (trimmed.StartsWith('<'))
        {
            if (content.Contains("<PackageReference") ||
                content.Contains("<Project") && content.Contains("Sdk="))
            {
                return ManifestType.NuGet;
            }
        }

        if (content.Contains("module ") && content.Contains("go "))
        {
            return ManifestType.Go;
        }

        if (content.StartsWith("module ", StringComparison.Ordinal) ||
            content.Contains("\nmodule ") ||
            content.Contains("require ") ||
            content.Contains("require ("))
        {
            if (content.Contains("go ") || content.Contains("require"))
            {
                return ManifestType.Go;
            }
        }

        var lines = content.Split('\n');
        var pipPatternCount = 0;
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
            {
                continue;
            }

            if (line.Contains("==") || line.Contains(">=") || line.Contains("<=") || line.Contains("~="))
            {
                pipPatternCount++;
            }
            else if (line.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '[' || c == ']'))
            {
                pipPatternCount++;
            }

            if (pipPatternCount >= 2)
            {
                return ManifestType.Pip;
            }
        }

        if (pipPatternCount == 1 && lines.Length <= 3)
        {
            return ManifestType.Pip;
        }

        return null;
    }
}

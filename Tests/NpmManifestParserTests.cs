using dep_drift_checker.Models;
using dep_drift_checker.Parsers;

namespace dep_drift_checker.Tests;

public class NpmManifestParserTests
{
    private readonly NpmManifestParser _parser = new();

    [Fact]
    public void ManifestType_ReturnsNpm()
    {
        Assert.Equal(ManifestType.Npm, _parser.ManifestType);
    }

    [Fact]
    public async Task ParseAsync_WithValidPackageJson_ParsesDependencies()
    {
        var content = """
        {
            "name": "my-app",
            "version": "1.0.0",
            "dependencies": {
                "express": "^4.18.2",
                "lodash": "~4.17.21"
            }
        }
        """;

        var result = await _parser.ParseAsync(content);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.Name == "express" && d.CurrentVersion == "4.18.2");
        Assert.Contains(result, d => d.Name == "lodash" && d.CurrentVersion == "4.17.21");
    }

    [Fact]
    public async Task ParseAsync_WithDevDependencies_ParsesBothSections()
    {
        var content = """
        {
            "name": "my-app",
            "dependencies": {
                "react": "^18.2.0"
            },
            "devDependencies": {
                "jest": "^29.5.0",
                "typescript": "^5.0.0"
            }
        }
        """;

        var result = await _parser.ParseAsync(content);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, d => d.Name == "react" && d.CurrentVersion == "18.2.0");
        Assert.Contains(result, d => d.Name == "jest" && d.CurrentVersion == "29.5.0");
        Assert.Contains(result, d => d.Name == "typescript" && d.CurrentVersion == "5.0.0");
    }

    [Fact]
    public async Task ParseAsync_WithOnlyDevDependencies_ParsesDevDependencies()
    {
        var content = """
        {
            "name": "my-lib",
            "devDependencies": {
                "eslint": "^8.40.0"
            }
        }
        """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("eslint", result[0].Name);
        Assert.Equal("8.40.0", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithNoDependencies_ReturnsEmptyList()
    {
        var content = """
        {
            "name": "empty-project",
            "version": "1.0.0"
        }
        """;

        var result = await _parser.ParseAsync(content);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseAsync_WithExactVersion_PreservesVersion()
    {
        var content = """
        {
            "dependencies": {
                "exact-package": "1.2.3"
            }
        }
        """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("1.2.3", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithEmptyContent_ReturnsEmptyList()
    {
        var result = await _parser.ParseAsync("");

        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseAsync_WithNullContent_ReturnsEmptyList()
    {
        var result = await _parser.ParseAsync(null!);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseAsync_WithWhitespaceContent_ReturnsEmptyList()
    {
        var result = await _parser.ParseAsync("   \n\t  ");

        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseAsync_WithMalformedJson_ReturnsEmptyList()
    {
        var content = "{ invalid json }";

        var result = await _parser.ParseAsync(content);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseAsync_WithIncompleteJson_ReturnsEmptyList()
    {
        var content = """
        {
            "dependencies": {
                "package": "1.0.0"
        """;

        var result = await _parser.ParseAsync(content);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseAsync_WithDependenciesAsArray_ReturnsEmptyList()
    {
        var content = """
        {
            "dependencies": ["package1", "package2"]
        }
        """;

        var result = await _parser.ParseAsync(content);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseAsync_WithNonStringVersionValue_SkipsDependency()
    {
        var content = """
        {
            "dependencies": {
                "valid-package": "1.0.0",
                "invalid-package": 123,
                "another-valid": "2.0.0"
            }
        }
        """;

        var result = await _parser.ParseAsync(content);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.Name == "valid-package");
        Assert.Contains(result, d => d.Name == "another-valid");
        Assert.DoesNotContain(result, d => d.Name == "invalid-package");
    }

    [Fact]
    public async Task ParseAsync_WithEmptyVersionString_SkipsDependency()
    {
        var content = """
        {
            "dependencies": {
                "valid-package": "1.0.0",
                "empty-version": ""
            }
        }
        """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("valid-package", result[0].Name);
    }

    [Fact]
    public async Task ParseAsync_WithScopedPackages_ParsesCorrectly()
    {
        var content = """
        {
            "dependencies": {
                "@types/node": "^18.0.0",
                "@angular/core": "^16.0.0"
            }
        }
        """;

        var result = await _parser.ParseAsync(content);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.Name == "@types/node" && d.CurrentVersion == "18.0.0");
        Assert.Contains(result, d => d.Name == "@angular/core" && d.CurrentVersion == "16.0.0");
    }

    [Fact]
    public async Task ParseAsync_WithVersionRanges_NormalizesVersion()
    {
        var content = """
        {
            "dependencies": {
                "caret": "^1.2.3",
                "tilde": "~4.5.6",
                "exact": "7.8.9"
            }
        }
        """;

        var result = await _parser.ParseAsync(content);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, d => d.Name == "caret" && d.CurrentVersion == "1.2.3");
        Assert.Contains(result, d => d.Name == "tilde" && d.CurrentVersion == "4.5.6");
        Assert.Contains(result, d => d.Name == "exact" && d.CurrentVersion == "7.8.9");
    }
}

using dep_drift_checker.Models;
using dep_drift_checker.Parsers;

namespace dep_drift_checker.Tests;

public class PipManifestParserTests
{
    private readonly PipManifestParser _parser = new();

    [Fact]
    public void ManifestType_ReturnsPip()
    {
        Assert.Equal(ManifestType.Pip, _parser.ManifestType);
    }

    [Fact]
    public async Task ParseAsync_WithPinnedVersions_ParsesDependencies()
    {
        var content = """
            flask==2.3.2
            requests==2.31.0
            numpy==1.24.3
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, d => d.Name == "flask" && d.CurrentVersion == "2.3.2");
        Assert.Contains(result, d => d.Name == "requests" && d.CurrentVersion == "2.31.0");
        Assert.Contains(result, d => d.Name == "numpy" && d.CurrentVersion == "1.24.3");
    }

    [Fact]
    public async Task ParseAsync_WithMinimumVersions_ParsesDependencies()
    {
        var content = """
            django>=4.2.0
            celery>=5.3.0
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.Name == "django" && d.CurrentVersion == "4.2.0");
        Assert.Contains(result, d => d.Name == "celery" && d.CurrentVersion == "5.3.0");
    }

    [Fact]
    public async Task ParseAsync_WithBarePackageNames_ParsesWithEmptyVersion()
    {
        var content = """
            flask
            requests
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.Name == "flask" && d.CurrentVersion == "");
        Assert.Contains(result, d => d.Name == "requests" && d.CurrentVersion == "");
    }

    [Fact]
    public async Task ParseAsync_WithComments_IgnoresCommentLines()
    {
        var content = """
            # This is a comment
            flask==2.3.2
            # Another comment
            requests==2.31.0
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.Name == "flask");
        Assert.Contains(result, d => d.Name == "requests");
    }

    [Fact]
    public async Task ParseAsync_WithInlineComments_StripsComments()
    {
        var content = """
            flask==2.3.2  # web framework
            requests==2.31.0  # HTTP library
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.Name == "flask" && d.CurrentVersion == "2.3.2");
        Assert.Contains(result, d => d.Name == "requests" && d.CurrentVersion == "2.31.0");
    }

    [Fact]
    public async Task ParseAsync_WithRequirementIncludes_IgnoresIncludeLines()
    {
        var content = """
            -r base.txt
            flask==2.3.2
            --requirement dev.txt
            requests==2.31.0
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.Name == "flask");
        Assert.Contains(result, d => d.Name == "requests");
    }

    [Fact]
    public async Task ParseAsync_WithEmptyLines_IgnoresEmptyLines()
    {
        var content = """
            flask==2.3.2

            requests==2.31.0

            """;

        var result = await _parser.ParseAsync(content);

        Assert.Equal(2, result.Count);
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
    public async Task ParseAsync_WithCompatibleRelease_ParsesVersion()
    {
        var content = "requests~=2.31.0";

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("requests", result[0].Name);
        Assert.Equal("2.31.0", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithArbitraryEquality_ParsesVersion()
    {
        var content = "package===1.0.0";

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("package", result[0].Name);
        Assert.Equal("1.0.0", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithExclusion_ParsesVersion()
    {
        var content = "package!=1.0.0";

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("package", result[0].Name);
        Assert.Equal("1.0.0", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithLessThan_ParsesVersion()
    {
        var content = "package<2.0.0";

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("package", result[0].Name);
        Assert.Equal("2.0.0", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithLessThanOrEqual_ParsesVersion()
    {
        var content = "package<=2.0.0";

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("package", result[0].Name);
        Assert.Equal("2.0.0", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithGreaterThan_ParsesVersion()
    {
        var content = "package>1.0.0";

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("package", result[0].Name);
        Assert.Equal("1.0.0", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithVersionRange_ParsesFirstVersion()
    {
        var content = "package>=1.0.0,<2.0.0";

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("package", result[0].Name);
        Assert.Equal("1.0.0", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithExtras_ParsesPackageName()
    {
        var content = "requests[security]==2.31.0";

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("requests", result[0].Name);
        Assert.Equal("2.31.0", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithExtrasNoVersion_ParsesPackageName()
    {
        var content = "requests[security,socks]";

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("requests", result[0].Name);
        Assert.Equal("", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithUnderscoresInName_NormalizesToHyphens()
    {
        var content = "my_package==1.0.0";

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("my-package", result[0].Name);
    }

    [Fact]
    public async Task ParseAsync_WithMixedCaseNames_NormalizesToLowercase()
    {
        var content = "Flask==2.3.2";

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("flask", result[0].Name);
    }

    [Fact]
    public async Task ParseAsync_WithEditable_IgnoresEditableLine()
    {
        var content = """
            -e git+https://github.com/user/repo.git#egg=package
            flask==2.3.2
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("flask", result[0].Name);
    }

    [Fact]
    public async Task ParseAsync_WithIndexUrl_IgnoresIndexUrlLine()
    {
        var content = """
            -i https://pypi.org/simple
            flask==2.3.2
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("flask", result[0].Name);
    }

    [Fact]
    public async Task ParseAsync_WithExtraIndexUrl_IgnoresExtraIndexUrlLine()
    {
        var content = """
            --extra-index-url https://example.com/simple
            flask==2.3.2
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("flask", result[0].Name);
    }

    [Fact]
    public async Task ParseAsync_WithPreReleaseVersion_PreservesPreReleaseTag()
    {
        var content = "package==1.0.0rc1";

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("1.0.0rc1", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithDevVersion_PreservesDevTag()
    {
        var content = "package==1.0.0.dev1";

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("1.0.0.dev1", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithPostVersion_PreservesPostTag()
    {
        var content = "package==1.0.0.post1";

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("1.0.0.post1", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_RealWorldRequirements_ParsesAllPackages()
    {
        var content = """
            # Web Framework
            Flask==2.3.2
            Werkzeug>=2.3.0

            # Database
            SQLAlchemy==2.0.19
            psycopg2-binary>=2.9.6

            # Development dependencies
            -r dev-requirements.txt

            # Testing
            pytest==7.4.0  # test runner
            pytest-cov
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Equal(6, result.Count);
        Assert.Contains(result, d => d.Name == "flask" && d.CurrentVersion == "2.3.2");
        Assert.Contains(result, d => d.Name == "werkzeug" && d.CurrentVersion == "2.3.0");
        Assert.Contains(result, d => d.Name == "sqlalchemy" && d.CurrentVersion == "2.0.19");
        Assert.Contains(result, d => d.Name == "psycopg2-binary" && d.CurrentVersion == "2.9.6");
        Assert.Contains(result, d => d.Name == "pytest" && d.CurrentVersion == "7.4.0");
        Assert.Contains(result, d => d.Name == "pytest-cov" && d.CurrentVersion == "");
    }

    [Fact]
    public async Task ParseAsync_WithWhitespaceAroundOperator_ParsesCorrectly()
    {
        var content = "flask == 2.3.2";

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("flask", result[0].Name);
        Assert.Equal("2.3.2", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithLeadingWhitespace_ParsesCorrectly()
    {
        var content = "    flask==2.3.2";

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("flask", result[0].Name);
        Assert.Equal("2.3.2", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithWindowsLineEndings_ParsesCorrectly()
    {
        var content = "flask==2.3.2\r\nrequests==2.31.0\r\n";

        var result = await _parser.ParseAsync(content);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.Name == "flask");
        Assert.Contains(result, d => d.Name == "requests");
    }
}

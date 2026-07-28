using dep_drift_checker.Models;
using dep_drift_checker.Parsers;

namespace dep_drift_checker.Tests;

public class GoModManifestParserTests
{
    private readonly GoModManifestParser _parser = new();

    [Fact]
    public void ManifestType_ReturnsGo()
    {
        Assert.Equal(ManifestType.Go, _parser.ManifestType);
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
    public async Task ParseAsync_WithSingleLineRequire_ParsesDependency()
    {
        var content = """
            module example.com/mymodule

            go 1.21

            require github.com/gin-gonic/gin v1.9.1
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("github.com/gin-gonic/gin", result[0].Name);
        Assert.Equal("1.9.1", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithMultipleSingleLineRequires_ParsesAllDependencies()
    {
        var content = """
            module example.com/mymodule

            go 1.21

            require github.com/gin-gonic/gin v1.9.1
            require github.com/stretchr/testify v1.8.4
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.Name == "github.com/gin-gonic/gin" && d.CurrentVersion == "1.9.1");
        Assert.Contains(result, d => d.Name == "github.com/stretchr/testify" && d.CurrentVersion == "1.8.4");
    }

    [Fact]
    public async Task ParseAsync_WithRequireBlock_ParsesAllDependencies()
    {
        var content = """
            module example.com/mymodule

            go 1.21

            require (
                github.com/gin-gonic/gin v1.9.1
                github.com/stretchr/testify v1.8.4
                golang.org/x/text v0.14.0
            )
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, d => d.Name == "github.com/gin-gonic/gin" && d.CurrentVersion == "1.9.1");
        Assert.Contains(result, d => d.Name == "github.com/stretchr/testify" && d.CurrentVersion == "1.8.4");
        Assert.Contains(result, d => d.Name == "golang.org/x/text" && d.CurrentVersion == "0.14.0");
    }

    [Fact]
    public async Task ParseAsync_WithIndirectDependencies_ParsesWithIndirectComment()
    {
        var content = """
            module example.com/mymodule

            go 1.21

            require (
                github.com/gin-gonic/gin v1.9.1
                github.com/bytedance/sonic v1.9.1 // indirect
                golang.org/x/net v0.17.0 // indirect
            )
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, d => d.Name == "github.com/gin-gonic/gin" && d.CurrentVersion == "1.9.1");
        Assert.Contains(result, d => d.Name == "github.com/bytedance/sonic" && d.CurrentVersion == "1.9.1");
        Assert.Contains(result, d => d.Name == "golang.org/x/net" && d.CurrentVersion == "0.17.0");
    }

    [Fact]
    public async Task ParseAsync_WithReplaceDirective_IgnoresReplaceLines()
    {
        var content = """
            module example.com/mymodule

            go 1.21

            require github.com/gin-gonic/gin v1.9.1

            replace github.com/gin-gonic/gin => ../local-gin
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("github.com/gin-gonic/gin", result[0].Name);
    }

    [Fact]
    public async Task ParseAsync_WithReplaceBlock_IgnoresReplaceBlock()
    {
        var content = """
            module example.com/mymodule

            go 1.21

            require (
                github.com/gin-gonic/gin v1.9.1
            )

            replace (
                github.com/gin-gonic/gin => ../local-gin
                golang.org/x/text => golang.org/x/text v0.3.0
            )
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("github.com/gin-gonic/gin", result[0].Name);
    }

    [Fact]
    public async Task ParseAsync_WithVersionPrefix_StripsVPrefix()
    {
        var content = """
            module example.com/mymodule

            require github.com/gin-gonic/gin v1.9.1
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("1.9.1", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithPseudoVersion_ParsesPseudoVersion()
    {
        var content = """
            module example.com/mymodule

            require github.com/some/package v0.0.0-20231001123456-abcdef123456
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("github.com/some/package", result[0].Name);
        Assert.Equal("0.0.0-20231001123456-abcdef123456", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithIncompatibleVersion_ParsesVersion()
    {
        var content = """
            module example.com/mymodule

            require github.com/some/package v2.0.0+incompatible
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("github.com/some/package", result[0].Name);
        Assert.Equal("2.0.0", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithMajorVersionSuffix_ParsesFullPath()
    {
        var content = """
            module example.com/mymodule

            require github.com/go-chi/chi/v5 v5.0.10
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("github.com/go-chi/chi/v5", result[0].Name);
        Assert.Equal("5.0.10", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithExcludeDirective_IgnoresExcludeLines()
    {
        var content = """
            module example.com/mymodule

            require github.com/gin-gonic/gin v1.9.1

            exclude github.com/gin-gonic/gin v1.8.0
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("github.com/gin-gonic/gin", result[0].Name);
        Assert.Equal("1.9.1", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithRetractDirective_IgnoresRetractLines()
    {
        var content = """
            module example.com/mymodule

            require github.com/gin-gonic/gin v1.9.1

            retract v1.0.0
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("github.com/gin-gonic/gin", result[0].Name);
    }

    [Fact]
    public async Task ParseAsync_WithMixedRequireSyntax_ParsesAll()
    {
        var content = """
            module example.com/mymodule

            go 1.21

            require github.com/gin-gonic/gin v1.9.1

            require (
                github.com/stretchr/testify v1.8.4
                golang.org/x/text v0.14.0
            )

            require github.com/go-chi/chi/v5 v5.0.10
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Equal(4, result.Count);
        Assert.Contains(result, d => d.Name == "github.com/gin-gonic/gin");
        Assert.Contains(result, d => d.Name == "github.com/stretchr/testify");
        Assert.Contains(result, d => d.Name == "golang.org/x/text");
        Assert.Contains(result, d => d.Name == "github.com/go-chi/chi/v5");
    }

    [Fact]
    public async Task ParseAsync_WithEmptyRequireBlock_ReturnsEmptyList()
    {
        var content = """
            module example.com/mymodule

            require (
            )
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseAsync_WithCommentOnlyLines_IgnoresComments()
    {
        var content = """
            module example.com/mymodule

            require (
                // main dependency
                github.com/gin-gonic/gin v1.9.1
            )
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("github.com/gin-gonic/gin", result[0].Name);
    }

    [Fact]
    public async Task ParseAsync_WithWindowsLineEndings_ParsesCorrectly()
    {
        var content = "module example.com/mymodule\r\n\r\nrequire github.com/gin-gonic/gin v1.9.1\r\n";

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("github.com/gin-gonic/gin", result[0].Name);
        Assert.Equal("1.9.1", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_RealWorldGoMod_ParsesAllDependencies()
    {
        var content = """
            module github.com/example/myservice

            go 1.21

            require (
                github.com/gin-gonic/gin v1.9.1
                github.com/go-redis/redis/v8 v8.11.5
                github.com/jackc/pgx/v5 v5.4.3
                github.com/stretchr/testify v1.8.4
                go.uber.org/zap v1.26.0
            )

            require (
                github.com/bytedance/sonic v1.9.1 // indirect
                github.com/cespare/xxhash/v2 v2.2.0 // indirect
                github.com/dgryski/go-rendezvous v0.0.0-20200823014737-9f7001d12a5f // indirect
                golang.org/x/net v0.17.0 // indirect
                golang.org/x/sys v0.13.0 // indirect
                golang.org/x/text v0.14.0 // indirect
            )

            replace github.com/example/internal => ../internal
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Equal(11, result.Count);
        Assert.Contains(result, d => d.Name == "github.com/gin-gonic/gin" && d.CurrentVersion == "1.9.1");
        Assert.Contains(result, d => d.Name == "github.com/go-redis/redis/v8" && d.CurrentVersion == "8.11.5");
        Assert.Contains(result, d => d.Name == "github.com/jackc/pgx/v5" && d.CurrentVersion == "5.4.3");
        Assert.Contains(result, d => d.Name == "github.com/stretchr/testify" && d.CurrentVersion == "1.8.4");
        Assert.Contains(result, d => d.Name == "go.uber.org/zap" && d.CurrentVersion == "1.26.0");
        Assert.Contains(result, d => d.Name == "github.com/bytedance/sonic" && d.CurrentVersion == "1.9.1");
        Assert.Contains(result, d => d.Name == "github.com/cespare/xxhash/v2" && d.CurrentVersion == "2.2.0");
        Assert.Contains(result, d => d.Name == "github.com/dgryski/go-rendezvous" && d.CurrentVersion == "0.0.0-20200823014737-9f7001d12a5f");
        Assert.Contains(result, d => d.Name == "golang.org/x/net" && d.CurrentVersion == "0.17.0");
        Assert.Contains(result, d => d.Name == "golang.org/x/sys" && d.CurrentVersion == "0.13.0");
        Assert.Contains(result, d => d.Name == "golang.org/x/text" && d.CurrentVersion == "0.14.0");
    }

    [Fact]
    public async Task ParseAsync_WithPreReleaseVersion_ParsesPreReleaseTag()
    {
        var content = """
            module example.com/mymodule

            require github.com/some/package v1.0.0-beta.1
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("1.0.0-beta.1", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithRcVersion_ParsesRcTag()
    {
        var content = """
            module example.com/mymodule

            require github.com/some/package v2.0.0-rc1
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("2.0.0-rc1", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithExcludeBlock_IgnoresExcludeBlock()
    {
        var content = """
            module example.com/mymodule

            require github.com/gin-gonic/gin v1.9.1

            exclude (
                github.com/gin-gonic/gin v1.8.0
                github.com/gin-gonic/gin v1.7.0
            )
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("github.com/gin-gonic/gin", result[0].Name);
    }

    [Fact]
    public async Task ParseAsync_WithRetractBlock_IgnoresRetractBlock()
    {
        var content = """
            module example.com/mymodule

            require github.com/gin-gonic/gin v1.9.1

            retract (
                v1.0.0
                [v1.1.0, v1.2.0]
            )
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("github.com/gin-gonic/gin", result[0].Name);
    }

    [Fact]
    public async Task ParseAsync_WithNoSpaceAfterRequire_ParsesDependency()
    {
        var content = """
            module example.com/mymodule

            require(
                github.com/gin-gonic/gin v1.9.1
            )
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("github.com/gin-gonic/gin", result[0].Name);
    }

    [Fact]
    public async Task ParseAsync_WithToolchainDirective_IgnoresToolchain()
    {
        var content = """
            module example.com/mymodule

            go 1.21

            toolchain go1.21.3

            require github.com/gin-gonic/gin v1.9.1
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("github.com/gin-gonic/gin", result[0].Name);
    }
}

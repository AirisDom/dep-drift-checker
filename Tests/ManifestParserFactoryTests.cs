using dep_drift_checker.Models;
using dep_drift_checker.Parsers;

namespace dep_drift_checker.Tests;

public class ManifestParserFactoryTests
{
    private readonly ManifestParserFactory _factory = new();

    [Fact]
    public void GetParser_WithNpmType_ReturnsNpmParser()
    {
        var parser = _factory.GetParser(ManifestType.Npm);

        Assert.IsType<NpmManifestParser>(parser);
        Assert.Equal(ManifestType.Npm, parser.ManifestType);
    }

    [Fact]
    public void GetParser_WithNuGetType_ReturnsCsprojParser()
    {
        var parser = _factory.GetParser(ManifestType.NuGet);

        Assert.IsType<CsprojManifestParser>(parser);
        Assert.Equal(ManifestType.NuGet, parser.ManifestType);
    }

    [Fact]
    public void GetParser_WithPipType_ReturnsPipParser()
    {
        var parser = _factory.GetParser(ManifestType.Pip);

        Assert.IsType<PipManifestParser>(parser);
        Assert.Equal(ManifestType.Pip, parser.ManifestType);
    }

    [Fact]
    public void GetParser_WithGoType_ReturnsGoModParser()
    {
        var parser = _factory.GetParser(ManifestType.Go);

        Assert.IsType<GoModManifestParser>(parser);
        Assert.Equal(ManifestType.Go, parser.ManifestType);
    }

    [Fact]
    public void GetParser_WithInvalidType_ThrowsArgumentException()
    {
        var invalidType = (ManifestType)999;

        Assert.Throws<ArgumentException>(() => _factory.GetParser(invalidType));
    }

    [Theory]
    [InlineData("package.json")]
    [InlineData("PACKAGE.JSON")]
    [InlineData("Package.Json")]
    [InlineData("/path/to/package.json")]
    [InlineData("C:\\projects\\app\\package.json")]
    public void GetParser_WithPackageJsonFilename_ReturnsNpmParser(string filename)
    {
        var parser = _factory.GetParser(filename);

        Assert.IsType<NpmManifestParser>(parser);
    }

    [Theory]
    [InlineData("MyProject.csproj")]
    [InlineData("app.csproj")]
    [InlineData("MYPROJECT.CSPROJ")]
    [InlineData("/path/to/MyProject.csproj")]
    [InlineData("C:\\projects\\MyProject.csproj")]
    public void GetParser_WithCsprojFilename_ReturnsCsprojParser(string filename)
    {
        var parser = _factory.GetParser(filename);

        Assert.IsType<CsprojManifestParser>(parser);
    }

    [Theory]
    [InlineData("requirements.txt")]
    [InlineData("REQUIREMENTS.TXT")]
    [InlineData("requirements-dev.txt")]
    [InlineData("requirements-prod.txt")]
    [InlineData("requirements_test.txt")]
    [InlineData("/path/to/requirements.txt")]
    public void GetParser_WithRequirementsTxtFilename_ReturnsPipParser(string filename)
    {
        var parser = _factory.GetParser(filename);

        Assert.IsType<PipManifestParser>(parser);
    }

    [Theory]
    [InlineData("go.mod")]
    [InlineData("GO.MOD")]
    [InlineData("/path/to/go.mod")]
    [InlineData("C:\\projects\\myapp\\go.mod")]
    public void GetParser_WithGoModFilename_ReturnsGoModParser(string filename)
    {
        var parser = _factory.GetParser(filename);

        Assert.IsType<GoModManifestParser>(parser);
    }

    [Theory]
    [InlineData("unknown.txt")]
    [InlineData("manifest.xml")]
    [InlineData("pom.xml")]
    [InlineData("Cargo.toml")]
    public void GetParser_WithUnknownFilename_ThrowsArgumentException(string filename)
    {
        Assert.Throws<ArgumentException>(() => _factory.GetParser(filename));
    }

    [Fact]
    public void DetectManifestTypeFromFilename_WithPackageJson_ReturnsNpm()
    {
        var result = _factory.DetectManifestTypeFromFilename("package.json");

        Assert.Equal(ManifestType.Npm, result);
    }

    [Fact]
    public void DetectManifestTypeFromFilename_WithCsproj_ReturnsNuGet()
    {
        var result = _factory.DetectManifestTypeFromFilename("MyProject.csproj");

        Assert.Equal(ManifestType.NuGet, result);
    }

    [Fact]
    public void DetectManifestTypeFromFilename_WithRequirementsTxt_ReturnsPip()
    {
        var result = _factory.DetectManifestTypeFromFilename("requirements.txt");

        Assert.Equal(ManifestType.Pip, result);
    }

    [Fact]
    public void DetectManifestTypeFromFilename_WithGoMod_ReturnsGo()
    {
        var result = _factory.DetectManifestTypeFromFilename("go.mod");

        Assert.Equal(ManifestType.Go, result);
    }

    [Fact]
    public void GetParserFromContent_WithNpmContent_ReturnsNpmParser()
    {
        var content = """
        {
            "name": "my-app",
            "version": "1.0.0",
            "dependencies": {
                "express": "^4.18.2"
            }
        }
        """;

        var parser = _factory.GetParserFromContent(content);

        Assert.IsType<NpmManifestParser>(parser);
    }

    [Fact]
    public void GetParserFromContent_WithCsprojContent_ReturnsCsprojParser()
    {
        var content = """
        <Project Sdk="Microsoft.NET.Sdk.Web">
          <PropertyGroup>
            <TargetFramework>net8.0</TargetFramework>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
          </ItemGroup>
        </Project>
        """;

        var parser = _factory.GetParserFromContent(content);

        Assert.IsType<CsprojManifestParser>(parser);
    }

    [Fact]
    public void GetParserFromContent_WithGoModContent_ReturnsGoModParser()
    {
        var content = """
        module github.com/myorg/myapp

        go 1.21

        require (
            github.com/gin-gonic/gin v1.9.1
            github.com/lib/pq v1.10.9
        )
        """;

        var parser = _factory.GetParserFromContent(content);

        Assert.IsType<GoModManifestParser>(parser);
    }

    [Fact]
    public void GetParserFromContent_WithPipContent_ReturnsPipParser()
    {
        var content = """
        requests==2.31.0
        flask>=2.0.0
        numpy~=1.24.0
        """;

        var parser = _factory.GetParserFromContent(content);

        Assert.IsType<PipManifestParser>(parser);
    }

    [Fact]
    public void GetParserFromContent_WithEmptyContent_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => _factory.GetParserFromContent(""));
    }

    [Fact]
    public void GetParserFromContent_WithWhitespaceContent_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => _factory.GetParserFromContent("   \n\t  "));
    }

    [Fact]
    public void DetectManifestType_WithFilenameOnly_DetectsFromFilename()
    {
        var result = _factory.DetectManifestType("package.json", null);

        Assert.Equal(ManifestType.Npm, result);
    }

    [Fact]
    public void DetectManifestType_WithContentOnly_DetectsFromContent()
    {
        var content = """
        {
            "dependencies": {
                "lodash": "^4.17.21"
            }
        }
        """;

        var result = _factory.DetectManifestType(null, content);

        Assert.Equal(ManifestType.Npm, result);
    }

    [Fact]
    public void DetectManifestType_WithBothFilenameAndContent_PrefersFilename()
    {
        var goContent = """
        module example.com/myapp
        go 1.21
        """;

        var result = _factory.DetectManifestType("package.json", goContent);

        Assert.Equal(ManifestType.Npm, result);
    }

    [Fact]
    public void DetectManifestType_WithAmbiguousFilenameAndContent_FallsBackToContent()
    {
        var npmContent = """
        {
            "name": "app",
            "dependencies": {
                "express": "1.0.0"
            }
        }
        """;

        var result = _factory.DetectManifestType("unknown.txt", npmContent);

        Assert.Equal(ManifestType.Npm, result);
    }

    [Fact]
    public void DetectManifestType_WithNoFilenameOrContent_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => _factory.DetectManifestType(null, null));
    }

    [Fact]
    public void DetectManifestType_WithEmptyFilenameAndContent_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => _factory.DetectManifestType("", ""));
    }

    [Fact]
    public void DetectManifestTypeFromContent_WithMinimalNpmJson_ReturnsNpm()
    {
        var content = """
        {
            "devDependencies": {
                "typescript": "^5.0.0"
            }
        }
        """;

        var result = _factory.DetectManifestTypeFromContent(content);

        Assert.Equal(ManifestType.Npm, result);
    }

    [Fact]
    public void DetectManifestTypeFromContent_WithProjectSdk_ReturnsNuGet()
    {
        var content = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net8.0</TargetFramework>
          </PropertyGroup>
        </Project>
        """;

        var result = _factory.DetectManifestTypeFromContent(content);

        Assert.Equal(ManifestType.NuGet, result);
    }

    [Fact]
    public void DetectManifestTypeFromContent_WithSingleLineGoMod_ReturnsGo()
    {
        var content = "module example.com/app\n\ngo 1.21\n\nrequire github.com/pkg/errors v0.9.1";

        var result = _factory.DetectManifestTypeFromContent(content);

        Assert.Equal(ManifestType.Go, result);
    }

    [Fact]
    public void DetectManifestTypeFromContent_WithSimplePipRequirements_ReturnsPip()
    {
        var content = """
        # Python dependencies
        requests>=2.28.0
        flask==2.0.0
        """;

        var result = _factory.DetectManifestTypeFromContent(content);

        Assert.Equal(ManifestType.Pip, result);
    }

    [Fact]
    public void GetParser_ReturnsSameParserInstance_ForSameType()
    {
        var parser1 = _factory.GetParser(ManifestType.Npm);
        var parser2 = _factory.GetParser(ManifestType.Npm);

        Assert.Same(parser1, parser2);
    }

    [Fact]
    public void GetParser_ReturnsDifferentParsers_ForDifferentTypes()
    {
        var npmParser = _factory.GetParser(ManifestType.Npm);
        var nugetParser = _factory.GetParser(ManifestType.NuGet);

        Assert.NotSame(npmParser, nugetParser);
    }

    [Theory]
    [InlineData("requirements-local.txt", ManifestType.Pip)]
    [InlineData("requirements-staging.txt", ManifestType.Pip)]
    [InlineData("requirements_dev.txt", ManifestType.Pip)]
    public void DetectManifestTypeFromFilename_WithVariousRequirementsFiles_ReturnsPip(string filename, ManifestType expected)
    {
        var result = _factory.DetectManifestTypeFromFilename(filename);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void DetectManifestTypeFromContent_WithJsonWithNameAndVersion_ReturnsNpm()
    {
        var content = """
        {
            "name": "my-package",
            "version": "1.0.0"
        }
        """;

        var result = _factory.DetectManifestTypeFromContent(content);

        Assert.Equal(ManifestType.Npm, result);
    }

    [Fact]
    public void DetectManifestTypeFromContent_WithSinglePipDependency_ReturnsPip()
    {
        var content = "requests==2.31.0";

        var result = _factory.DetectManifestTypeFromContent(content);

        Assert.Equal(ManifestType.Pip, result);
    }
}

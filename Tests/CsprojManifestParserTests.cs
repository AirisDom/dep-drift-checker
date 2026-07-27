using dep_drift_checker.Models;
using dep_drift_checker.Parsers;

namespace dep_drift_checker.Tests;

public class CsprojManifestParserTests
{
    private readonly CsprojManifestParser _parser = new();

    [Fact]
    public void ManifestType_ReturnsNuGet()
    {
        Assert.Equal(ManifestType.NuGet, _parser.ManifestType);
    }

    [Fact]
    public async Task ParseAsync_WithVersionAttribute_ParsesPackageReferences()
    {
        var content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
                <PackageReference Include="Serilog" Version="3.1.1" />
              </ItemGroup>
            </Project>
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.Name == "Newtonsoft.Json" && d.CurrentVersion == "13.0.3");
        Assert.Contains(result, d => d.Name == "Serilog" && d.CurrentVersion == "3.1.1");
    }

    [Fact]
    public async Task ParseAsync_WithVersionChildElement_ParsesPackageReferences()
    {
        var content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Microsoft.Extensions.Logging">
                  <Version>8.0.0</Version>
                </PackageReference>
              </ItemGroup>
            </Project>
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("Microsoft.Extensions.Logging", result[0].Name);
        Assert.Equal("8.0.0", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithMixedVersionFormats_ParsesBoth()
    {
        var content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="PackageWithAttribute" Version="1.0.0" />
                <PackageReference Include="PackageWithElement">
                  <Version>2.0.0</Version>
                </PackageReference>
              </ItemGroup>
            </Project>
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.Name == "PackageWithAttribute" && d.CurrentVersion == "1.0.0");
        Assert.Contains(result, d => d.Name == "PackageWithElement" && d.CurrentVersion == "2.0.0");
    }

    [Fact]
    public async Task ParseAsync_WithMultipleItemGroups_ParsesAll()
    {
        var content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Package1" Version="1.0.0" />
              </ItemGroup>
              <ItemGroup>
                <PackageReference Include="Package2" Version="2.0.0" />
              </ItemGroup>
            </Project>
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.Name == "Package1");
        Assert.Contains(result, d => d.Name == "Package2");
    }

    [Fact]
    public async Task ParseAsync_WithNoPackageReferences_ReturnsEmptyList()
    {
        var content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Empty(result);
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
    public async Task ParseAsync_WithMalformedXml_ReturnsEmptyList()
    {
        var content = "<Project><ItemGroup><PackageReference Include=\"Test\" Version=\"1.0.0\">";

        var result = await _parser.ParseAsync(content);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseAsync_WithMissingIncludeAttribute_SkipsPackage()
    {
        var content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Version="1.0.0" />
                <PackageReference Include="ValidPackage" Version="2.0.0" />
              </ItemGroup>
            </Project>
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("ValidPackage", result[0].Name);
    }

    [Fact]
    public async Task ParseAsync_WithMissingVersion_SkipsPackage()
    {
        var content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="NoVersionPackage" />
                <PackageReference Include="ValidPackage" Version="2.0.0" />
              </ItemGroup>
            </Project>
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("ValidPackage", result[0].Name);
    }

    [Fact]
    public async Task ParseAsync_WithExactVersionRange_ExtractsVersion()
    {
        var content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="ExactRange" Version="[1.0.0]" />
              </ItemGroup>
            </Project>
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("1.0.0", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithMinimumVersionRange_ExtractsMinVersion()
    {
        var content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="MinRange" Version="[1.0.0,)" />
              </ItemGroup>
            </Project>
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("1.0.0", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithMaximumVersionRange_ExtractsMaxVersion()
    {
        var content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="MaxRange" Version="(,2.0.0]" />
              </ItemGroup>
            </Project>
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("2.0.0", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithBoundedVersionRange_ExtractsMinVersion()
    {
        var content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="BoundedRange" Version="[1.0.0,2.0.0)" />
              </ItemGroup>
            </Project>
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("1.0.0", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithSelfClosingTag_ParsesCorrectly()
    {
        var content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="SelfClosing" Version="1.2.3"/>
              </ItemGroup>
            </Project>
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("SelfClosing", result[0].Name);
        Assert.Equal("1.2.3", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithPreReleaseVersion_PreservesPreReleaseTag()
    {
        var content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="PreRelease" Version="1.0.0-beta.1" />
              </ItemGroup>
            </Project>
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("1.0.0-beta.1", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithVersionAttributePreferredOverElement_UsesAttribute()
    {
        var content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="BothFormats" Version="1.0.0">
                  <Version>2.0.0</Version>
                </PackageReference>
              </ItemGroup>
            </Project>
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("1.0.0", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_WithConditionAttribute_ParsesPackage()
    {
        var content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
                <PackageReference Include="ConditionalPackage" Version="1.0.0" />
              </ItemGroup>
            </Project>
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("ConditionalPackage", result[0].Name);
    }

    [Fact]
    public async Task ParseAsync_WithPrivateAssets_ParsesPackage()
    {
        var content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="AnalyzerPackage" Version="1.0.0">
                  <PrivateAssets>all</PrivateAssets>
                  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
                </PackageReference>
              </ItemGroup>
            </Project>
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("AnalyzerPackage", result[0].Name);
        Assert.Equal("1.0.0", result[0].CurrentVersion);
    }

    [Fact]
    public async Task ParseAsync_RealWorldCsproj_ParsesAllPackages()
    {
        var content = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.0" />
                <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
              </ItemGroup>

              <ItemGroup>
                <PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
                <PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />
              </ItemGroup>
            </Project>
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Equal(4, result.Count);
        Assert.Contains(result, d => d.Name == "Microsoft.AspNetCore.OpenApi" && d.CurrentVersion == "8.0.0");
        Assert.Contains(result, d => d.Name == "Swashbuckle.AspNetCore" && d.CurrentVersion == "6.5.0");
        Assert.Contains(result, d => d.Name == "Serilog.AspNetCore" && d.CurrentVersion == "8.0.0");
        Assert.Contains(result, d => d.Name == "Serilog.Sinks.Console" && d.CurrentVersion == "5.0.1");
    }

    [Fact]
    public async Task ParseAsync_WithNamespacedXml_ParsesCorrectly()
    {
        var content = """
            <?xml version="1.0" encoding="utf-8"?>
            <Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
              <ItemGroup>
                <PackageReference Include="OldStylePackage" Version="1.0.0" />
              </ItemGroup>
            </Project>
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("OldStylePackage", result[0].Name);
    }

    [Fact]
    public async Task ParseAsync_WithFloatingVersion_PreservesWildcard()
    {
        var content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="FloatingMajor" Version="1.*" />
                <PackageReference Include="FloatingMinor" Version="1.0.*" />
              </ItemGroup>
            </Project>
            """;

        var result = await _parser.ParseAsync(content);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.Name == "FloatingMajor" && d.CurrentVersion == "1.*");
        Assert.Contains(result, d => d.Name == "FloatingMinor" && d.CurrentVersion == "1.0.*");
    }
}

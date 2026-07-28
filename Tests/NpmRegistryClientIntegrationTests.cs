using dep_drift_checker.Registry;

namespace dep_drift_checker.Tests;

[Trait("Category", "Integration")]
public class NpmRegistryClientIntegrationTests
{
    private static bool ShouldSkipIntegrationTests =>
        Environment.GetEnvironmentVariable("CI") == "true" ||
        Environment.GetEnvironmentVariable("SKIP_INTEGRATION_TESTS") == "true";

    [Fact]
    public async Task GetLatestVersionAsync_WithRealRegistry_ReturnsExpressVersion()
    {
        if (ShouldSkipIntegrationTests)
        {
            return;
        }

        var httpClient = new HttpClient();
        var client = new NpmRegistryClient(httpClient);

        var result = await client.GetLatestVersionAsync("express");

        Assert.NotNull(result);
        Assert.Matches(@"^\d+\.\d+\.\d+", result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithRealRegistry_ReturnsLodashVersion()
    {
        if (ShouldSkipIntegrationTests)
        {
            return;
        }

        var httpClient = new HttpClient();
        var client = new NpmRegistryClient(httpClient);

        var result = await client.GetLatestVersionAsync("lodash");

        Assert.NotNull(result);
        Assert.Matches(@"^\d+\.\d+\.\d+", result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithRealRegistry_ReturnsTypesNodeVersion()
    {
        if (ShouldSkipIntegrationTests)
        {
            return;
        }

        var httpClient = new HttpClient();
        var client = new NpmRegistryClient(httpClient);

        var result = await client.GetLatestVersionAsync("@types/node");

        Assert.NotNull(result);
        Assert.Matches(@"^\d+\.\d+\.\d+", result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithRealRegistry_NonexistentPackageReturnsNull()
    {
        if (ShouldSkipIntegrationTests)
        {
            return;
        }

        var httpClient = new HttpClient();
        var client = new NpmRegistryClient(httpClient);

        var result = await client.GetLatestVersionAsync("this-package-definitely-does-not-exist-xyz-123456");

        Assert.Null(result);
    }
}

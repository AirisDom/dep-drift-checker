using System.Net;
using dep_drift_checker.Registry;

namespace dep_drift_checker.Tests;

public class NuGetRegistryClientTests
{
    [Fact]
    public async Task GetLatestVersionAsync_WithValidPackage_ReturnsLatestStableVersion()
    {
        var responseContent = """{"versions":["1.0.0","1.1.0","2.0.0"]}""";
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, responseContent);
        var httpClient = new HttpClient(handler);
        var client = new NuGetRegistryClient(httpClient);

        var result = await client.GetLatestVersionAsync("Newtonsoft.Json");

        Assert.Equal("2.0.0", result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithMixedVersions_FiltersOutPrereleaseByDefault()
    {
        var responseContent = """{"versions":["1.0.0","2.0.0","3.0.0-beta1","3.0.0-rc1"]}""";
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, responseContent);
        var httpClient = new HttpClient(handler);
        var client = new NuGetRegistryClient(httpClient);

        var result = await client.GetLatestVersionAsync("Newtonsoft.Json");

        Assert.Equal("2.0.0", result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithIncludePrerelease_ReturnsPrereleaseVersion()
    {
        var responseContent = """{"versions":["1.0.0","2.0.0","3.0.0-beta1"]}""";
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, responseContent);
        var httpClient = new HttpClient(handler);
        var client = new NuGetRegistryClient(httpClient, includePrerelease: true);

        var result = await client.GetLatestVersionAsync("Newtonsoft.Json");

        Assert.Equal("3.0.0-beta1", result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithOnlyPrereleaseVersions_ReturnsNull()
    {
        var responseContent = """{"versions":["1.0.0-alpha","2.0.0-beta"]}""";
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, responseContent);
        var httpClient = new HttpClient(handler);
        var client = new NuGetRegistryClient(httpClient);

        var result = await client.GetLatestVersionAsync("SomePackage");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithNotFoundPackage_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.NotFound, "");
        var httpClient = new HttpClient(handler);
        var client = new NuGetRegistryClient(httpClient);

        var result = await client.GetLatestVersionAsync("nonexistent-package-xyz");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithUnlistedPackage_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.NotFound, "");
        var httpClient = new HttpClient(handler);
        var client = new NuGetRegistryClient(httpClient);

        var result = await client.GetLatestVersionAsync("UnlistedPackage");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithNullPackageName_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "{}");
        var httpClient = new HttpClient(handler);
        var client = new NuGetRegistryClient(httpClient);

        var result = await client.GetLatestVersionAsync(null!);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithEmptyPackageName_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "{}");
        var httpClient = new HttpClient(handler);
        var client = new NuGetRegistryClient(httpClient);

        var result = await client.GetLatestVersionAsync("");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithWhitespacePackageName_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "{}");
        var httpClient = new HttpClient(handler);
        var client = new NuGetRegistryClient(httpClient);

        var result = await client.GetLatestVersionAsync("   ");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithMalformedJson_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "not valid json");
        var httpClient = new HttpClient(handler);
        var client = new NuGetRegistryClient(httpClient);

        var result = await client.GetLatestVersionAsync("Newtonsoft.Json");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithMissingVersionsProperty_ReturnsNull()
    {
        var responseContent = """{"name":"Newtonsoft.Json"}""";
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, responseContent);
        var httpClient = new HttpClient(handler);
        var client = new NuGetRegistryClient(httpClient);

        var result = await client.GetLatestVersionAsync("Newtonsoft.Json");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithEmptyVersionsArray_ReturnsNull()
    {
        var responseContent = """{"versions":[]}""";
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, responseContent);
        var httpClient = new HttpClient(handler);
        var client = new NuGetRegistryClient(httpClient);

        var result = await client.GetLatestVersionAsync("SomePackage");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithRetryableError_RetriesWithBackoff()
    {
        var responses = new[]
        {
            (HttpStatusCode.ServiceUnavailable, ""),
            (HttpStatusCode.ServiceUnavailable, ""),
            (HttpStatusCode.OK, """{"versions":["1.0.0"]}""")
        };
        var handler = new MockHttpMessageHandler(responses);
        var httpClient = new HttpClient(handler);
        var client = new NuGetRegistryClient(httpClient, maxRetries: 3, initialDelay: TimeSpan.FromMilliseconds(10));

        var result = await client.GetLatestVersionAsync("Newtonsoft.Json");

        Assert.Equal("1.0.0", result);
        Assert.Equal(3, handler.CallCount);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithTooManyRequests_RetriesWithBackoff()
    {
        var responses = new[]
        {
            (HttpStatusCode.TooManyRequests, ""),
            (HttpStatusCode.OK, """{"versions":["2.0.0"]}""")
        };
        var handler = new MockHttpMessageHandler(responses);
        var httpClient = new HttpClient(handler);
        var client = new NuGetRegistryClient(httpClient, maxRetries: 3, initialDelay: TimeSpan.FromMilliseconds(10));

        var result = await client.GetLatestVersionAsync("Newtonsoft.Json");

        Assert.Equal("2.0.0", result);
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WhenExceedsMaxRetries_ThrowsException()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.ServiceUnavailable, "");
        var httpClient = new HttpClient(handler);
        var client = new NuGetRegistryClient(httpClient, maxRetries: 2, initialDelay: TimeSpan.FromMilliseconds(10));

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetLatestVersionAsync("Newtonsoft.Json"));

        Assert.Equal(3, handler.CallCount);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithHttpRequestException_RetriesWithBackoff()
    {
        var handler = new MockHttpMessageHandler(new HttpRequestException("Connection failed"), 2);
        handler.SuccessResponseAfterFailures = """{"versions":["4.0.0"]}""";
        var httpClient = new HttpClient(handler);
        var client = new NuGetRegistryClient(httpClient, maxRetries: 3, initialDelay: TimeSpan.FromMilliseconds(10));

        var result = await client.GetLatestVersionAsync("Newtonsoft.Json");

        Assert.Equal("4.0.0", result);
        Assert.Equal(3, handler.CallCount);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithCancellation_ThrowsCancelledException()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, """{"versions":["1.0.0"]}""");
        var httpClient = new HttpClient(handler);
        var client = new NuGetRegistryClient(httpClient);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            client.GetLatestVersionAsync("Newtonsoft.Json", cts.Token));
    }

    [Fact]
    public async Task GetLatestVersionAsync_SendsCorrectUrl_WithLowercasePackageName()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, """{"versions":["1.0.0"]}""");
        var httpClient = new HttpClient(handler);
        var client = new NuGetRegistryClient(httpClient);

        await client.GetLatestVersionAsync("Newtonsoft.Json");

        Assert.Equal("https://api.nuget.org/v3-flatcontainer/newtonsoft.json/index.json", handler.LastRequestUri?.AbsoluteUri);
    }

    [Fact]
    public async Task GetLatestVersionAsync_SendsJsonAcceptHeader()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, """{"versions":["1.0.0"]}""");
        var httpClient = new HttpClient(handler);
        var client = new NuGetRegistryClient(httpClient);

        await client.GetLatestVersionAsync("Newtonsoft.Json");

        Assert.Contains("application/json", handler.LastRequestHeaders?["Accept"]);
    }

    [Fact]
    public async Task GetLatestVersionAsync_NotFoundDoesNotRetry()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.NotFound, "");
        var httpClient = new HttpClient(handler);
        var client = new NuGetRegistryClient(httpClient, maxRetries: 3, initialDelay: TimeSpan.FromMilliseconds(10));

        var result = await client.GetLatestVersionAsync("nonexistent");

        Assert.Null(result);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithVariousPrereleaseFormats_FiltersCorrectly()
    {
        var responseContent = """{"versions":["1.0.0","2.0.0-alpha","2.0.0-beta.1","2.0.0-preview.2","2.0.0-rc1","1.5.0"]}""";
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, responseContent);
        var httpClient = new HttpClient(handler);
        var client = new NuGetRegistryClient(httpClient);

        var result = await client.GetLatestVersionAsync("SomePackage");

        Assert.Equal("1.5.0", result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithMixedCasePackageName_NormalizesToLowercase()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, """{"versions":["1.0.0"]}""");
        var httpClient = new HttpClient(handler);
        var client = new NuGetRegistryClient(httpClient);

        await client.GetLatestVersionAsync("Microsoft.Extensions.DependencyInjection");

        Assert.Equal("https://api.nuget.org/v3-flatcontainer/microsoft.extensions.dependencyinjection/index.json",
            handler.LastRequestUri?.AbsoluteUri);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithSemanticVersionPrereleases_FiltersCorrectly()
    {
        var responseContent = """{"versions":["6.0.0","7.0.0","8.0.0-preview.1","8.0.0-preview.7"]}""";
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, responseContent);
        var httpClient = new HttpClient(handler);
        var client = new NuGetRegistryClient(httpClient);

        var result = await client.GetLatestVersionAsync("Microsoft.AspNetCore.App");

        Assert.Equal("7.0.0", result);
    }
}

using System.Net;
using dep_drift_checker.Registry;

namespace dep_drift_checker.Tests;

public class NpmRegistryClientTests
{
    [Fact]
    public async Task GetLatestVersionAsync_WithValidPackage_ReturnsVersion()
    {
        var responseContent = """{"name":"express","version":"4.18.2"}""";
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, responseContent);
        var httpClient = new HttpClient(handler);
        var client = new NpmRegistryClient(httpClient);

        var result = await client.GetLatestVersionAsync("express");

        Assert.Equal("4.18.2", result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithScopedPackage_ReturnsVersion()
    {
        var responseContent = """{"name":"@types/node","version":"20.0.0"}""";
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, responseContent);
        var httpClient = new HttpClient(handler);
        var client = new NpmRegistryClient(httpClient);

        var result = await client.GetLatestVersionAsync("@types/node");

        Assert.Equal("20.0.0", result);
        Assert.Contains("%40types%2Fnode", handler.LastRequestUri?.AbsoluteUri);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithNotFoundPackage_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.NotFound, "");
        var httpClient = new HttpClient(handler);
        var client = new NpmRegistryClient(httpClient);

        var result = await client.GetLatestVersionAsync("nonexistent-package-xyz");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithNullPackageName_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "{}");
        var httpClient = new HttpClient(handler);
        var client = new NpmRegistryClient(httpClient);

        var result = await client.GetLatestVersionAsync(null!);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithEmptyPackageName_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "{}");
        var httpClient = new HttpClient(handler);
        var client = new NpmRegistryClient(httpClient);

        var result = await client.GetLatestVersionAsync("");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithWhitespacePackageName_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "{}");
        var httpClient = new HttpClient(handler);
        var client = new NpmRegistryClient(httpClient);

        var result = await client.GetLatestVersionAsync("   ");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithMalformedJson_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "not valid json");
        var httpClient = new HttpClient(handler);
        var client = new NpmRegistryClient(httpClient);

        var result = await client.GetLatestVersionAsync("express");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithMissingVersionProperty_ReturnsNull()
    {
        var responseContent = """{"name":"express","description":"Fast web framework"}""";
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, responseContent);
        var httpClient = new HttpClient(handler);
        var client = new NpmRegistryClient(httpClient);

        var result = await client.GetLatestVersionAsync("express");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithRetryableError_RetriesWithBackoff()
    {
        var responses = new[]
        {
            (HttpStatusCode.ServiceUnavailable, ""),
            (HttpStatusCode.ServiceUnavailable, ""),
            (HttpStatusCode.OK, """{"version":"1.0.0"}""")
        };
        var handler = new MockHttpMessageHandler(responses);
        var httpClient = new HttpClient(handler);
        var client = new NpmRegistryClient(httpClient, maxRetries: 3, initialDelay: TimeSpan.FromMilliseconds(10));

        var result = await client.GetLatestVersionAsync("express");

        Assert.Equal("1.0.0", result);
        Assert.Equal(3, handler.CallCount);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithTooManyRequests_RetriesWithBackoff()
    {
        var responses = new[]
        {
            (HttpStatusCode.TooManyRequests, ""),
            (HttpStatusCode.OK, """{"version":"2.0.0"}""")
        };
        var handler = new MockHttpMessageHandler(responses);
        var httpClient = new HttpClient(handler);
        var client = new NpmRegistryClient(httpClient, maxRetries: 3, initialDelay: TimeSpan.FromMilliseconds(10));

        var result = await client.GetLatestVersionAsync("lodash");

        Assert.Equal("2.0.0", result);
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithGatewayTimeout_RetriesWithBackoff()
    {
        var responses = new[]
        {
            (HttpStatusCode.GatewayTimeout, ""),
            (HttpStatusCode.OK, """{"version":"3.0.0"}""")
        };
        var handler = new MockHttpMessageHandler(responses);
        var httpClient = new HttpClient(handler);
        var client = new NpmRegistryClient(httpClient, maxRetries: 3, initialDelay: TimeSpan.FromMilliseconds(10));

        var result = await client.GetLatestVersionAsync("axios");

        Assert.Equal("3.0.0", result);
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WhenExceedsMaxRetries_ThrowsException()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.ServiceUnavailable, "");
        var httpClient = new HttpClient(handler);
        var client = new NpmRegistryClient(httpClient, maxRetries: 2, initialDelay: TimeSpan.FromMilliseconds(10));

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetLatestVersionAsync("express"));

        Assert.Equal(3, handler.CallCount);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithHttpRequestException_RetriesWithBackoff()
    {
        var handler = new MockHttpMessageHandler(new HttpRequestException("Connection failed"), 2);
        handler.SuccessResponseAfterFailures = """{"version":"4.0.0"}""";
        var httpClient = new HttpClient(handler);
        var client = new NpmRegistryClient(httpClient, maxRetries: 3, initialDelay: TimeSpan.FromMilliseconds(10));

        var result = await client.GetLatestVersionAsync("express");

        Assert.Equal("4.0.0", result);
        Assert.Equal(3, handler.CallCount);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithCancellation_ThrowsCancelledException()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, """{"version":"1.0.0"}""");
        var httpClient = new HttpClient(handler);
        var client = new NpmRegistryClient(httpClient);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            client.GetLatestVersionAsync("express", cts.Token));
    }

    [Fact]
    public async Task GetLatestVersionAsync_SendsCorrectUrl()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, """{"version":"1.0.0"}""");
        var httpClient = new HttpClient(handler);
        var client = new NpmRegistryClient(httpClient);

        await client.GetLatestVersionAsync("express");

        Assert.Equal("https://registry.npmjs.org/express/latest", handler.LastRequestUri?.AbsoluteUri);
    }

    [Fact]
    public async Task GetLatestVersionAsync_SendsJsonAcceptHeader()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, """{"version":"1.0.0"}""");
        var httpClient = new HttpClient(handler);
        var client = new NpmRegistryClient(httpClient);

        await client.GetLatestVersionAsync("express");

        Assert.Contains("application/json", handler.LastRequestHeaders?["Accept"]);
    }

    [Fact]
    public async Task GetLatestVersionAsync_NotFoundDoesNotRetry()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.NotFound, "");
        var httpClient = new HttpClient(handler);
        var client = new NpmRegistryClient(httpClient, maxRetries: 3, initialDelay: TimeSpan.FromMilliseconds(10));

        var result = await client.GetLatestVersionAsync("nonexistent");

        Assert.Null(result);
        Assert.Equal(1, handler.CallCount);
    }
}

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<(HttpStatusCode StatusCode, string Content)> _responses = new();
    private readonly Exception? _exceptionToThrow;
    private readonly int _throwUntilAttempt;

    public int CallCount { get; private set; }
    public Uri? LastRequestUri { get; private set; }
    public Dictionary<string, string>? LastRequestHeaders { get; private set; }
    public string? SuccessResponseAfterFailures { get; set; }

    public MockHttpMessageHandler(HttpStatusCode statusCode, string content)
    {
        _responses.Enqueue((statusCode, content));
    }

    public MockHttpMessageHandler((HttpStatusCode StatusCode, string Content)[] responses)
    {
        foreach (var response in responses)
        {
            _responses.Enqueue(response);
        }
    }

    public MockHttpMessageHandler(Exception exception, int throwUntilAttempt)
    {
        _exceptionToThrow = exception;
        _throwUntilAttempt = throwUntilAttempt;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        CallCount++;
        LastRequestUri = request.RequestUri;
        LastRequestHeaders = request.Headers.ToDictionary(
            h => h.Key,
            h => string.Join(",", h.Value));

        if (_exceptionToThrow != null && CallCount <= _throwUntilAttempt)
        {
            throw _exceptionToThrow;
        }

        if (_exceptionToThrow != null && SuccessResponseAfterFailures != null)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(SuccessResponseAfterFailures)
            };
        }

        if (_responses.Count > 0)
        {
            var (statusCode, content) = _responses.Dequeue();
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            };
        }

        return await Task.FromResult(new HttpResponseMessage(_responses.Count > 0
            ? _responses.Peek().StatusCode
            : HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("")
        });
    }
}

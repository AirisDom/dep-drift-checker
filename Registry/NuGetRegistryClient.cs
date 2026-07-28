using System.Net;
using System.Text.Json;

namespace dep_drift_checker.Registry;

public class NuGetRegistryClient : IRegistryClient
{
    private readonly HttpClient _httpClient;
    private readonly int _maxRetries;
    private readonly TimeSpan _initialDelay;
    private readonly bool _includePrerelease;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NuGetRegistryClient(HttpClient httpClient, int maxRetries = 3, TimeSpan? initialDelay = null, bool includePrerelease = false)
    {
        _httpClient = httpClient;
        _maxRetries = maxRetries;
        _initialDelay = initialDelay ?? TimeSpan.FromMilliseconds(500);
        _includePrerelease = includePrerelease;
    }

    public async Task<string?> GetLatestVersionAsync(string packageName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return null;

        var lowercasePackageName = packageName.ToLowerInvariant();
        var url = $"https://api.nuget.org/v3-flatcontainer/{lowercasePackageName}/index.json";

        var attempt = 0;
        while (true)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Accept", "application/json");

                using var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;

                if (ShouldRetry(response.StatusCode) && attempt < _maxRetries)
                {
                    await DelayWithBackoff(attempt, cancellationToken);
                    attempt++;
                    continue;
                }

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return ParseLatestVersionFromResponse(content);
            }
            catch (HttpRequestException) when (attempt < _maxRetries)
            {
                await DelayWithBackoff(attempt, cancellationToken);
                attempt++;
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < _maxRetries)
            {
                await DelayWithBackoff(attempt, cancellationToken);
                attempt++;
            }
        }
    }

    private static bool ShouldRetry(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.TooManyRequests or
                      HttpStatusCode.ServiceUnavailable or
                      HttpStatusCode.GatewayTimeout or
                      HttpStatusCode.BadGateway or
                      HttpStatusCode.RequestTimeout;

    private async Task DelayWithBackoff(int attempt, CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromMilliseconds(_initialDelay.TotalMilliseconds * Math.Pow(2, attempt));
        await Task.Delay(delay, cancellationToken);
    }

    private string? ParseLatestVersionFromResponse(string content)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            if (!root.TryGetProperty("versions", out var versionsElement))
                return null;

            var versions = new List<string>();
            foreach (var versionElement in versionsElement.EnumerateArray())
            {
                var version = versionElement.GetString();
                if (version != null)
                    versions.Add(version);
            }

            if (versions.Count == 0)
                return null;

            if (_includePrerelease)
                return versions[^1];

            for (var i = versions.Count - 1; i >= 0; i--)
            {
                if (!IsPrerelease(versions[i]))
                    return versions[i];
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsPrerelease(string version)
    {
        return version.Contains('-');
    }
}

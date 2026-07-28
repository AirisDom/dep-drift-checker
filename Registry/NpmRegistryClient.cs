using System.Net;
using System.Text.Json;

namespace dep_drift_checker.Registry;

public class NpmRegistryClient : IRegistryClient
{
    private readonly HttpClient _httpClient;
    private readonly int _maxRetries;
    private readonly TimeSpan _initialDelay;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NpmRegistryClient(HttpClient httpClient, int maxRetries = 3, TimeSpan? initialDelay = null)
    {
        _httpClient = httpClient;
        _maxRetries = maxRetries;
        _initialDelay = initialDelay ?? TimeSpan.FromMilliseconds(500);
    }

    public async Task<string?> GetLatestVersionAsync(string packageName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return null;

        var encodedPackageName = Uri.EscapeDataString(packageName);
        var url = $"https://registry.npmjs.org/{encodedPackageName}/latest";

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
                return ParseVersionFromResponse(content);
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

    private static string? ParseVersionFromResponse(string content)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            if (root.TryGetProperty("version", out var versionElement))
            {
                return versionElement.GetString();
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

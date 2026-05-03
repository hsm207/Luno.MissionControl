using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// A professional-grade OTLP telemetry forwarder that bridges browser-based 
/// telemetry to the Aspire Dashboard. Adheres to Clean Architecture by 
/// isolating the infrastructure concerns of proxying and authentication.
/// </summary>
public sealed class OtlpForwarder
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OtlpForwarder> _logger;
    private readonly Dictionary<string, string> _otlpHeaders;

    public OtlpForwarder(HttpClient httpClient, ILogger<OtlpForwarder> logger, string endpoint, string? rawHeaders)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Ensure the base address is set correctly for relative path forwarding.
        _httpClient.BaseAddress = new Uri(endpoint.TrimEnd('/') + "/");
        
        _otlpHeaders = ParseOtlpHeaders(rawHeaders);
    }

    /// <summary>
    /// Forwards an OTLP request from the client to the internal collector.
    /// Implements the "Humble Object" logic for the proxy.
    /// </summary>
    public async Task<IResult> ForwardAsync(string path, HttpContext context)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Content = new StreamContent(context.Request.Body);

        // Map Content-specific headers
        if (context.Request.ContentType != null)
        {
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(context.Request.ContentType);
        }

        if (context.Request.ContentLength.HasValue)
        {
            request.Content.Headers.ContentLength = context.Request.ContentLength.Value;
        }

        // Inject the server's authentication headers (the "Secret Sauce")
        foreach (var (key, value) in _otlpHeaders)
        {
            request.Headers.TryAddWithoutValidation(key, value);
        }

        try
        {
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("OTLP Forwarder: Collector returned {StatusCode} for /{Path}. Detail: {Detail}",
                    response.StatusCode, path, errorBody);
            }

            return Results.StatusCode((int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OTLP Forwarder: Failed to forward request to /{Path}", path);
            return Results.StatusCode(StatusCodes.Status502BadGateway);
        }
    }

    private static Dictionary<string, string> ParseOtlpHeaders(string? rawHeaders)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(rawHeaders)) return result;

        foreach (var part in rawHeaders.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = part.IndexOf('=');
            if (idx > 0)
            {
                result[part[..idx].Trim()] = part[(idx + 1)..].Trim();
            }
        }

        return result;
    }
}

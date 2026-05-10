using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Luno.MissionControl.Application.Models;

namespace Luno.MissionControl.Web.Client.Adapters;

/// <summary>
/// Base class for BFF proxies to standardize HTTP communication and error handling.
/// </summary>
public abstract class BffProxyBase(HttpClient httpClient, ILogger logger)
{
    protected readonly HttpClient HttpClient = httpClient;
    protected readonly ILogger Logger = logger;

    protected async Task<TResponse> PostAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken ct = default)
        where TResponse : class, new()
    {
        try
        {
            var response = await HttpClient.PostAsJsonAsync(requestUri, request, ct);
            return await HandleResponseAsync<TResponse>(response, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "HTTP POST failed for {Uri}", requestUri);
            throw;
        }
    }

    protected async Task<TResponse> GetAsync<TResponse>(string requestUri, CancellationToken ct = default)
        where TResponse : class, new()
    {
        try
        {
            var response = await HttpClient.GetAsync(requestUri, ct);
            return await HandleResponseAsync<TResponse>(response, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "HTTP GET failed for {Uri}", requestUri);
            throw;
        }
    }

    private async Task<TResponse> HandleResponseAsync<TResponse>(HttpResponseMessage response, CancellationToken ct)
        where TResponse : class, new()
    {
        if (response.IsSuccessStatusCode)
        {
            // If the server returns 204 No Content or an empty body, we return a new instance of TResponse
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent ||
                response.Content.Headers.ContentLength == 0)
            {
                return new TResponse();
            }

            return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct) ?? new TResponse();
        }

        // Standardized ProblemDetails handling
        var problem = await response.Content.ReadFromJsonAsync<LunoProblemDetailsDto>(cancellationToken: ct);
        var message = problem?.Detail ?? $"Communication failure (HTTP {response.StatusCode})";

        Logger.LogWarning("BFF error at {Uri}: {Message}", response.RequestMessage?.RequestUri, message);

        throw new HttpRequestException(message, null, response.StatusCode);
    }
}

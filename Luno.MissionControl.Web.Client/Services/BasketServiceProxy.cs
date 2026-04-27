using System.Net.Http.Json;
using Luno.MissionControl.Application;
using Luno.MissionControl.Application.Models;

namespace Luno.MissionControl.Web.Client.Services;

/// <summary>
/// A Client-side proxy for the IBasketService that delegates orchestration calls to the BFF.
/// Ensures that the WASM client remains a 'Humble Object' without SDK dependencies.
/// </summary>
public class BasketServiceProxy : IBasketService
{
    private readonly HttpClient _httpClient;

    public BasketServiceProxy(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<BasketExecutionResult> ExecuteAsync(BasketExecutionRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/basket/execute", request, ct);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<BasketExecutionResult>(cancellationToken: ct)
                       ?? new BasketExecutionResult(false, Array.Empty<OrderSummary>(), "BFF returned a null execution result.");
            }

            // If we're here, we might have a ProblemDetails (400/500) from the gateway panic-catch
            var problem = await response.Content.ReadFromJsonAsync<LunoProblemDetails>(cancellationToken: ct);
            return new BasketExecutionResult(false, Array.Empty<OrderSummary>(), problem?.Detail ?? $"Communication failure (HTTP {response.StatusCode})");
        }
        catch (Exception ex)
        {
            return new BasketExecutionResult(false, Array.Empty<OrderSummary>(), $"Network Error: {ex.Message}");
        }
    }
}

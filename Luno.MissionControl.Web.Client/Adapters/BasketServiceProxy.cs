using Luno.MissionControl.Application.Ports;
using Luno.MissionControl.Application.Commands;
using Luno.MissionControl.Application.Models;
using Microsoft.Extensions.Logging;

namespace Luno.MissionControl.Web.Client.Adapters;

/// <summary>
/// A Client-side proxy for the IBasketService that delegates orchestration calls to the BFF.
/// </summary>
public class BasketServiceProxy(HttpClient httpClient, ILogger<BasketServiceProxy> logger) 
    : BffProxyBase(httpClient, logger), IBasketService
{
    /// <summary>
    /// Executes a full basket allocation by sending the command to the BFF.
    /// </summary>
    public async Task<BasketExecutionResponseDto> ExecuteAsync(ExecuteAllocationCommand command, CancellationToken ct = default)
    {
        try
        {
            return await PostAsync<ExecuteAllocationCommand, BasketExecutionResponseDto>("/api/basket/execute", command, ct);
        }
        catch (HttpRequestException ex)
        {
            // Map the standardized exception back to the response model for UI consumption
            return new BasketExecutionResponseDto(false, [], ex.Message);
        }
        catch (Exception ex)
        {
            return new BasketExecutionResponseDto(false, [], $"Unexpected Error: {ex.Message}");
        }
    }
}

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Logging;

namespace Luno.MissionControl.Web.Services;

/// <summary>
/// Monitors the lifecycle of Blazor Server circuits to provide visibility into hydration transitions
/// and client connection stability.
/// </summary>
public class CircuitLifecycleLogger : CircuitHandler
{
    private readonly ILogger<CircuitLifecycleLogger> _logger;

    public CircuitLifecycleLogger(ILogger<CircuitLifecycleLogger> logger)
    {
        _logger = logger;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Blazor Circuit Opened. ID: {CircuitId}", circuit.Id);
        return base.OnCircuitOpenedAsync(circuit, cancellationToken);
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Blazor Circuit Connected (Up). ID: {CircuitId}", circuit.Id);
        return base.OnConnectionUpAsync(circuit, cancellationToken);
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Blazor Circuit Disconnected (Down). ID: {CircuitId}", circuit.Id);
        return base.OnConnectionDownAsync(circuit, cancellationToken);
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Blazor Circuit Closed. ID: {CircuitId}", circuit.Id);
        return base.OnCircuitClosedAsync(circuit, cancellationToken);
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Luno.MissionControl.Application.Commands;
using Luno.MissionControl.Application.Models;

namespace Luno.MissionControl.Application;

/// <summary>
/// A high-fidelity simulator for the Basket Orchestrator.
/// Provides realistic delay and deterministic success without requiring API credentials or SDK connectivity.
/// </summary>
public sealed class SimulatedBasketOrchestrator : IBasketService
{
    private static readonly Random _rng = new();

    public async Task<BasketExecutionResult> ExecuteAsync(BasketExecutionRequest request, CancellationToken ct = default)
    {
        using var activity = ForensicTracing.StartActivity("SimulateExecuteBasket");
        activity?.SetTag("simulation.mode", true);

        // 1. Validation (Same as real orchestrator)
        if (request.Allocations.Count == 0)
        {
            return new BasketExecutionResult(false, Array.Empty<OrderSummary>(), "Simulation Failed: Basket must contain at least one allocation.");
        }

        // 2. Realistic "Orchestration" Delay
        // Simulate the overhead of market metadata resolution and account verification
        await Task.Delay(800, ct);

        var orders = new List<OrderSummary>();

        foreach (var allocation in request.Allocations)
        {
            // Simulate network latency for each order placement
            await Task.Delay(_rng.Next(150, 400), ct);

            // Generate a fake but realistic-looking Luno Order ID
            string simulatedOrderId = $"BX-{_rng.Next(100000, 999999)}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
            orders.Add(new OrderSummary(simulatedOrderId, allocation.Pair));
        }

        return new BasketExecutionResult(true, orders);
    }
}

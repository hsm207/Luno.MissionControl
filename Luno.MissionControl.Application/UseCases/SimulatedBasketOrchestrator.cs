using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Luno.MissionControl.Application.Models;
using Luno.MissionControl.Application.Commands;
using Luno.MissionControl.Application.Ports;
using Luno.MissionControl.Core.Models;
using Microsoft.Extensions.Logging;

namespace Luno.MissionControl.Application.UseCases;

/// <summary>
/// A high-fidelity simulator for the Basket Orchestrator.
/// Provides realistic delay and deterministic success without requiring API credentials or SDK connectivity.
/// </summary>
public sealed class SimulatedBasketOrchestrator(ILogger<SimulatedBasketOrchestrator> logger) : IBasketService
{
    private static readonly Random _rng = new();

    public async Task<BasketExecutionResponse> ExecuteAsync(ExecuteAllocationCommand command, CancellationToken ct = default)
    {
        // [FORENSIC SIGNAL] Log the incoming request for audit and test verification
        logger.LogInformation("[SIMULATION] Order request received for {TotalSpend} units. Assets: {Count}", 
            command.TotalSpend, command.Allocations.Count);

        // 1. Validation using Core Domain Models (Valid-by-Construction)
        // Note: Core model expects weights in 0-100 range, whereas DTO uses 0.0-1.0
        var domainAllocations = command.Allocations
            .Select(a => new Luno.MissionControl.Core.Models.Allocation(a.Pair, new Luno.MissionControl.Core.Models.AllocationWeight(a.Weight * 100m)))
            .ToList();

        try
        {
            var basket = new OrderBasket(command.TotalSpend, domainAllocations);
            List<OrderSummary> orders = [];

            // 2. Realistic "Orchestration" Delay
            logger.LogDebug("[SIMULATION] Resolving market metadata and verifying account state...");
            await Task.Delay(800, ct);

            foreach (var allocation in basket.Allocations)
            {
                logger.LogInformation("[SIMULATION] Processing allocation: {Pair} with target spend {Spend}", 
                    allocation.Pair, allocation.TargetSpend);

                // Simulate network latency for each order placement
                await Task.Delay(_rng.Next(150, 400), ct);

                // Generate a fake but realistic-looking Luno Order ID
                string simulatedOrderId = $"BX-{_rng.Next(100000, 999999)}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
                orders.Add(new OrderSummary(simulatedOrderId, allocation.Pair));
            }

            return new BasketExecutionResponse(true, orders);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Simulated basket execution failed");
            return new BasketExecutionResponse(false, [], ex.Message);
        }
    }
}

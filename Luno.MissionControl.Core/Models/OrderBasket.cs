using Luno.MissionControl.Core.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace Luno.MissionControl.Core.Models;

/// <summary>
/// Represents a single execution allocation within a basket.
/// </summary>
public record Allocation(string Pair, AllocationWeight Weight)
{
    /// <summary>
    /// The specific amount of the total spend allocated to this pair.
    /// </summary>
    public decimal TargetSpend { get; internal set; }
}

/// <summary>
/// Represents a validated collection of order allocations (The Basket).
/// </summary>
public record OrderBasket
{
    private const decimal Tolerance = 0.0001m;
    
    public decimal TotalSpend { get; }
    public IReadOnlyList<Allocation> Allocations { get; }

    public OrderBasket(decimal totalSpend, IEnumerable<Allocation> allocations)
    {
        TotalSpend = totalSpend;
        Allocations = [.. allocations];

        // 1. Invariant: No Duplicate Asset Pairs
        var pairDuplicates = Allocations.GroupBy(a => a.Pair).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (pairDuplicates.Any())
        {
            throw new LunoDomainException("An Order Basket must prohibit duplicate asset pairs.");
        }

        // 2. Invariant: Total Weight Equivalence (100%)
        var totalWeight = Allocations.Sum(a => (decimal)a.Weight);
        if (Math.Abs(totalWeight - 100.0m) > Tolerance)
        {
            throw new LunoDomainException("An Order Basket must ensure the sum of its asset weights equals exactly 100%.");
        }

        // 3. Calculate Target Spends
        foreach (var allocation in Allocations)
        {
            allocation.TargetSpend = TotalSpend * (decimal)allocation.Weight / 100.0m;
        }
    }
}

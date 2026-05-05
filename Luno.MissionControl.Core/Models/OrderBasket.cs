using Luno.MissionControl.Core.Exceptions;

namespace Luno.MissionControl.Core.Models;

/// <summary>
/// Represents a validated collection of order allocations (The Basket).
/// </summary>
public record OrderBasket
{
    private const decimal Tolerance = 0.0001m;
    public IReadOnlyList<Allocation> Allocations { get; }

    public OrderBasket(IEnumerable<Allocation> allocations)
    {
        Allocations = [.. allocations];

        // 1. Invariant: No Duplicate Assets
        var assetDuplicates = Allocations.GroupBy(a => a.Asset).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (assetDuplicates.Any())
        {
            throw new LunoDomainException("An Order Basket must prohibit duplicate asset pairs.");
        }

        // 2. Invariant: Total Weight Equivalence (100%)
        var totalWeight = Allocations.Sum(a => (decimal)a.Weight);
        if (Math.Abs(totalWeight - 100.0m) > Tolerance)
        {
            throw new LunoDomainException("An Order Basket must ensure the sum of its asset weights equals exactly 100% (within 0.0001% tolerance).");
        }
    }
}

/// <summary>
/// Represents a single execution allocation within a basket.
/// </summary>
public record Allocation(string Asset, AllocationWeight Weight);

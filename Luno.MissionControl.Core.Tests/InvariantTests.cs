using Luno.MissionControl.Core.Exceptions;
using Luno.MissionControl.Core.Models;
using Xunit;

namespace Luno.MissionControl.Core.Tests;

public class InvariantTests
{
    [Fact(DisplayName = "An Allocation Weight must represent a real number between 0.0 and 100.0 to prevent invalid allocation math.")]
    public void Weight_Should_Enforce_Percentage_Bounds()
    {
        // Act & Assert
        Assert.Throws<LunoDomainException>(() => new AllocationWeight(-1.0m));
        Assert.Throws<LunoDomainException>(() => new AllocationWeight(100.0001m));
        
        var validWeight = new AllocationWeight(50.0m);
        Assert.Equal(50.0m, (decimal)validWeight);
    }

    [Fact(DisplayName = "An Order Basket must ensure the sum of its asset weights equals exactly 100% (within 0.0001% tolerance).")]
    public void Basket_Should_Enforce_Total_Weight_Equivalence()
    {
        // Arrange
        var invalidAllocations = new List<Allocation>
        {
            new("BTC", new AllocationWeight(50.0m)),
            new("ETH", new AllocationWeight(49.9m)) // 99.9%
        };

        var validAllocations = new List<Allocation>
        {
            new("BTC", new AllocationWeight(50.0m)),
            new("ETH", new AllocationWeight(50.0m)) // 100%
        };

        // Act & Assert
        Assert.Throws<LunoDomainException>(() => new OrderBasket(1000m, invalidAllocations));
        
        var basket = new OrderBasket(1000m, validAllocations);
        Assert.Equal(2, basket.Allocations.Count);
        Assert.Equal(500m, basket.Allocations[0].TargetSpend);
    }

    [Fact(DisplayName = "An Order Basket must prohibit duplicate asset pairs.")]
    public void Basket_Should_Reject_Duplicate_Assets()
    {
        // Arrange
        var duplicateAllocations = new List<Allocation>
        {
            new("XBTUSDC", new AllocationWeight(50.0m)),
            new("XBTUSDC", new AllocationWeight(50.0m))
        };

        // Act & Assert
        Assert.Throws<LunoDomainException>(() => new OrderBasket(1000m, duplicateAllocations));
    }

    [Fact(DisplayName = "An Order Basket must respect the Current policy constraints limit on asset count.")]
    public void Basket_Should_Enforce_Capacity_Limit()
    {
        // Arrange
        // Generating 201 unique assets to exceed the 200 limit.
        var overstuffedAllocations = Enumerable.Range(1, 201)
            .Select(i => new Allocation($"PAIR_{i}", new AllocationWeight(100.0m / 201.0m)))
            .ToList();

        // Act & Assert
        var ex = Assert.Throws<LunoDomainException>(() => new OrderBasket(1000m, overstuffedAllocations));
        Assert.Contains("Current policy constraints limit order basket size to 200 assets", ex.Message);
    }
}

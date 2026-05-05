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
        Assert.Throws<LunoDomainException>(() => new OrderBasket(invalidAllocations));
        
        var basket = new OrderBasket(validAllocations);
        Assert.Equal(2, basket.Allocations.Count);
    }

    [Fact(DisplayName = "An Order Basket must prohibit duplicate asset pairs.")]
    public void Basket_Should_Reject_Duplicate_Assets()
    {
        // Arrange
        var duplicateAllocations = new List<Allocation>
        {
            new("BTC", new AllocationWeight(50.0m)),
            new("BTC", new AllocationWeight(50.0m))
        };

        // Act & Assert
        Assert.Throws<LunoDomainException>(() => new OrderBasket(duplicateAllocations));
    }
}

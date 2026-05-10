using Luno.MissionControl.Core.Exceptions;
using Luno.MissionControl.Core.Models;
using Luno.MissionControl.Core.Services;
using Xunit;
using WalletAmbiguityException = Luno.MissionControl.Core.Exceptions.WalletAmbiguityException;

namespace Luno.MissionControl.Core.Tests;

public class WalletResolverTests
{
    [Fact(DisplayName = "Given an asset with exactly one associated Luno account, the resolver must automatically select that account to ensure seamless execution.")]
    public void Given_A_Single_Account_When_Resolving_Then_It_Should_Return_The_Account()
    {
        // Arrange
        var resolver = new WalletResolver();
        var candidates = new[]
        {
            new LunoAccount { Id = 123, Name = "XBT Wallet", Balance = 1.0m }
        };

        // Act
        var result = resolver.Resolve(candidates, "XBT", null);

        // Assert
        Assert.Equal(123, result.Id);
    }

    [Fact(DisplayName = "Given an asset with multiple accounts and a valid user preference, the resolver must strictly adhere to the pinned selection to ensure deterministic order placement.")]
    public void Given_Multiple_Accounts_With_A_Pinned_Preference_When_Resolving_Then_It_Should_Return_The_Preferred_Account()
    {
        // Arrange
        var resolver = new WalletResolver();
        var candidates = new[]
        {
            new LunoAccount { Id = 101, Name = "XBT Trading", Balance = 0.5m },
            new LunoAccount { Id = 102, Name = "XBT Savings", Balance = 2.0m }
        };
        var preference = new TradingAccountPreference
        {
            CurrencyCode = "XBT",
            AccountId = 101,
            LastUpdated = DateTime.UtcNow
        };

        // Act
        var result = resolver.Resolve(candidates, "XBT", preference);

        // Assert
        Assert.Equal(101, result.Id);
    }

    [Fact(DisplayName = "Given an asset with multiple accounts but no user preference, the resolver must fail-fast with an ambiguity exception to prevent non-deterministic execution.")]
    public void Given_Multiple_Accounts_Without_A_Preference_When_Resolving_Then_It_Should_Throw_WalletAmbiguityException()
    {
        // Arrange
        var resolver = new WalletResolver();
        var candidates = new[]
        {
            new LunoAccount { Id = 101, Name = "XBT Trading", Balance = 0.5m },
            new LunoAccount { Id = 102, Name = "XBT Savings", Balance = 2.0m }
        };

        // Act & Assert
        Assert.Throws<WalletAmbiguityException>(() => resolver.Resolve(candidates, "XBT", null));
    }

    [Fact(DisplayName = "Given an asset with multiple accounts and a preference that no longer matches any live account, the resolver must treat the state as ambiguous and fail-fast.")]
    public void Given_Multiple_Accounts_With_An_Invalid_Preference_When_Resolving_Then_It_Should_Throw_WalletAmbiguityException()
    {
        // Arrange
        var resolver = new WalletResolver();
        var candidates = new[]
        {
            new LunoAccount { Id = 101, Name = "XBT Trading", Balance = 0.5m },
            new LunoAccount { Id = 102, Name = "XBT Savings", Balance = 2.0m }
        };
        var stalePreference = new TradingAccountPreference
        {
            CurrencyCode = "XBT",
            AccountId = 999, // Doesn't match any live account
            LastUpdated = DateTime.UtcNow
        };

        // Act & Assert
        Assert.Throws<WalletAmbiguityException>(() => resolver.Resolve(candidates, "XBT", stalePreference));
    }

    [Fact(DisplayName = "Given a request to resolve an asset that has no associated Luno accounts, the resolver must throw a descriptive exception to facilitate diagnostic troubleshooting.")]
    public void Given_No_Accounts_When_Resolving_Then_It_Should_Throw_WalletNotFoundException()
    {
        // Arrange
        var resolver = new WalletResolver();
        var candidates = Array.Empty<LunoAccount>();

        // Act & Assert
        Assert.Throws<WalletNotFoundException>(() => resolver.Resolve(candidates, "XBT", null));
    }
}

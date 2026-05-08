using Luno.MissionControl.Core.Exceptions;
using Luno.MissionControl.Core.Models;
using Luno.MissionControl.Core.Services;
using Xunit;

namespace Luno.MissionControl.Core.Tests;

public class WalletResolverTests
{
    [Fact(DisplayName = "Given an asset with exactly one associated Luno account, the resolver must automatically select that account to ensure seamless execution.")]
    public void Given_A_Single_Account_When_Resolving_Then_It_Should_Return_The_Account()
    {
        // Arrange
        var resolver = new WalletResolver();
        var accounts = new[]
        {
            new LunoAccount { Id = 123, Name = "XBT Wallet", Currency = "XBT", Balance = 1.0m },
            new LunoAccount { Id = 456, Name = "MYR Wallet", Currency = "MYR", Balance = 1000m }
        };

        // Act
        var result = resolver.Resolve(accounts, "XBT", null);

        // Assert
        Assert.Equal(123, result.Id);
    }

    [Fact(DisplayName = "Given an asset with multiple accounts and a valid user preference, the resolver must strictly adhere to the pinned selection to ensure deterministic order placement.")]
    public void Given_Multiple_Accounts_With_A_Pinned_Preference_When_Resolving_Then_It_Should_Return_The_Preferred_Account()
    {
        // Arrange
        var resolver = new WalletResolver();
        var accounts = new[]
        {
            new LunoAccount { Id = 101, Name = "XBT Trading", Currency = "XBT", Balance = 0.5m },
            new LunoAccount { Id = 102, Name = "XBT Savings", Currency = "XBT", Balance = 2.0m }
        };
        var preference = new TradingAccountPreference 
        { 
            CurrencyCode = "XBT", 
            BaseAccountId = 101, 
            CounterAccountId = 0 // Irrelevant for this test
        };

        // Act
        // Resolving as Base account
        var result = resolver.Resolve(accounts, "XBT", preference, isBase: true);

        // Assert
        Assert.Equal(101, result.Id);
    }

    [Fact(DisplayName = "Given an asset with multiple accounts but no user preference, the resolver must fail-fast with an ambiguity exception to prevent non-deterministic execution.")]
    public void Given_Multiple_Accounts_Without_A_Preference_When_Resolving_Then_It_Should_Throw_WalletAmbiguityException()
    {
        // Arrange
        var resolver = new WalletResolver();
        var accounts = new[]
        {
            new LunoAccount { Id = 101, Name = "XBT Trading", Currency = "XBT", Balance = 0.5m },
            new LunoAccount { Id = 102, Name = "XBT Savings", Currency = "XBT", Balance = 2.0m }
        };

        // Act & Assert
        Assert.Throws<WalletAmbiguityException>(() => resolver.Resolve(accounts, "XBT", null));
    }

    [Fact(DisplayName = "Given an asset with multiple accounts and a preference that no longer matches any live account, the resolver must treat the state as ambiguous and fail-fast.")]
    public void Given_Multiple_Accounts_With_An_Invalid_Preference_When_Resolving_Then_It_Should_Throw_WalletAmbiguityException()
    {
        // Arrange
        var resolver = new WalletResolver();
        var accounts = new[]
        {
            new LunoAccount { Id = 101, Name = "XBT Trading", Currency = "XBT", Balance = 0.5m },
            new LunoAccount { Id = 102, Name = "XBT Savings", Currency = "XBT", Balance = 2.0m }
        };
        var stalePreference = new TradingAccountPreference 
        { 
            CurrencyCode = "XBT", 
            BaseAccountId = 999, // Doesn't exist anymore
            CounterAccountId = 0 
        };

        // Act & Assert
        Assert.Throws<WalletAmbiguityException>(() => resolver.Resolve(accounts, "XBT", stalePreference, isBase: true));
    }

    [Fact(DisplayName = "Given a request to resolve an asset that has no associated Luno accounts, the resolver must throw a descriptive exception to facilitate diagnostic troubleshooting.")]
    public void Given_No_Accounts_When_Resolving_Then_It_Should_Throw_WalletNotFoundException()
    {
        // Arrange
        var resolver = new WalletResolver();
        var accounts = new[]
        {
            new LunoAccount { Id = 456, Name = "MYR Wallet", Currency = "MYR", Balance = 1000m }
        };

        // Act & Assert
        Assert.Throws<WalletNotFoundException>(() => resolver.Resolve(accounts, "XBT", null));
    }
}

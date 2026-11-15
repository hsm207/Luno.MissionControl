using Luno.MissionControl.Core.Exceptions;
using Luno.MissionControl.Core.Models;
using Luno.MissionControl.Core.Services;
using Xunit;

namespace Luno.MissionControl.Core.Tests;

/// <summary>Unit tests for <see cref="WalletResolver"/> SPOT account resolution.</summary>
public class WalletResolverTests
{
    private const long TransactionalAccountId = 101L;
    private const long SpotAccountId = 102L;
    private const long MissingAccountId = 999L;
    private const decimal DefaultBalance = 1.0m;
    private const decimal SpotBalance = 2.0m;
    private const decimal TransactionalBalance = 0.5m;

    private readonly WalletResolver _resolver = new();

    /// <summary>Given a pinned SPOT account, Returns its AccountId.</summary>
    [Fact(DisplayName = "Given a pinned SPOT account among candidates, "
        + "When ResolveSpotAccountId runs, Then return the pinned AccountId")]
    public void Given_Pinned_Spot_Account_When_ResolveSpotAccountId_Then_Returns_AccountId()
    {
        LunoAccount[] candidates =
        [
            Account(TransactionalAccountId, "TRANSACTIONAL", "XBT Transactional", TransactionalBalance),
            Account(SpotAccountId, "SPOT", "XBT Spot", SpotBalance)
        ];

        var result = _resolver.ResolveSpotAccountId(candidates, "XBT", Preference(SpotAccountId));

        Assert.Equal(SpotAccountId, result);
    }

    /// <summary>Given an unpinned asset, Throws <see cref="UnpinnedWalletException"/>.</summary>
    [Fact(DisplayName = "Given an unpinned asset, When ResolveSpotAccountId runs, "
        + "Then throw UnpinnedWalletException")]
    public void Given_Unpinned_Asset_When_ResolveSpotAccountId_Then_Throws_UnpinnedWalletException()
    {
        LunoAccount[] candidates = [Account(SpotAccountId, "SPOT", "XBT Spot")];

        Assert.Throws<UnpinnedWalletException>(() =>
            _resolver.ResolveSpotAccountId(candidates, "XBT", null));
    }

    /// <summary>Given a pinned non-SPOT account type, Throws <see cref="NonSpotWalletException"/>.</summary>
    [Theory(DisplayName = "Given a pinned non-SPOT account type, When ResolveSpotAccountId runs, "
        + "Then throw NonSpotWalletException")]
    [InlineData("TRANSACTIONAL")]
    [InlineData("STAKING")]
    [InlineData("SAVINGS")]
    [InlineData("EARN")]
    [InlineData("")]
    public void Given_Pinned_NonSpot_Account_When_ResolveSpotAccountId_Then_Throws_NonSpotWalletException(
        string accountType)
    {
        LunoAccount[] candidates = [Account(TransactionalAccountId, accountType, "XBT Non-Spot")];

        Assert.Throws<NonSpotWalletException>(() =>
            _resolver.ResolveSpotAccountId(candidates, "XBT", Preference(TransactionalAccountId)));
    }

    /// <summary>Given a stale pinned AccountId, Throws <see cref="PinnedWalletNotFoundException"/>.</summary>
    [Fact(DisplayName = "Given a pinned AccountId absent from live candidates, "
        + "When ResolveSpotAccountId runs, Then throw PinnedWalletNotFoundException")]
    public void Given_Pinned_Account_Missing_When_ResolveSpotAccountId_Then_Throws_PinnedWalletNotFoundException()
    {
        LunoAccount[] candidates = [Account(SpotAccountId, "SPOT", "XBT Spot")];

        Assert.Throws<PinnedWalletNotFoundException>(() =>
            _resolver.ResolveSpotAccountId(candidates, "XBT", Preference(MissingAccountId)));
    }

    private static LunoAccount Account(
        long id,
        string accountType,
        string name,
        decimal balance = DefaultBalance) =>
        new() { Id = id, Name = name, Balance = balance, AccountType = accountType };

    private static TradingAccountPreference Preference(long accountId) =>
        new() { CurrencyCode = "XBT", AccountId = accountId, LastUpdated = DateTime.UtcNow };
}

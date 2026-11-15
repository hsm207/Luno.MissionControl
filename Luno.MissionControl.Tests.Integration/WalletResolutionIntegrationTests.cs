using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Xunit;
using Luno.MissionControl.Application;
using Luno.MissionControl.Application.Ports;
using Luno.MissionControl.Application.Commands;
using Luno.MissionControl.Infrastructure;
using Luno.MissionControl.Infrastructure.Adapters;
using CoreModels = Luno.MissionControl.Core.Models;
using Luno.SDK;

namespace Luno.MissionControl.Tests.Integration;

/// <summary>Integration tests for <see cref="IBasketService"/> pre-trade SPOT wallet guards.</summary>
public class WalletResolutionIntegrationTests
{
    private const long EthSpotId = 102L;
    private const long EthSavingsSpotId = 103L;
    private const long MyrSpotId = 201L;
    private const long XrpTransactionalId = 301L;
    private const decimal BasketAmount = 100m;
    private const decimal FullAllocationWeight = 1.0m;
    private const decimal HalfAllocationWeight = 0.5m;

    private readonly IServiceProvider _serviceProvider;
    private readonly StubAccountAdapter _stubAccountAdapter = new();
    private readonly FakeWalletRepository _fakeWalletRepo = new();

    /// <summary>Initializes DI container with in-memory adapters and spy trader.</summary>
    public WalletResolutionIntegrationTests()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Luno:ApiKeyId"] = "test-id",
                ["Luno:ApiKeySecret"] = "test-secret"
            })
            .Build();

        services.AddInfrastructureServices(configuration);
        services.AddApplicationServices(isDevelopment: false);
        services.AddSingleton<ILunoAccountAdapter>(_stubAccountAdapter);
        services.AddSingleton<IWalletRepository>(_fakeWalletRepo);
        services.AddScoped<ILunoTrader>(sp => new SpyTrader(sp.GetRequiredService<LunoSdkExchangeAdapter>()));
        services.AddLogging(builder => builder.AddConsole());
        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>Given all assets pinned to SPOT, Places order with pinned AccountIds.</summary>
    [Fact(DisplayName = "Given all assets pinned to SPOT accounts, "
        + "When ExecuteAsync runs, Then place order with pinned AccountIds")]
    public async Task ExecuteAsync_WhenAllAssetsPinnedToSpot_ThenPlacesOrderWithPinnedAccountIds()
    {
        var orchestrator = _serviceProvider.GetRequiredService<IBasketService>();
        _stubAccountAdapter.Accounts["ETH"] = [
            new() { Id = EthSpotId, Name = "Main ETH", AccountType = "SPOT" },
            new() { Id = EthSavingsSpotId, Name = "Savings ETH", AccountType = "SPOT" }
        ];
        _stubAccountAdapter.Accounts["MYR"] = [new() { Id = MyrSpotId, Name = "Main MYR", AccountType = "SPOT" }];
        await SavePinAsync("ETH", EthSpotId);
        await SavePinAsync("MYR", MyrSpotId);

        var result = await orchestrator.ExecuteAsync(
            new ExecuteAllocationCommand(BasketAmount, [new AllocationRequest("ETHMYR", FullAllocationWeight)]));

        Assert.True(result.Success, $"Execution failed: {result.ErrorMessage}");
        var call = Assert.Single(((SpyTrader)_serviceProvider.GetRequiredService<ILunoTrader>()).Calls);
        Assert.Equal((EthSpotId, MyrSpotId), call);
    }

    /// <summary>Given an unpinned single account, Refuses execution and places zero orders.</summary>
    [Fact(DisplayName = "Given an unpinned single account, "
        + "When ExecuteAsync runs, Then refuse and place zero orders")]
    public async Task ExecuteAsync_WhenAssetUnpinned_ThenRefusesAndPlacesZeroOrders()
    {
        var orchestrator = _serviceProvider.GetRequiredService<IBasketService>();
        _stubAccountAdapter.Accounts["ETH"] = [new() { Id = EthSpotId, Name = "Solo ETH", AccountType = "SPOT" }];
        _stubAccountAdapter.Accounts["MYR"] = [new() { Id = MyrSpotId, Name = "Solo MYR", AccountType = "SPOT" }];

        var result = await orchestrator.ExecuteAsync(
            new ExecuteAllocationCommand(BasketAmount, [new AllocationRequest("ETHMYR", FullAllocationWeight)]));

        var spy = (SpyTrader)_serviceProvider.GetRequiredService<ILunoTrader>();
        Assert.False(result.Success);
        Assert.Contains("no pinned SPOT trading wallet", result.ErrorMessage);
        Assert.Empty(spy.Calls);
    }

    /// <summary>Given a later asset pinned to TRANSACTIONAL, Refuses entire basket with zero orders.</summary>
    [Fact(DisplayName = "Given a multi-allocation basket where a later asset is pinned to TRANSACTIONAL, "
        + "When ExecuteAsync runs, Then refuse and place zero orders")]
    public async Task ExecuteAsync_WhenSecondAllocationHasNonSpotPin_ThenRefusesAndPlacesZeroOrders()
    {
        var orchestrator = _serviceProvider.GetRequiredService<IBasketService>();
        _stubAccountAdapter.Accounts["ETH"] = [new() { Id = EthSpotId, Name = "ETH Spot", AccountType = "SPOT" }];
        _stubAccountAdapter.Accounts["MYR"] = [new() { Id = MyrSpotId, Name = "MYR Spot", AccountType = "SPOT" }];
        _stubAccountAdapter.Accounts["XRP"] =
            [new() { Id = XrpTransactionalId, Name = "XRP Transactional", AccountType = "TRANSACTIONAL" }];
        await SavePinAsync("ETH", EthSpotId);
        await SavePinAsync("MYR", MyrSpotId);
        await SavePinAsync("XRP", XrpTransactionalId);

        var result = await orchestrator.ExecuteAsync(new ExecuteAllocationCommand(
            BasketAmount,
            [new AllocationRequest("ETHMYR", HalfAllocationWeight), new AllocationRequest("XRPMYR", HalfAllocationWeight)]));

        var spy = (SpyTrader)_serviceProvider.GetRequiredService<ILunoTrader>();
        Assert.False(result.Success);
        Assert.Empty(spy.Calls);
    }

    private Task SavePinAsync(string asset, long accountId) =>
        _fakeWalletRepo.SavePreferenceAsync(
            new() { CurrencyCode = asset, AccountId = accountId, LastUpdated = DateTime.UtcNow });
}

/// <summary>Spy <see cref="ILunoTrader"/> recording placed account IDs without hitting Luno exchange.</summary>
public class SpyTrader(ILunoTrader realTrader) : ILunoTrader
{
    public List<(long BaseId, long CounterId)> Calls { get; } = [];

    /// <inheritdoc />
    public Task<CoreModels.OrderEstimation> EstimateOrderAsync(string pair, decimal spend, CancellationToken ct = default)
        => realTrader.EstimateOrderAsync(pair, spend, ct);

    /// <inheritdoc />
    public Task<string> PostOrderAsync(
        CoreModels.OrderEstimation estimation,
        long baseAccountId,
        long counterAccountId,
        CancellationToken ct = default)
    {
        Calls.Add((baseAccountId, counterAccountId));
        return Task.FromResult("order-stub-123");
    }
}

/// <summary>In-memory stub for <see cref="ILunoAccountAdapter"/>.</summary>
public class StubAccountAdapter : ILunoAccountAdapter
{
    public Dictionary<string, List<CoreModels.LunoAccount>> Accounts { get; } = [];

    /// <inheritdoc />
    public Task<IDictionary<string, List<CoreModels.LunoAccount>>> GetAccountsAsync(CancellationToken ct = default) =>
        Task.FromResult<IDictionary<string, List<CoreModels.LunoAccount>>>(
            Accounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
}

/// <summary>In-memory fake for <see cref="IWalletRepository"/>.</summary>
public class FakeWalletRepository : IWalletRepository
{
    public Dictionary<string, CoreModels.TradingAccountPreference> Preferences { get; } = [];

    /// <inheritdoc />
    public Task<CoreModels.TradingAccountPreference?> GetPreferenceAsync(string key, CancellationToken ct = default)
    {
        Preferences.TryGetValue(key, out var pref);
        return Task.FromResult(pref);
    }

    /// <inheritdoc />
    public Task SavePreferenceAsync(CoreModels.TradingAccountPreference preference, CancellationToken ct = default)
    {
        Preferences[preference.CurrencyCode] = preference;
        return Task.CompletedTask;
    }
}

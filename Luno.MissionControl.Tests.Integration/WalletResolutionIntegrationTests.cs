using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Xunit;
using Luno.MissionControl.Application;
using Luno.MissionControl.Application.Ports;
using Luno.MissionControl.Application.UseCases;
using Luno.MissionControl.Application.Commands;
using Luno.MissionControl.Application.Models;
using Luno.MissionControl.Infrastructure;
using Luno.MissionControl.Infrastructure.Adapters;
using CoreModels = Luno.MissionControl.Core.Models;
using Luno.SDK;

namespace Luno.MissionControl.Tests.Integration;

public class WalletResolutionIntegrationTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly StubAccountAdapter _stubAccountAdapter = new();
    private readonly FakeWalletRepository _fakeWalletRepo = new();

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

    [Fact(DisplayName = "Given all assets pinned to SPOT accounts, When ExecuteAsync runs, Then place order with pinned AccountIds")]
    public async Task ExecuteAsync_WhenAllAssetsPinnedToSpot_ThenPlacesOrderWithPinnedAccountIds()
    {
        // --- 1. ARRANGE ---
        var orchestrator = _serviceProvider.GetRequiredService<IBasketService>();
        _stubAccountAdapter.Accounts["ETH"] = [
            new() { Id = 102, Name = "Main ETH", AccountType = "SPOT" },
            new() { Id = 103, Name = "Savings ETH", AccountType = "SPOT" }
        ];
        _stubAccountAdapter.Accounts["MYR"] = [new() { Id = 201, Name = "Main MYR", AccountType = "SPOT" }];
        await _fakeWalletRepo.SavePreferenceAsync(new() { CurrencyCode = "ETH", AccountId = 102, LastUpdated = DateTime.UtcNow });
        await _fakeWalletRepo.SavePreferenceAsync(new() { CurrencyCode = "MYR", AccountId = 201, LastUpdated = DateTime.UtcNow });

        // --- 2. ACT ---
        var command = new ExecuteAllocationCommand(100m, [new AllocationRequest("ETHMYR", 1.0m)]);
        var result = await orchestrator.ExecuteAsync(command);

        // --- 3. ASSERT ---
        Assert.True(result.Success, $"Execution failed: {result.ErrorMessage}");
        var call = Assert.Single(((SpyTrader)_serviceProvider.GetRequiredService<ILunoTrader>()).Calls);
        Assert.Equal((102, 201), call);
    }

    [Fact(DisplayName = "Given an unpinned single account, When ExecuteAsync runs, Then refuse and place zero orders")]
    public async Task ExecuteAsync_WhenAssetUnpinned_ThenRefusesAndPlacesZeroOrders()
    {
        // --- 1. ARRANGE ---
        var orchestrator = _serviceProvider.GetRequiredService<IBasketService>();
        _stubAccountAdapter.Accounts["ETH"] = [new() { Id = 102, Name = "Solo ETH", AccountType = "SPOT" }];
        _stubAccountAdapter.Accounts["MYR"] = [new() { Id = 201, Name = "Solo MYR", AccountType = "SPOT" }];

        // --- 2. ACT ---
        var command = new ExecuteAllocationCommand(100m, [new AllocationRequest("ETHMYR", 1.0m)]);
        var result = await orchestrator.ExecuteAsync(command);

        // --- 3. ASSERT ---
        var spy = (SpyTrader)_serviceProvider.GetRequiredService<ILunoTrader>();
        Assert.False(result.Success);
        Assert.Contains("no pinned SPOT trading wallet", result.ErrorMessage);
        Assert.Empty(spy.Calls);
    }

    [Fact(DisplayName = "Given a multi-allocation basket where a later asset is pinned to TRANSACTIONAL, When ExecuteAsync runs, Then refuse and place zero orders")]
    public async Task ExecuteAsync_WhenSecondAllocationHasNonSpotPin_ThenRefusesAndPlacesZeroOrders()
    {
        // --- 1. ARRANGE ---
        var orchestrator = _serviceProvider.GetRequiredService<IBasketService>();
        _stubAccountAdapter.Accounts["ETH"] = [new() { Id = 102, Name = "ETH Spot", AccountType = "SPOT" }];
        _stubAccountAdapter.Accounts["MYR"] = [new() { Id = 201, Name = "MYR Spot", AccountType = "SPOT" }];
        _stubAccountAdapter.Accounts["XRP"] = [new() { Id = 301, Name = "XRP Transactional", AccountType = "TRANSACTIONAL" }];
        await _fakeWalletRepo.SavePreferenceAsync(new() { CurrencyCode = "ETH", AccountId = 102, LastUpdated = DateTime.UtcNow });
        await _fakeWalletRepo.SavePreferenceAsync(new() { CurrencyCode = "MYR", AccountId = 201, LastUpdated = DateTime.UtcNow });
        await _fakeWalletRepo.SavePreferenceAsync(new() { CurrencyCode = "XRP", AccountId = 301, LastUpdated = DateTime.UtcNow });

        // --- 2. ACT ---
        var command = new ExecuteAllocationCommand(100m, [new AllocationRequest("ETHMYR", 0.5m), new AllocationRequest("XRPMYR", 0.5m)]);
        var result = await orchestrator.ExecuteAsync(command);

        // --- 3. ASSERT ---
        var spy = (SpyTrader)_serviceProvider.GetRequiredService<ILunoTrader>();
        Assert.False(result.Success);
        Assert.Empty(spy.Calls);
    }
}

// --- SUPPORTING TEST DOUBLES ---

public class SpyTrader(ILunoTrader realTrader) : ILunoTrader
{
    public List<(long BaseId, long CounterId)> Calls { get; } = [];

    public Task<CoreModels.OrderEstimation> EstimateOrderAsync(string pair, decimal spend, CancellationToken ct = default)
        => realTrader.EstimateOrderAsync(pair, spend, ct);

    public Task<string> PostOrderAsync(CoreModels.OrderEstimation estimation, long baseAccountId, long counterAccountId, CancellationToken ct = default)
    {
        Calls.Add((baseAccountId, counterAccountId));
        return Task.FromResult("order-stub-123");
    }
}

public class StubAccountAdapter : ILunoAccountAdapter
{
    public Dictionary<string, List<CoreModels.LunoAccount>> Accounts { get; } = [];

    public Task<IDictionary<string, List<CoreModels.LunoAccount>>> GetAccountsAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IDictionary<string, List<CoreModels.LunoAccount>>>(
            Accounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
    }
}

public class FakeWalletRepository : IWalletRepository
{
    public Dictionary<string, CoreModels.TradingAccountPreference> Preferences { get; } = [];

    public Task<CoreModels.TradingAccountPreference?> GetPreferenceAsync(string key, CancellationToken ct = default)
    {
        Preferences.TryGetValue(key, out var pref);
        return Task.FromResult(pref);
    }

    public Task SavePreferenceAsync(CoreModels.TradingAccountPreference preference, CancellationToken ct = default)
    {
        Preferences[preference.CurrencyCode] = preference;
        return Task.CompletedTask;
    }
}

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

    public static IEnumerable<object[]> ResolutionScenarios =>
        new List<object[]>
        {
            // Scenario 1: Multiple Accounts + Preference = Success ✅
            new object[] { 
                new List<CoreModels.LunoAccount> { 
                    new() { Id = 102, Name = "Main ETH" }, 
                    new() { Id = 103, Name = "Savings ETH" } 
                },
                new List<CoreModels.LunoAccount> { new() { Id = 201, Name = "Main MYR" } },
                new List<CoreModels.TradingAccountPreference> { 
                    new() { CurrencyCode = "ETH", AccountId = 102, LastUpdated = DateTime.UtcNow },
                    new() { CurrencyCode = "MYR", AccountId = 201, LastUpdated = DateTime.UtcNow }
                },
                true
            },

            // Scenario 2: Single Account + No Preference = Success ✅
            new object[] { 
                new List<CoreModels.LunoAccount> { new() { Id = 102, Name = "Solo ETH" } },
                new List<CoreModels.LunoAccount> { new() { Id = 201, Name = "Solo MYR" } },
                new List<CoreModels.TradingAccountPreference>(), 
                true
            },

            // Scenario 3: Multiple Accounts + No Preference = Ambiguity FAILURE ❌
            new object[] { 
                new List<CoreModels.LunoAccount> { 
                    new() { Id = 102, Name = "Main ETH" }, 
                    new() { Id = 103, Name = "Savings ETH" } 
                },
                new List<CoreModels.LunoAccount> { new() { Id = 201, Name = "Main MYR" } },
                new List<CoreModels.TradingAccountPreference>(), 
                false
            }
        };

    [Theory(DisplayName = "The Orchestrator must resolve the correct wallets based on user preferences or deterministic defaults.")]
    [MemberData(nameof(ResolutionScenarios))]
    public async Task ResolveWallets_ShouldAdhereToZeroAmbiguityMandate(
        List<CoreModels.LunoAccount> ethAccounts, 
        List<CoreModels.LunoAccount> myrAccounts,
        List<CoreModels.TradingAccountPreference> preferences,
        bool expectSuccess)
    {
        // --- 1. ARRANGE ---
        var orchestrator = _serviceProvider.GetRequiredService<IBasketService>();
        
        _stubAccountAdapter.Accounts["ETH"] = ethAccounts;
        _stubAccountAdapter.Accounts["MYR"] = myrAccounts;

        foreach (var pref in preferences)
        {
            await _fakeWalletRepo.SavePreferenceAsync(pref);
        }

        // --- 2. ACT ---
        // COMMAND HARDENED: No more manual ID overrides. Orchestrator MUST resolve.
        var command = new ExecuteAllocationCommand(100m, [new AllocationRequest("ETHMYR", 1.0m)]);
        var result = await orchestrator.ExecuteAsync(command);

        // --- 3. ASSERT ---
        if (expectSuccess)
        {
            Assert.True(result.Success, $"Execution failed: {result.ErrorMessage}");
            var spy = (SpyTrader)_serviceProvider.GetRequiredService<ILunoTrader>();
            
            Assert.NotEmpty(spy.Calls);
            var call = spy.Calls.Last();
            
            var expectedBaseId = preferences.FirstOrDefault(p => p.CurrencyCode == "ETH")?.AccountId ?? ethAccounts.First().Id;
            var expectedCounterId = preferences.FirstOrDefault(p => p.CurrencyCode == "MYR")?.AccountId ?? myrAccounts.First().Id;
            
            Assert.Equal(expectedBaseId, call.BaseId);
            Assert.Equal(expectedCounterId, call.CounterId);
        }
        else
        {
            Assert.False(result.Success, "Expected resolution to fail due to ambiguity.");
            Assert.Contains("Ambiguity", result.ErrorMessage);
        }
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

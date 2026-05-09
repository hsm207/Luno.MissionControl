using System.Net.Http.Json;
using Aspire.Hosting.Testing;
using Luno.MissionControl.Core.Models;
using Xunit;

namespace Luno.MissionControl.Tests.E2E;

/// <summary>
/// A high-fidelity persistence audit using .NET Aspire testing infrastructure.
/// Verifies the vertical slice from API to Postgres with strong domain model parity.
/// </summary>
public class WalletPersistenceIntegrationTests(MissionControlTestingApplicationFactory factory) 
    : IClassFixture<MissionControlTestingApplicationFactory>
{
    [Fact(DisplayName = "Scenario: Pinning an account via API must persist to Postgres and reflect in the overview")]
    public async Task PinAccount_ShouldPersistToDatabase_AndReflectInOverview()
    {
        // --- 1. ARRANGE ---
        var app = await factory.CreateAndStartAsync();
        var client = app.CreateHttpClient("webfrontend");
        
        const string asset = "SOL";
        const long accountId = 4078499081933439467L;

        // --- 2. ACT ---
        var pinResponse = await client.PostAsync($"/api/wallets/pin?asset={asset}&accountId={accountId}", null);
        pinResponse.EnsureSuccessStatusCode();

        // --- 3. ASSERT ---
        var overviewResponse = await client.GetFromJsonAsync<List<Wallet>>("/api/wallets/overview");
        
        Assert.NotNull(overviewResponse);
        var solWallet = overviewResponse.FirstOrDefault(w => w.Asset == asset);
        
        Assert.NotNull(solWallet);
        Assert.Equal(accountId, solWallet.ResolvedAccountId);
        Assert.False(solWallet.IsAmbiguous, "Wallet should no longer be ambiguous after pinning.");
        
        Assert.NotNull(solWallet.PinnedPreference);
        Assert.Equal(accountId, solWallet.PinnedPreference.AccountId);
    }
}

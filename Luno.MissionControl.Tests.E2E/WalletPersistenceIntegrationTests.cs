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
    private const long SolSpotAccountId = 4078499081933439467L;

    /// <summary>
    /// Verifies pinning a SOL account via HTTP POST persists to Postgres and reflects in GET overview.
    /// </summary>
    [Fact(DisplayName = "Scenario: Pinning an account via API must persist to Postgres and reflect in overview")]
    public async Task PinAccount_ShouldPersistToDatabase_AndReflectInOverview()
    {
        var app = await factory.CreateAndStartAsync();
        var client = app.CreateHttpClient("webfrontend");

        var pinResponse = await client.PostAsync(
            $"/api/wallets/pin?asset=SOL&accountId={SolSpotAccountId}", null);
        pinResponse.EnsureSuccessStatusCode();
        var overview = await client.GetFromJsonAsync<List<Wallet>>("/api/wallets/overview");

        AssertPinnedWallet(overview, "SOL", SolSpotAccountId);
    }

    private static void AssertPinnedWallet(
        List<Wallet>? overview,
        string asset,
        long expectedAccountId)
    {
        Assert.NotNull(overview);
        var wallet = Assert.Single(overview, w => w.Asset == asset);
        Assert.Equal(expectedAccountId, wallet.ResolvedAccountId);
        Assert.NotNull(wallet.PinnedPreference);
        Assert.Equal(expectedAccountId, wallet.PinnedPreference.AccountId);
    }
}

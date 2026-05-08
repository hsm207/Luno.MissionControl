using System.Net.Http.Json;
using Luno.MissionControl.Application.Ports;
using Luno.MissionControl.Core.Models;

namespace Luno.MissionControl.Web.Client.Adapters;

/// <summary>
/// A Client-side proxy for the IWalletOrchestrator that delegates orchestration calls to the BFF.
/// This allows the WASM client to trigger wallet operations without direct DB or SDK access.
/// </summary>
public class WalletServiceProxy(HttpClient httpClient) : IWalletOrchestrator
{
    public async Task<List<Wallet>> GetWalletOverviewAsync(CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<Wallet>>("/api/wallets/overview", ct) ?? [];
    }

    public async Task PinAccountAsync(string asset, long accountId, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsync($"/api/wallets/pin?asset={asset}&accountId={accountId}", null, ct);
        response.EnsureSuccessStatusCode();
    }
}

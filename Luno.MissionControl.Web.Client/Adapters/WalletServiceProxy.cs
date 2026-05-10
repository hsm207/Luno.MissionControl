using Luno.MissionControl.Application.Ports;
using Luno.MissionControl.Core.Models;
using Microsoft.Extensions.Logging;

namespace Luno.MissionControl.Web.Client.Adapters;

/// <summary>
/// A Client-side proxy for the IWalletOrchestrator that delegates orchestration calls to the BFF.
/// </summary>
public class WalletServiceProxy(HttpClient httpClient, ILogger<WalletServiceProxy> logger) 
    : BffProxyBase(httpClient, logger), IWalletOrchestrator
{
    public async Task<List<Wallet>> GetWalletOverviewAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<Wallet>>("/api/wallets/overview", ct);
    }

    public async Task PinAccountAsync(string asset, long accountId, CancellationToken ct = default)
    {
        // We use a generic object for the null request/response to satisfy the base class constraints
        await PostAsync<object, object>($"/api/wallets/pin?asset={asset}&accountId={accountId}", new { }, ct);
    }
}

using Luno.MissionControl.Application.Ports;
using Luno.MissionControl.Application.Diagnostics;
using Luno.MissionControl.Core.Models;
using Luno.SDK;
using Luno.SDK.Application.Account;
using Microsoft.Extensions.Logging;

namespace Luno.MissionControl.Infrastructure.Adapters;

/// <summary>
/// Infrastructure adapter that provides account and balance information from the Luno SDK.
/// </summary>
public class LunoSdkAccountAdapter(ILunoClient lunoClient, ILogger<LunoSdkAccountAdapter> logger) : ILunoAccountAdapter
{
    public async Task<IDictionary<string, List<LunoAccount>>> GetAccountsAsync(CancellationToken ct = default)
    {
        using var activity = ForensicTracing.StartActivity("LunoSDK.GetBalances");
        logger.LogDebug("Calling Luno SDK to fetch account balances...");

        try
        {
            var response = await lunoClient.Accounts.GetBalancesAsync(new GetBalancesQuery(), ct);
            
            activity?.SetTag("luno.account_count", response.Count());

            return response
                .GroupBy(b => b.Asset)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(b => new LunoAccount
                    {
                        Id = long.Parse(b.AccountId),
                        Name = b.Name,
                        Balance = b.Available
                    }).ToList()
                );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch balances from Luno SDK.");
            throw;
        }
    }
}

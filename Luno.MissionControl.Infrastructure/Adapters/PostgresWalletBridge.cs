using Microsoft.EntityFrameworkCore;
using Luno.MissionControl.Application.Ports;
using Luno.MissionControl.Core.Models;
using Luno.MissionControl.Infrastructure.Adapters.Persistence;

namespace Luno.MissionControl.Infrastructure.Adapters;

/// <summary>
/// PostgreSQL implementation of the IWalletRepository, leveraging Npgsql for high-performance persistence.
/// </summary>
public class PostgresWalletBridge(SettingsDbContext context) : IWalletRepository
{
    public async Task<TradingAccountPreference?> GetPreferenceAsync(string currencyCode, CancellationToken ct = default)
    {
        return await context.AccountPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.CurrencyCode == currencyCode, ct);
    }

    public async Task SavePreferenceAsync(TradingAccountPreference preference, CancellationToken ct = default)
    {
        var existing = await context.AccountPreferences
            .FirstOrDefaultAsync(p => p.CurrencyCode == preference.CurrencyCode, ct);

        if (existing is not null)
        {
            context.Entry(existing).CurrentValues.SetValues(preference);
        }
        else
        {
            await context.AccountPreferences.AddAsync(preference, ct);
        }

        await context.SaveChangesAsync(ct);
    }
}

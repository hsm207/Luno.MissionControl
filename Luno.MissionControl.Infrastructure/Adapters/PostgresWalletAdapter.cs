using Microsoft.EntityFrameworkCore;
using Luno.MissionControl.Application.Ports;
using Luno.MissionControl.Application.Diagnostics;
using Luno.MissionControl.Core.Models;
using Luno.MissionControl.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace Luno.MissionControl.Infrastructure.Adapters;

/// <summary>
/// PostgreSQL implementation of the IWalletRepository, leveraging Npgsql for high-performance persistence.
/// </summary>
public class PostgresWalletAdapter(SettingsDbContext context, ILogger<PostgresWalletAdapter> logger) : IWalletRepository
{
    public async Task<TradingAccountPreference?> GetPreferenceAsync(string currencyCode, CancellationToken ct = default)
    {
        using var activity = ForensicTracing.StartActivity("DB.GetPreference");
        activity?.SetTag("db.currency", currencyCode);
        
        logger.LogDebug("Fetching preference for {CurrencyCode} from Postgres...", currencyCode);

        return await context.AccountPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.CurrencyCode == currencyCode, ct);
    }

    public async Task SavePreferenceAsync(TradingAccountPreference preference, CancellationToken ct = default)
    {
        using var activity = ForensicTracing.StartActivity("DB.SavePreference");
        activity?.SetTag("db.currency", preference.CurrencyCode);

        logger.LogInformation("Saving preference for {CurrencyCode} to Postgres...", preference.CurrencyCode);

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

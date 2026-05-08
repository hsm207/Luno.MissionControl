using Microsoft.EntityFrameworkCore;
using Luno.MissionControl.Core.Models;

namespace Luno.MissionControl.Infrastructure.Persistence;

/// <summary>
/// A high-performance, isolated DbContext for Mission Control settings.
/// </summary>
public class SettingsDbContext(DbContextOptions<SettingsDbContext> options) : DbContext(options)
{
    /// <summary>
    /// User preferences for Luno trading accounts.
    /// </summary>
    public DbSet<TradingAccountPreference> AccountPreferences => Set<TradingAccountPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TradingAccountPreference>(entity =>
        {
            entity.ToTable("AccountPreferences");
            entity.HasKey(e => e.CurrencyCode);
            
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(e => e.BaseAccountId)
                .IsRequired();

            entity.Property(e => e.CounterAccountId)
                .IsRequired();
        });
    }
}

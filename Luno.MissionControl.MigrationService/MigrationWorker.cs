using Luno.MissionControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Luno.MissionControl.MigrationService;

public class MigrationWorker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<MigrationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SettingsDbContext>();

        logger.LogInformation("Starting database migration with execution strategy... 🫦🚀");

        try
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await dbContext.Database.MigrateAsync(stoppingToken);
            });

            logger.LogInformation("Database migration completed successfully! 🫦🏁✨");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database! 😱💥");
            throw;
        }

        hostApplicationLifetime.StopApplication();
    }
}

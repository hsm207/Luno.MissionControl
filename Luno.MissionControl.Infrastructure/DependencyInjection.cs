using Luno.MissionControl.Application.Ports;
using Luno.MissionControl.Infrastructure.Adapters;
using Luno.MissionControl.Infrastructure.Adapters.Persistence;
using Luno.SDK;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Luno.MissionControl.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLunoClient(options =>
        {
            options.WithCredentials(
                configuration["Luno:ApiKeyId"] ?? string.Empty,
                configuration["Luno:ApiKeySecret"] ?? string.Empty);
        });

        services.AddScoped<LunoSdkBridge>();
        services.AddScoped<ILunoTrader>(sp => sp.GetRequiredService<LunoSdkBridge>());
        services.AddScoped<ILunoMarketData>(sp => sp.GetRequiredService<LunoSdkBridge>());
        
        services.AddScoped<IWalletRepository, PostgresWalletBridge>();

        return services;
    }
}

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Luno.MissionControl.Web.Client.Infrastructure;

public static class SignalRRegistration
{
    public static IServiceCollection AddPriceHubClient(this IServiceCollection services)
    {
        services.AddKeyedScoped<HubConnection>("PriceHub", (sp, key) =>
        {
            var navManager = sp.GetRequiredService<NavigationManager>();
            var loggerProvider = sp.GetRequiredService<ILoggerProvider>();

            return new HubConnectionBuilder()
                .WithUrl(navManager.ToAbsoluteUri("/hubs/price"))
                .WithAutomaticReconnect()
                .ConfigureLogging(logging =>
                {
                    logging.AddProvider(loggerProvider);
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .Build();
        });

        return services;
    }
}

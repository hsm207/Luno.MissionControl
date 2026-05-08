using Microsoft.Extensions.DependencyInjection;
using Luno.MissionControl.Application.Commands;
using Luno.MissionControl.Application.Ports;
using Luno.MissionControl.Application.UseCases;

namespace Luno.MissionControl.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, bool isDevelopment)
    {
        // We branch on isDevelopment to provide a high-fidelity 'Simulated' environment for testing
        // and local development without requiring live Luno API keys or incurring trade costs.
        if (isDevelopment)
        {
            services.AddScoped<IBasketService, SimulatedBasketOrchestrator>();
        }
        else
        {
            services.AddScoped<IBasketService, BasketOrchestrator>();
        }

        return services;
    }
}

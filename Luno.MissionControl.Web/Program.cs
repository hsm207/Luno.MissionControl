using Luno.MissionControl.Web.Components;
using Luno.MissionControl.Web.Hubs;
using Luno.MissionControl.Web.Services;
using Luno.MissionControl.Application;
using Luno.MissionControl.Application.Ports;
using Luno.MissionControl.Application.Diagnostics;
using Luno.MissionControl.Application.UseCases;
using Luno.MissionControl.Application.Commands;
using Luno.MissionControl.Application.Models;
using Luno.MissionControl.Web.Client.Adapters;
using Luno.MissionControl.Web.Client.Services;
using Luno.MissionControl.Web.Controllers;
using Luno.MissionControl.Infrastructure.Adapters;
using Luno.MissionControl.Infrastructure.Persistence;
using Luno.MissionControl.Infrastructure;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// --- CORE ARCHITECTURE ---
builder.AddNpgsqlDbContext<SettingsDbContext>("settingsdb");
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices(builder.Environment.IsDevelopment());

// --- UI & FRAMEWORK ---
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => options.DetailedErrors = builder.Environment.IsDevelopment())
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddScoped<CircuitHandler, CircuitLifecycleLogger>();
builder.Services.AddSignalR();
builder.Services.AddFluentUIComponents(config =>
{
    config.MarkupSanitized.SanitizeInlineStyle = (value) => value;
});

// --- MISSION CONTROL SERVICES ---
builder.Services.AddSingleton<PriceBroadcaster>();
builder.Services.AddSingleton<IPriceBroadcaster>(sp => sp.GetRequiredService<PriceBroadcaster>());
builder.Services.AddSingleton<MarketInventory>();
builder.Services.AddScoped<ServerBasketState>();
builder.Services.AddScoped<IBasketState>(sp => sp.GetRequiredService<ServerBasketState>());
builder.Services.AddScoped<IPriceClient>(sp => sp.GetRequiredService<ServerBasketState>());
builder.Services.AddHostedService<MarketWatchService>();
builder.Services.AddScoped<Luno.MissionControl.Web.Client.Components.Layout.MainLayoutViewModel>();
builder.Services.AddScoped<IPersistenceBridge, PersistenceBridge>();

var app = builder.Build();

// Ensure the database is created at startup with high-fidelity resilience.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SettingsDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    // We use a simple retry pattern to allow the database container to finish initializing.
    var retries = 5;
    while (retries > 0)
    {
        try
        {
            await context.Database.EnsureCreatedAsync();
            break;
        }
        catch (Exception ex) when (retries > 1)
        {
            logger.LogWarning("Waiting for database to be ready... ({Retries} attempts left). Error: {Message}", retries, ex.Message);
            retries--;
            await Task.Delay(2000);
        }
    }
}

app.MapDefaultEndpoints();
app.MapOtlpForwarder();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Luno.MissionControl.Web.Client._Imports).Assembly);

app.MapHub<PriceHub>("/hubs/price");
app.MapBasketActions();
app.MapWalletActions();

app.Run();

public partial class Program { }

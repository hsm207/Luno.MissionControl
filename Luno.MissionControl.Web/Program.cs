using Luno.MissionControl.Web.Components;
using Luno.MissionControl.Web.Hubs;
using Luno.MissionControl.Web.Services;
using Luno.MissionControl.Application;
using Luno.MissionControl.Application.Models;
using Luno.MissionControl.Web.Client.Services;
using Luno.SDK;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => options.DetailedErrors = builder.Environment.IsDevelopment())
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddScoped<CircuitHandler, CircuitLifecycleLogger>();

builder.Services.AddSignalR();
builder.Services.AddFluentUIComponents(config =>
{
    config.MarkupSanitized.SanitizeInlineStyle = (value) => value;
});

builder.Services.AddLunoClient(options =>
{
    options.WithCredentials(
        builder.Configuration["Luno:ApiKeyId"] ?? string.Empty,
        builder.Configuration["Luno:ApiKeySecret"] ?? string.Empty);
});
builder.Services.AddSingleton<MarketInventory>();
builder.Services.AddSingleton<IPriceBroadcaster, PriceBroadcaster>();

builder.Services.AddScoped<ServerBasketState>();
builder.Services.AddScoped<IBasketState>(sp => sp.GetRequiredService<ServerBasketState>());
builder.Services.AddScoped<IPriceClient>(sp => sp.GetRequiredService<ServerBasketState>());

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IBasketService, SimulatedBasketOrchestrator>();
}
else
{
    builder.Services.AddScoped<IBasketService, BasketOrchestrator>();
}

builder.Services.AddHostedService<MarketWatchService>();

builder.Services.AddScoped<Luno.MissionControl.Web.Client.Components.Layout.MainLayoutViewModel>();

var app = builder.Build();

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

app.MapPost("/api/basket/execute", async (BasketExecutionRequest request, IBasketService service, CancellationToken ct) =>
{
    try
    {
        var result = await service.ExecuteAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Exception)
    {
        // Absolute panic fallback for system-level errors (DI, network stack, etc.)
        return Results.Problem(
            detail: "A critical system error occurred at the gateway.",
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Internal Server Error");
    }
});

app.Run();
public partial class Program { }

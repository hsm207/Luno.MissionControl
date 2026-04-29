using Luno.MissionControl.Web.Components;
using Luno.MissionControl.Web.Hubs;
using Luno.MissionControl.Web.Services;
using Luno.MissionControl.Application;
using Luno.MissionControl.Application.Models;
using Luno.MissionControl.Web.Client.Services;
using Luno.SDK;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => options.DetailedErrors = builder.Environment.IsDevelopment())
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddSignalR();
builder.Services.AddFluentUIComponents(config =>
{
    config.MarkupSanitized.SanitizeInlineStyle = (value) => value;
});

// 1. SDK & Core Infrastructure
builder.Services.AddLunoClient();
builder.Services.AddSingleton<MarketInventory>();
builder.Services.AddSingleton<IPriceBroadcaster, PriceBroadcaster>();

// 2. Application Policies
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

// 3. Background Services
builder.Services.AddHostedService<MarketWatchService>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Luno.MissionControl.Web.Client._Imports).Assembly);

// 4. BFF Hubs & Endpoints
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

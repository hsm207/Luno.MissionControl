using Luno.MissionControl.Application.Ports;
using Microsoft.AspNetCore.Mvc;

namespace Luno.MissionControl.Web.Controllers;

/// <summary>
/// A server-side Driving Adapter that exposes Wallet Orchestration logic as an API.
/// This allows the WASM client to perform operations that require server-side context (e.g. Database, SDK).
/// </summary>
public static class WalletController
{
    public static void MapWalletActions(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/wallets");

        group.MapGet("/overview", async (IWalletOrchestrator orchestrator, CancellationToken ct) =>
        {
            var overview = await orchestrator.GetWalletOverviewAsync(ct);
            return Results.Ok(overview);
        });

        group.MapPost("/pin", async ([FromQuery] string asset, [FromQuery] long accountId, IWalletOrchestrator orchestrator, CancellationToken ct) =>
        {
            await orchestrator.PinAccountAsync(asset, accountId, ct);
            return Results.NoContent();
        });
    }
}

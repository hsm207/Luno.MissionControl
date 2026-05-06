using Luno.MissionControl.Application.Commands;
using Luno.MissionControl.Application.Ports;

namespace Luno.MissionControl.Web.Controllers;

/// <summary>
/// A server-side Driving Adapter (Controller) that maps incoming HTTP requests to internal application commands.
/// Following the 'Screaming Architecture' mandate by explicitly revealing its intent as an API controller.
/// </summary>
public static class BasketController
{
    public static void MapBasketActions(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/basket");

        group.MapPost("/execute", async (ExecuteAllocationCommand command, IBasketService service, CancellationToken ct) =>
        {
            try
            {
                var result = await service.ExecuteAsync(command, ct);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                // [FORENSIC SIGNAL] Critical gateway failure would be logged here
                return Results.Problem(
                    detail: "A critical system error occurred at the gateway.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error");
            }
        });
    }
}

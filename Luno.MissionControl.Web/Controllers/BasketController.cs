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

        group.MapPost("/execute", async (ExecuteAllocationCommand command, IBasketService service, ILogger<Program> logger, CancellationToken ct) =>
        {
            try
            {
                var result = await service.ExecuteAsync(command, ct);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception in /api/basket/execute. Type: {ExceptionType}, Command: {@Command}", ex.GetType().Name, command);
                return Results.Problem(
                    detail: "An internal system error occurred. Our engineers have been notified.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Execution Error");
            }
        });
    }
}

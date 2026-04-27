using System.Net.Http.Json;
using Luno.MissionControl.Application;
using Luno.MissionControl.Application.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Luno.MissionControl.Tests.Integration;

/// <summary>
/// High-Fidelity Tier 2 Verification of the BFF Handshake.
/// Ensures the Minimal API correctly deserializes requests and delegates to the Orchestrator.
/// </summary>
public class BffHandshakeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BffHandshakeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostExecute_WithValidPayload_DelegatesToOrchestrator()
    {
        // Arrange
        var mockService = new Mock<IBasketService>();
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped(_ => mockService.Object);
            });
        }).CreateClient();

        var request = new BasketExecutionRequest(
            TotalSpend: 1000m,
            Allocations: new[]
            {
                new Allocation("XBTZAR", 0.6m),
                new Allocation("ETHZAR", 0.4m)
            }
        );

        mockService.Setup(s => s.ExecuteAsync(It.IsAny<BasketExecutionRequest>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new BasketExecutionResult(true, new List<OrderSummary>()))
                   .Verifiable();

        // Act
        var response = await client.PostAsJsonAsync("/api/basket/execute", request);

        // Assert
        response.EnsureSuccessStatusCode();
        mockService.Verify(s => s.ExecuteAsync(
            It.Is<BasketExecutionRequest>(r => r.TotalSpend == 1000m && r.Allocations.Count == 2),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}

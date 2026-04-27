using System;
using System.Linq;
using System.Threading.Tasks;
using Luno.MissionControl.Application;
using Luno.MissionControl.Application.Commands;
using Luno.MissionControl.Application.Models;
using Luno.SDK;
using Luno.SDK.Infrastructure;
using Luno.SDK.Trading;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Luno.MissionControl.Tests.Integration;

public class BasketOrchestratorIntegrationTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly ILunoClient _lunoClient;
    private readonly BasketOrchestrator _orchestrator;

    public BasketOrchestratorIntegrationTests()
    {
        _server = WireMockServer.Start();
        
        // Setup the real SDK client pointed at our WireMock server.
        // We trim the trailing slash to ensure Kiota template expansion {+baseurl}/api/... results in single-slash paths.
        var options = new LunoClientOptions { BaseUrl = _server.Url!.TrimEnd('/') }
            .WithCredentials("test-key", "test-secret");
            
        _lunoClient = new LunoClient(options);
        _orchestrator = new BasketOrchestrator(_lunoClient);
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }

    [Fact(DisplayName = "Given valid weights, When executing basket, Then resolve accounts and place orders sequentially")]
    public async Task HandleAsync_SuccessFlow_ExecutesCorrectSequentially()
    {
        // Arrange
        var command = new ExecuteAllocationCommand(100m, new[]
        {
            new Allocation("XBTMYR", 0.6m),
            new Allocation("ETHMYR", 0.4m)
        });

        // 1. Mock Market Metadata (Realistic limits to satisfy SDK validation)
        _server.Given(Request.Create().WithPath("/api/exchange/1/markets").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    markets = new[]
                    {
                        new { market_id = "XBTMYR", trading_status = "ACTIVE", base_currency = "XBT", counter_currency = "MYR", min_volume = "0.0001", max_volume = "100", volume_scale = 6, min_price = "1", max_price = "1000000", price_scale = 2, fee_scale = 8 },
                        new { market_id = "ETHMYR", trading_status = "ACTIVE", base_currency = "ETH", counter_currency = "MYR", min_volume = "0.001", max_volume = "1000", volume_scale = 4, min_price = "1", max_price = "1000000", price_scale = 2, fee_scale = 8 }
                    }
                }));

        // 2. Mock Account Balances
        _server.Given(Request.Create().WithPath("/api/1/balance").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    balance = new[]
                    {
                        new { account_id = "1001", asset = "XBT", balance = "1.0", reserved = "0", unconfirmed = "0" },
                        new { account_id = "1002", asset = "ETH", balance = "10.0", reserved = "0", unconfirmed = "0" },
                        new { account_id = "1003", asset = "MYR", balance = "500.0", reserved = "0", unconfirmed = "0" }
                    }
                }));

        // 3. Mock Tickers (Internal CalculateOrderSize calls)
        _server.Given(Request.Create().WithPath("/api/1/ticker").WithParam("pair", "XBTMYR").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    pair = "XBTMYR",
                    timestamp = 1772555388322,
                    bid = "99000",
                    ask = "100000",
                    last_trade = "99500",
                    rolling_24_hour_volume = "10.5",
                    status = "ACTIVE"
                }));

        _server.Given(Request.Create().WithPath("/api/1/ticker").WithParam("pair", "ETHMYR").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    pair = "ETHMYR",
                    timestamp = 1772555388322,
                    bid = "9900",
                    ask = "10000",
                    last_trade = "9950",
                    rolling_24_hour_volume = "150.0",
                    status = "ACTIVE"
                }));

        // 4. Mock Order Placement
        _server.Given(Request.Create().WithPath("/api/1/postorder").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    order_id = "ORD-" + Guid.NewGuid().ToString("N")[..8]
                }));

        // Act
        var result = await _orchestrator.HandleAsync(command);

        // Assert
        Assert.True(result.Success, $"Execution failed: {result.ErrorMessage}");
        Assert.Equal(2, result.Orders.Count);
        
        // Verify we actually hit the postorder endpoint twice
        var postRequests = _server.FindLogEntries(Request.Create().WithPath("/api/1/postorder"));
        Assert.Equal(2, postRequests.Count);
    }

    [Fact(DisplayName = "Given invalid weights, When executing basket, Then fail fast")]
    public async Task HandleAsync_InvalidWeights_ThrowsException()
    {
        // Arrange
        var command = new ExecuteAllocationCommand(100m, new[]
        {
            new Allocation("XBTMYR", 0.5m),
            new Allocation("ETHMYR", 0.4m) // Sum is 0.9
        });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _orchestrator.HandleAsync(command));
        Assert.Contains("Weights must sum exactly to 1.00", ex.Message);
    }

    [Fact(DisplayName = "Given missing account, When executing basket, Then fail fast with error")]
    public async Task HandleAsync_MissingAccount_FailsFast()
    {
        // Arrange
        var command = new ExecuteAllocationCommand(100m, new[] { new Allocation("XBTMYR", 1.0m) });

        // 1. Mock Markets
        _server.Given(Request.Create().WithPath("/api/exchange/1/markets").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { markets = new[] { new { market_id = "XBTMYR", trading_status = "ACTIVE", base_currency = "XBT", counter_currency = "MYR", min_volume = "0.1", max_volume = "100", volume_scale = 6, min_price = "1", max_price = "1000000", price_scale = 2, fee_scale = 8 } } }));

        // 2. Mock Balances
        _server.Given(Request.Create().WithPath("/api/1/balance").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    balance = new[] { new { account_id = "1003", asset = "MYR", balance = "500", reserved = "0", unconfirmed = "0" } }
                })); // XBT missing!

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _orchestrator.HandleAsync(command));
        Assert.Contains("No XBT account found", ex.Message);
    }

    [Fact(DisplayName = "Given empty allocations, When executing basket, Then fail fast")]
    public async Task HandleAsync_EmptyAllocations_Fail()
    {
        // Arrange
        var command = new ExecuteAllocationCommand(100m, Array.Empty<Allocation>());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _orchestrator.HandleAsync(command));
        Assert.Contains("Basket must contain at least one allocation", ex.Message);
    }
}

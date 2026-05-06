using Luno.MissionControl.Infrastructure.Adapters;
using Luno.MissionControl.Core.Models;
using Luno.SDK;
using Luno.SDK.Trading;
using Luno.SDK.Application.Trading;
using NSubstitute;
using Xunit;

namespace Luno.MissionControl.Infrastructure.Tests;

public class LunoSDKAdapterTests
{
    [Fact(DisplayName = "The Luno SDK Adapter must correctly translate domain requests into SDK calls without placing real orders.")]
    public async Task PostOrderAsync_ShouldMapCorrectly_ToSDKCommand()
    {
        var mockClient = Substitute.For<ILunoClient>();
        var adapter = new LunoSdkBridge(mockClient);

        var estimation = new OrderEstimation("XBTUSDC", 0.5m, 60000m, 30000m);
        var baseAccountId = 12345L;
        var counterAccountId = 67890L;

        mockClient.Trading.Requests.SendAsync<OrderQuote>(Arg.Any<CalculateOrderSizeQuery>())
            .Returns(new OrderQuote("XBTUSDC", OrderSide.Buy, 0.5m, 60000m, "USDC"));

        var fakeOrderId = "BX-MOCK-999";
        mockClient.Trading.Requests.SendAsync<OrderResponse>(Arg.Any<PostLimitOrderCommand>())
            .Returns(new OrderResponse { OrderId = fakeOrderId });

        var resultId = await adapter.PostOrderAsync(estimation, baseAccountId, counterAccountId);

        await mockClient.Trading.Requests.Received(1).SendAsync<OrderResponse>(Arg.Is<PostLimitOrderCommand>(c => 
            c.Pair == "XBTUSDC" &&
            c.BaseAccountId == baseAccountId &&
            c.CounterAccountId == counterAccountId &&
            c.Options.AuthorizeWriteOperation == true 
        ));

        Assert.Equal(fakeOrderId, resultId);
    }
}

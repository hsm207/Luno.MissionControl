using Luno.MissionControl.Infrastructure.Adapters;
using Luno.SDK;
using Luno.SDK.Application.Account;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Luno.MissionControl.Infrastructure.Tests;

public class LunoSdkAccountAdapterTests
{
    [Fact(DisplayName = "Given AccountBalanceResponse with AccountType, When GetAccountsAsync runs, Then LunoAccount preserves AccountType")]
    public async Task GetAccountsAsync_ShouldPreserveAccountType_FromSdkResponse()
    {
        var mockClient = Substitute.For<ILunoClient>();
        var adapter = new LunoSdkAccountAdapter(mockClient, Substitute.For<ILogger<LunoSdkAccountAdapter>>());
        mockClient.Accounts.Requests.SendAsync(Arg.Any<GetBalancesQuery>(), Arg.Any<CancellationToken>())
            .Returns([new AccountBalanceResponse("12345", "XBT", 1.5m, 0m, 0m, 1.5m, "Spot XBT", "TRANSACTIONAL")]);

        var accounts = await adapter.GetAccountsAsync();

        Assert.Equal("TRANSACTIONAL", accounts["XBT"].Single().AccountType);
    }
}

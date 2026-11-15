using Luno.MissionControl.Infrastructure.Adapters;
using Luno.SDK;
using Luno.SDK.Application.Account;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Luno.MissionControl.Infrastructure.Tests;

/// <summary>Unit tests for <see cref="LunoSdkAccountAdapter"/> ACL mapping.</summary>
public class LunoSdkAccountAdapterTests
{
    private const decimal DefaultBalance = 1.5m;
    private const decimal ZeroAmount = 0m;

    /// <summary>Given SDK balance with AccountType, Maps AccountType onto LunoAccount.</summary>
    [Fact(DisplayName = "Given AccountBalanceResponse with AccountType, "
        + "When GetAccountsAsync runs, Then LunoAccount preserves AccountType")]
    public async Task GetAccountsAsync_ShouldPreserveAccountType_FromSdkResponse()
    {
        var mockClient = Substitute.For<ILunoClient>();
        var adapter = new LunoSdkAccountAdapter(
            mockClient,
            Substitute.For<ILogger<LunoSdkAccountAdapter>>());
        mockClient.Accounts.Requests.SendAsync(Arg.Any<GetBalancesQuery>(), Arg.Any<CancellationToken>())
            .Returns([new AccountBalanceResponse(
                "12345", "XBT", DefaultBalance, ZeroAmount, ZeroAmount, DefaultBalance, "Spot XBT", "TRANSACTIONAL")]);

        var accounts = await adapter.GetAccountsAsync();

        Assert.Equal("TRANSACTIONAL", accounts["XBT"].Single().AccountType);
    }
}

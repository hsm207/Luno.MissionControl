namespace Luno.MissionControl.Web.Client.Components.Wallets;

using Luno.MissionControl.Core.Models;

public record WalletOverviewViewModel(
    string Asset,
    List<LunoAccount> Accounts,
    string? ResolvedAccountId,
    bool IsAmbiguous
);

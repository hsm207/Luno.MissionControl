using Luno.MissionControl.Application.Diagnostics;
using Luno.MissionControl.Application.Ports;
using Luno.MissionControl.Core.Models;
using Luno.MissionControl.Web.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Luno.MissionControl.Web.Client.Components.Wallets;

public partial class WalletsHub
{
    [Inject]
    private IWalletOrchestrator WalletOrchestrator { get; set; } = default!;

    [Inject]
    private IPersistenceBridge PersistenceBridge { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    private IReadOnlyList<SpotWalletViewModel>? _spotWallets;
    private IReadOnlyList<WalletCardModel> _transactionalCards = [];
    private IReadOnlyList<WalletCardModel> _otherViewOnlyCards = [];
    private bool _isLoading = true;
    private string? _errorMessage;

    private bool HasAnyWallets =>
        _spotWallets is { Count: > 0 } ||
        _transactionalCards.Count > 0 ||
        _otherViewOnlyCards.Count > 0;

    protected override async Task OnInitializedAsync()
    {
        await LoadOverviewAsync();
    }

    private async Task LoadOverviewAsync()
    {
        using var activity = ForensicTracing.StartActivity("UI.LoadWalletsHub");
        try
        {
            _isLoading = true;
            var items = await PersistenceBridge.GetOrLoadAsync("wallets-overview",
                () => WalletOrchestrator.GetWalletOverviewAsync());
            ApplyOverview(items);
            activity?.SetTag("ui.wallet_count", items.Count);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load wallets: {ex.Message}";
            activity?.SetTag("otel.status_code", "ERROR");
            activity?.SetTag("otel.status_description", ex.Message);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void ApplyOverview(IReadOnlyList<Wallet> items)
    {
        _spotWallets = items
            .Where(i => i.SpotAccounts.Count > 0)
            .OrderByDescending(i => i.ResolvedAccountId is null)
            .ThenBy(i => i.Asset)
            .Select(i => new SpotWalletViewModel
            {
                Asset = i.Asset,
                ResolvedAccountId = i.ResolvedAccountId?.ToString(),
                SpotAccounts = i.SpotAccounts
            })
            .ToList();

        var viewOnlyWallets = items
            .Where(i => i.ViewOnlyAccounts.Count > 0)
            .OrderBy(i => i.Asset)
            .Select(i => new ViewOnlyWalletViewModel
            {
                Asset = i.Asset,
                ViewOnlyAccounts = i.ViewOnlyAccounts
            })
            .ToList();

        _transactionalCards = WalletDisplayFormatter.BuildViewOnlyCards(
            viewOnlyWallets, a => a.AccountType == "TRANSACTIONAL", "Transactional");
        _otherViewOnlyCards = WalletDisplayFormatter.BuildViewOnlyCards(
            viewOnlyWallets, a => a.AccountType != "TRANSACTIONAL");
    }

    private async Task PinAccountAsync(string asset, long accountId)
    {
        using var activity = ForensicTracing.StartActivity("UI.PinAccount");
        activity?.SetTag("ui.asset", asset);
        activity?.SetTag("ui.accountId", accountId);

        try
        {
            await WalletOrchestrator.PinAccountAsync(asset, accountId);
            await LoadOverviewAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to pin account: {ex.Message}";
            activity?.SetTag("otel.status_code", "ERROR");
            activity?.SetTag("otel.status_description", ex.Message);
        }
    }

    private async Task CopyToClipboardAsync(string text)
    {
        await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
    }
}

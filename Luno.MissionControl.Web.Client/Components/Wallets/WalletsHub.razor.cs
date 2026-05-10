using Luno.MissionControl.Core.Models;
using Luno.MissionControl.Application.Ports;
using Luno.MissionControl.Application.Diagnostics;
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

    private List<WalletOverviewViewModel>? _overview;
    private bool _isLoading = true;
    private string? _errorMessage;

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
            _overview = items
                .OrderByDescending(i => i.IsAmbiguous)
                .ThenBy(i => i.Asset)
                .Select(i => new WalletOverviewViewModel(
                    i.Asset,
                    i.Accounts,
                    i.ResolvedAccountId?.ToString(),
                    i.IsAmbiguous
                )).ToList();

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

namespace Luno.MissionControl.Web.Client.Components.Dashboard;

using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Luno.MissionControl.Application.Models;

public partial class ReviewGate : ComponentBase
{
    private bool _isConfirmed = false;

    [Parameter]
    public BasketExecutionRequest Content { get; set; } = default!;

    [CascadingParameter]
    public IDialogInstance Dialog { get; set; } = default!;

    private string FormatPair(string pair)
    {
        if (string.IsNullOrEmpty(pair)) return pair;
        // Basic heuristic for Luno pairs: XBT/ETH/SOL/etc + MYR/USDC
        if (pair.EndsWith("MYR")) return $"{pair[..^3]} / MYR";
        if (pair.EndsWith("USDC")) return $"{pair[..^4]} / USDC";
        return pair;
    }

    private async Task ConfirmAsync()
    {
        await Dialog.CloseAsync(true);
    }

    private async Task CancelAsync()
    {
        await Dialog.CancelAsync();
    }
}

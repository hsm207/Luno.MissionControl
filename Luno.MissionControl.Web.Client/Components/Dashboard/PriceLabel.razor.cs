namespace Luno.MissionControl.Web.Client.Components.Dashboard;

using Microsoft.AspNetCore.Components;
using Luno.MissionControl.Application;
using Luno.MissionControl.Application.Models;

public partial class PriceLabel : ComponentBase, IDisposable
{
    [Parameter] public required string Pair { get; set; }
    [Inject] private IBasketState State { get; set; } = default!;

    private decimal? _price;

    protected override void OnInitialized()
    {
        // Initial state from cache
        if (State.Prices.TryGetValue(Pair, out var snapshot))
        {
            _price = snapshot.Price;
        }

        // Subscribe to future updates
        State.OnPriceUpdate += HandlePriceUpdate;
    }

    private void HandlePriceUpdate(TickerSnapshot snapshot)
    {
        if (snapshot.Pair == Pair)
        {
            _price = snapshot.Price;
            InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        State.OnPriceUpdate -= HandlePriceUpdate;
    }
}

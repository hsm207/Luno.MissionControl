namespace Luno.MissionControl.Web.Client.Components.Dashboard;

using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Luno.MissionControl.Application;
using Luno.MissionControl.Application.Ports;
using Luno.MissionControl.Application.Commands;
using Luno.MissionControl.Application.Models;
using Luno.MissionControl.Application.Diagnostics;
using Luno.MissionControl.Web.Client.Adapters;
using Luno.MissionControl.Web.Client.Services;
using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using AllocationRequest = Luno.MissionControl.Application.Commands.AllocationRequest;

/// <summary>
/// Orchestrates the selection, weighting, and execution of multi-asset investment baskets.
/// Manages live market data synchronization and currency-aware allocation transitions.
/// </summary>
public partial class ComboOrchestrator : ComponentBase, IDisposable
{
    private string _amountInputString = "50";

    [Inject] private IBasketState State { get; set; } = default!;
    [Inject] private IBasketService BasketService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ILogger<ComboOrchestrator> Logger { get; set; } = default!;
    [Inject] private IPersistenceBridge PersistenceBridge { get; set; } = default!;

    private List<AllocationRequest> _allocations = new();
    private IEnumerable<MarketMetadataDto> _selectedSearchItems { get; set; } = [];
    private ErrorBoundary? _errorBoundary;
    private decimal _rawSpendInput = 0m;
    private DateTimeOffset? _lastPriceUpdate;

    private string DataFreshnessText
    {
        get
        {
            if (_lastPriceUpdate == null) return "Connecting to live market stream...";
            var seconds = (int)(DateTimeOffset.UtcNow - _lastPriceUpdate.Value).TotalSeconds;
            return $"Prices refreshed {seconds} seconds ago";
        }
    }


    private Task OnSearchAsync(OptionsSearchEventArgs<MarketMetadataDto> e)
    {
        e.Items = State.AvailableMarkets
            .Where(m => m.CounterCurrency == GetLunoCounter(State.SelectedCurrency))
            .Where(m => string.IsNullOrEmpty(e.Text) ||
                        m.Pair.Contains(e.Text, StringComparison.OrdinalIgnoreCase) ||
                        m.BaseCurrency.Contains(e.Text, StringComparison.OrdinalIgnoreCase) ||
                        (e.Text.Equals("X", StringComparison.OrdinalIgnoreCase) && (m.BaseCurrency == "XBT" || m.BaseCurrency == "XRP")))
            .Where(m => !_allocations.Any(a => a.Pair == m.Pair))
            .OrderBy(m => m.BaseCurrency);

        return Task.CompletedTask;
    }

    private void AddAsset()
    {
        using var activity = ForensicTracing.StartActivity("Add Asset Button Clicked");

        var selected = _selectedSearchItems.FirstOrDefault();
        if (selected == null)
        {
            _ = ToastService.ShowToastAsync(options => { options.Intent = ToastIntent.Warning; options.Title = "Please search and select an asset first!"; });
            return;
        }

        activity?.SetTag("pair.id", selected.Pair);

        if (_allocations.Any(a => a.Pair == selected.Pair))
        {
            _ = ToastService.ShowToastAsync(options => { options.Intent = ToastIntent.Warning; options.Title = $"{selected.Pair} is already in the basket!"; });
            return;
        }

        _allocations = [.. _allocations, new AllocationRequest(selected.Pair, 0m)];
        _selectedSearchItems = [];

        _ = ToastService.ShowToastAsync(options => { options.Intent = ToastIntent.Success; options.Title = $"Added {selected.Pair} to your basket."; });
        StateHasChanged();
    }

    private string GetLunoCounter(string humanCurrency) => humanCurrency == "USD" ? "USDC" : humanCurrency;

    private string _currencyState
    {
        get => State.SelectedCurrency;
        set
        {
            if (State.SelectedCurrency != value)
            {
                Logger.LogDebug("Currency selection changing: {Old} -> {New}", State.SelectedCurrency, value);
                TransitionCurrency(State.SelectedCurrency, value);
                State.SelectedCurrency = value;
            }
        }
    }

    private decimal _totalWeight => _allocations.Sum(a => a.Weight);
    private decimal _calculatedTotal => _allocations.Sum(a => GetAllocatedAmount(a));
    private bool _isExecuting = false;

    private decimal GetAllocatedAmount(AllocationRequest alloc)
    {
        return Math.Round(State.TargetSpend * alloc.Weight, 2);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                Logger.LogDebug("Starting background state orchestration.");
                await State.StartAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "State initialization failed.");
                await ToastService.ShowToastAsync(options =>
                {
                    options.Intent = ToastIntent.Warning;
                    options.Title = "Initialization Warning";
                    options.Body = "Live data connectivity issue. Some features may be degraded.";
                });
            }
        }
    }

    protected override async Task OnInitializedAsync()
    {
        State.SelectedCurrency = await PersistenceBridge.GetOrLoadAsync("SelectedCurrency",
            () => Task.FromResult(State.SelectedCurrency));

        State.TargetSpend = await PersistenceBridge.GetOrLoadAsync("TargetSpend",
            () => Task.FromResult(State.TargetSpend));

        // Default starting state aligned with SelectedCurrency
        bool isMyr = State.SelectedCurrency == "MYR";

        _allocations =
        [
            new AllocationRequest(isMyr ? "XBTMYR" : "XBTUSDC", 0.6m),
            new AllocationRequest(isMyr ? "ETHMYR" : "ETHUSDC", 0.4m)
        ];

        _rawSpendInput = State.TargetSpend;

        State.OnPriceUpdate += HandlePriceUpdate;
        State.OnMarketsUpdate += HandleMarketsUpdate;
    }

    private void HandlePriceUpdate(TickerSnapshotDto snapshot)
    {
        if (_allocations.Any(a => a.Pair == snapshot.Pair))
        {
            Logger.LogTrace("Price updated for basket member: {Pair} = {Price}", snapshot.Pair, snapshot.Price);
        }
        _lastPriceUpdate = snapshot.Timestamp;
        InvokeAsync(StateHasChanged);
    }


    private void HandleMarketsUpdate(IReadOnlyList<MarketMetadataDto> markets)
    {
        InvokeAsync(StateHasChanged);
    }

    private void TransitionCurrency(string oldCurrency, string newCurrency)
    {
        using var activity = ForensicTracing.StartActivity("Currency Transition");

        List<AllocationRequest> newAllocationRequests = [];
        foreach (var alloc in _allocations)
        {
            var oldMarket = State.AvailableMarkets.FirstOrDefault(m => m.Pair == alloc.Pair);
            if (oldMarket != null)
            {
                var newMarket = State.AvailableMarkets.FirstOrDefault(m => m.BaseCurrency == oldMarket.BaseCurrency && m.CounterCurrency == GetLunoCounter(newCurrency));
                if (newMarket != null)
                {
                    newAllocationRequests.Add(alloc with { Pair = newMarket.Pair });
                }
            }
        }

        _allocations = newAllocationRequests;
    }

    private void OnAmountInputChanged()
    {
        if (decimal.TryParse(_amountInputString, NumberStyles.Any, CultureInfo.InvariantCulture, out var spend))
        {
            State.TargetSpend = spend;
            StateHasChanged();
        }
    }

    private void OnWeightChanged(AllocationRequest allocation, decimal newWeight)
    {
        UpdateWeight(allocation.Pair, newWeight);
    }

    private string GetCurrencySymbol() => State.SelectedCurrency switch
    {
        "MYR" => "RM",
        "USDC" => "$",
        _ => "$"
    };

    private string FormatPair(string pair)
    {
        var market = State.AvailableMarkets.FirstOrDefault(m => m.Pair == pair);
        if (market == null) return pair;
        return $"{market.BaseCurrency} / {market.CounterCurrency}";
    }

    public void Dispose()
    {
        State.OnPriceUpdate -= HandlePriceUpdate;
        State.OnMarketsUpdate -= HandleMarketsUpdate;
    }

    private void RemoveAsset(string pair)
    {
        _allocations = _allocations.Where(a => a.Pair != pair).ToList();
    }

    private void UpdateWeight(string pair, decimal newWeight)
    {
        _allocations = _allocations
            .Select(a => a.Pair == pair ? a with { Weight = newWeight } : a)
            .ToList();
    }

    private async Task ExecuteBasket()
    {
        using var forensic = ForensicTracing.StartActivity("BasketExecution");

        var command = new ExecuteAllocationCommand(
            State.TargetSpend,
            _allocations);

        var dialogResult = await DialogService.ShowDialogAsync<ReviewGate>(options =>
        {
            options.Header.Title = "CONFIRM YOUR COMBO";
            options.Modal = true;
            options.Parameters.Add(nameof(ReviewGate.Content), command);
            options.Width = "450px";
        });

        if (dialogResult.Cancelled)
        {
            Logger.LogDebug("Basket execution cancelled by user.");
            return;
        }

        Logger.LogInformation("Orders confirmed. Dispatching {Count} allocation(s) to the bridge.", _allocations.Count);
        _isExecuting = true;


        var progressToast = await ToastService.ShowToastInstanceAsync(options =>
        {
            options.Id = "basket-execution";
            options.Title = "Orchestrating Basket...";
            options.Intent = ToastIntent.Info;
            options.Timeout = null; // Stays until dismissed
        });


        var basketResult = await BasketService.ExecuteAsync(command);


        await progressToast.CloseAsync(ToastCloseReason.Dismissed);
        _isExecuting = false;

        if (basketResult.Success)
        {
            await ToastService.ShowToastAsync(options =>
            {
                options.Intent = ToastIntent.Success;
                options.Title = "Mission Accomplished";
                options.Body = $"Successfully deployed {basketResult.Orders.Count} limit orders to the basket.";
                options.Timeout = 2000;
            });
            _allocations.Clear();
        }
        else
        {
            await ToastService.ShowToastAsync(options =>
            {
                options.Intent = ToastIntent.Error;
                options.Title = "Execution Halted";
                options.Body = basketResult.ErrorMessage ?? "The mission bridge encountered an unknown failure.";
            });
        }
    }
}

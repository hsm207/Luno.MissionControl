namespace Luno.MissionControl.Web.Client.Components.Dashboard;

using Microsoft.AspNetCore.Components;

public partial class WeightInput : ComponentBase
{
    [Parameter]
    public decimal Weight { get; set; }

    [Parameter]
    public bool HideLabel { get; set; } = false;

    [Parameter]
    public EventCallback<decimal> WeightChanged { get; set; }

    private decimal? WeightValue
    {
        get => Weight * 100;
        set
        {
            var newVal = (value ?? 0) / 100m;
            if (newVal != Weight)
            {
                Weight = newVal;
                WeightChanged.InvokeAsync(newVal);
            }
        }
    }
}

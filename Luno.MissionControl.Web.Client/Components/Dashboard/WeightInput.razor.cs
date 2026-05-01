namespace Luno.MissionControl.Web.Client.Components.Dashboard;

using Microsoft.AspNetCore.Components;

public partial class WeightInput : ComponentBase
{
    [Parameter]
    public string? Id { get; set; }

    [Parameter]
    public decimal Weight { get; set; }

    [Parameter]
    public bool HideLabel { get; set; } = false;

    [Parameter]
    public EventCallback<decimal> WeightChanged { get; set; }

    protected string? _rawValue;

    protected override void OnParametersSet()
    {
        _rawValue = (Weight * 100).ToString("F2");
    }


    protected async Task OnInputChanged(string? value)
    {
        _rawValue = value;
        if (decimal.TryParse(value, out var result))
        {
            await WeightChanged.InvokeAsync(result / 100m);
        }
    }
}

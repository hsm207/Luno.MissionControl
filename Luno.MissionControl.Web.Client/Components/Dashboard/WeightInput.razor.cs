namespace Luno.MissionControl.Web.Client.Components.Dashboard;

using System.Globalization;
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

    private string _weightInputString = string.Empty;
    private string? _errorMessage;

    protected override void OnParametersSet()
    {
        // Only overwrite the local string if it has drifted from the actual Weight value.
        // This prevents re-renders from stripping the decimal separator ('.') while the user is typing.
        if (decimal.TryParse(_weightInputString, NumberStyles.Any, CultureInfo.InvariantCulture, out var current) && current / 100m == Weight)
        {
            return;
        }

        _weightInputString = (Weight * 100m).ToString("0.##", CultureInfo.InvariantCulture);
    }

    private void OnWeightInputChanged()
    {
        _errorMessage = null;

        // Skip validation if empty
        if (string.IsNullOrWhiteSpace(_weightInputString)) return;

        if (decimal.TryParse(_weightInputString, NumberStyles.Any, CultureInfo.InvariantCulture, out var percent))
        {
            if ((percent * 100m) % 1m != 0m)
            {
                _errorMessage = "Only 2 decimal places allowed!";
                return;
            }

            WeightChanged.InvokeAsync(percent / 100m);
        }
        else
        {
            _errorMessage = "Invalid number format!";
        }
    }
}

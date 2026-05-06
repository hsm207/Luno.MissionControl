using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Hosting;

namespace Luno.MissionControl.Web.Client.Components.Layout;

public partial class MainLayout
{
    [Inject] private IHostEnvironment HostEnvironment { get; set; } = default!;

    [Inject] private MainLayoutViewModel ViewModel { get; set; } = default!;

    protected override void OnInitialized()
    {
        if (HostEnvironment.IsProduction())
        {
            ViewModel.StatusText = "PRODUCTION";
            ViewModel.StatusClass = "danger-glow";
        }
        else
        {
            ViewModel.StatusText = "DEVELOPMENT";
            ViewModel.StatusClass = "gold-glow";
        }
    }
}

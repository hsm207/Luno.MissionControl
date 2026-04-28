using Microsoft.AspNetCore.Components;

namespace Luno.MissionControl.Web.Client.Components.Layout;

public partial class MainLayout : IDisposable
{
    [Inject] private PersistentComponentState ApplicationState { get; set; } = default!;
    private PersistingComponentStateSubscription _subscription;

    private MainLayoutViewModel ViewModel { get; } = new();

    protected override async Task OnInitializedAsync()
    {
        _subscription = ApplicationState.RegisterOnPersisting(PersistData);

        if (!ApplicationState.TryTakeFromJson<string>("AppTitle", out var title))
        {
            // Initializing on Server or first time
            ViewModel.AppTitle = "MISSION CONTROL";
            ViewModel.StatusText = "NOMINAL";
        }
        else
        {
            // Restoring on Client
            ViewModel.AppTitle = title!;
            ApplicationState.TryTakeFromJson<string>("StatusText", out var status);
            ViewModel.StatusText = status ?? "NOMINAL";
        }
    }

    private Task PersistData()
    {
        ApplicationState.PersistAsJson("AppTitle", ViewModel.AppTitle);
        ApplicationState.PersistAsJson("StatusText", ViewModel.StatusText);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _subscription.Dispose();
    }
}

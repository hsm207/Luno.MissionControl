using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Luno.MissionControl.Web.Client.Services;

/// <summary>
/// Orchestrates the synchronization of state between Server and Client using PersistentComponentState.
/// </summary>
public sealed class PersistenceBridge : IPersistenceBridge, IDisposable
{
    private readonly PersistentComponentState _state;
    private readonly Dictionary<string, object?> _persistingData = [];
    private PersistingComponentStateSubscription? _subscription;

    public PersistenceBridge(PersistentComponentState state)
    {
        _state = state;
    }

    /// <summary>
    /// Core rehydration logic: 
    /// 1. Try to take from already persisted state (Client side).
    /// 2. If not present, load data and register it for persistence (Server side).
    /// </summary>
    public async Task<T> GetOrLoadAsync<T>(string key, Func<Task<T>> loader)
    {
        if (_state.TryTakeFromJson<T>(key, out var restored) && restored is not null)
        {
            return restored;
        }

        var data = await loader();

        // If we are on the server, we register this data to be sent to the client
        _persistingData[key] = data;

        if (_subscription is null)
        {
            _subscription = _state.RegisterOnPersisting(PersistDataAsync, RenderMode.InteractiveAuto);
        }

        return data;
    }

    private Task PersistDataAsync()
    {
        foreach (var (key, value) in _persistingData)
        {
            _state.PersistAsJson(key, value);
        }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}

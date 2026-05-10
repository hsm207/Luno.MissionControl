using System;
using System.Threading.Tasks;

namespace Luno.MissionControl.Web.Client.Services;

/// <summary>
/// A bridge for synchronizing state between Server-side rendering and Client-side (WASM) execution.
/// </summary>
public interface IPersistenceBridge
{
    /// <summary>
    /// Attempts to retrieve a value from the persistence store. 
    /// If not found (e.g. initial server render or state already consumed), calls the loader.
    /// </summary>
    Task<T> GetOrLoadAsync<T>(string key, Func<Task<T>> loader);
}

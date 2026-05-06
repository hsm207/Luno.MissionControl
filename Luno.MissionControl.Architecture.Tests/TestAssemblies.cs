using System.Reflection;
using Luno.MissionControl.Core.Models;
using Luno.MissionControl.Application.UseCases;
using Luno.MissionControl.Infrastructure.Adapters;

namespace Luno.MissionControl.Architecture.Tests;

public static class TestAssemblies
{
    public static readonly Assembly Core = typeof(OrderEstimation).Assembly;
    public static readonly Assembly Application = typeof(BasketOrchestrator).Assembly;
    public static readonly Assembly Infrastructure = typeof(LunoSdkBridge).Assembly;
    public static readonly Assembly Web = typeof(Program).Assembly;
    public static readonly Assembly WebClient = typeof(Luno.MissionControl.Web.Client.Components.Dashboard.ComboOrchestrator).Assembly;

    /// <summary>
    /// Dynamically retrieves all internal assembly names defined in this class.
    /// </summary>
    public static string[] GetOtherInternalNames(params Assembly[] exclude)
    {
        return typeof(TestAssemblies)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(Assembly))
            .Select(f => (Assembly)f.GetValue(null)!)
            .Where(a => !exclude.Contains(a))
            .Select(a => a.GetName().Name!)
            .ToArray();
    }
}

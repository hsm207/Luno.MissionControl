using System.Diagnostics.Metrics;

namespace Luno.MissionControl.Application.Diagnostics;

/// <summary>
/// Provides high-signal business metrics for the Mission Control ecosystem.
/// </summary>
public static class ForensicMetrics
{
    public const string MeterName = "Luno.MissionControl.Forensics";
    
    private static readonly Meter Meter = new(MeterName);

    // Wallet Resolution Metrics
    public static readonly Counter<long> WalletsResolved = Meter.CreateCounter<long>(
        "wallets.resolved", "count", "Number of successful wallet resolutions");

    public static readonly Counter<long> WalletsAmbiguous = Meter.CreateCounter<long>(
        "wallets.ambiguous", "count", "Number of detected wallet ambiguities requiring user intervention");

    public static readonly Counter<long> WalletsNotFound = Meter.CreateCounter<long>(
        "wallets.not_found", "count", "Number of missing wallets");

    // Execution Metrics
    public static readonly Counter<long> OrdersExecuted = Meter.CreateCounter<long>(
        "orders.executed", "count", "Number of successfully executed orders");

    public static readonly Counter<long> ExecutionFailures = Meter.CreateCounter<long>(
        "execution.failures", "count", "Number of failed execution attempts");
}

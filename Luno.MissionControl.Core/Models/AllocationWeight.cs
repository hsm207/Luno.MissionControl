using Luno.MissionControl.Core.Exceptions;

namespace Luno.MissionControl.Core.Models;

/// <summary>
/// Represents an investment allocation weight, strictly bounded between 0.0 and 100.0.
/// </summary>
public record AllocationWeight(decimal Value)
{
    public decimal Value { get; init; } = (Value < 0.0m || Value > 100.0m)
        ? throw new LunoDomainException("An Allocation Weight must represent a real number between 0.0 and 100.0 to prevent invalid allocation math.")
        : Value;

    public static implicit operator decimal(AllocationWeight weight) => weight.Value;
    public static explicit operator AllocationWeight(decimal value) => new(value);

    public override string ToString() => $"{Value:F2}%";
}

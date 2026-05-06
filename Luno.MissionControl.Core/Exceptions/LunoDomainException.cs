namespace Luno.MissionControl.Core.Exceptions;

/// <summary>
/// Base exception for all business invariant violations in the Mission Control domain.
/// </summary>
public class LunoDomainException : Exception
{
    public LunoDomainException(string message) : base(message) { }
    public LunoDomainException(string message, Exception innerException) : base(message, innerException) { }
}

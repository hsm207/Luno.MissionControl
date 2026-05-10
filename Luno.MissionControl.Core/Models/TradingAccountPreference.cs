using System;
using System.ComponentModel.DataAnnotations;

namespace Luno.MissionControl.Core.Models;

/// <summary>
/// Represents a user's pinned account preference for a specific currency.
/// This model has been hardened to a single AccountId per currency, 
/// eliminating role-based (Base/Counter) ambiguity.
/// </summary>
public record TradingAccountPreference
{
    [Key]
    [Required]
    [MaxLength(10)]
    public required string CurrencyCode { get; init; }

    /// <summary>
    /// The specific Luno Account ID preferred for this currency.
    /// </summary>
    public required long AccountId { get; init; }

    public required DateTime LastUpdated { get; init; }
}

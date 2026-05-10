namespace Luno.MissionControl.Core.Exceptions;

/// <summary>
/// Thrown when multiple wallets exist for a currency and no preference has been set to resolve the ambiguity.
/// </summary>
public class WalletAmbiguityException(string currency, int accountCount)
    : LunoDomainException($"Ambiguity Crisis: Asset '{currency}' has {accountCount} associated accounts, but no primary preference is pinned. Deterministic execution is impossible.");

/// <summary>
/// Thrown when no wallets can be found for a requested currency.
/// </summary>
public class WalletNotFoundException(string currency)
    : LunoDomainException($"Resolution Failure: No Luno accounts found for asset '{currency}'.");

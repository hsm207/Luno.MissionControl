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

/// <summary>
/// Thrown when an asset has no pinned SPOT trading wallet preference.
/// </summary>
public class UnpinnedWalletException(string currency)
    : LunoDomainException($"Execution Refused: Asset '{currency}' has no pinned SPOT trading wallet.");

/// <summary>
/// Thrown when a pinned account is not a SPOT trading wallet.
/// </summary>
public class NonSpotWalletException(string currency, long accountId, string accountType)
    : LunoDomainException($"Execution Refused: Pinned account '{accountId}' for asset '{currency}' has AccountType '{accountType}', expected 'SPOT'.");

/// <summary>
/// Thrown when a pinned account ID is absent from live Luno balances.
/// </summary>
public class PinnedWalletNotFoundException(string currency, long accountId)
    : LunoDomainException($"Execution Refused: Pinned account '{accountId}' for asset '{currency}' was not found in live Luno balances.");

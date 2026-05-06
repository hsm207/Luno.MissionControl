# 🐛 BUG: Account Identity Crisis & Resolution

## Status: AWAITING SEMANTIC FIX (Hybrid Discovery Planned)

## Description
The Luno API (v1) `/api/1/balance` endpoint returns no metadata (names or types) if they are not explicitly set. For users with multiple accounts for a single asset (e.g., ADA, ETH, SOL), there is no structural way to distinguish a "Trading" account from a "Savings/Staking" account via a single fetch.

## The Resolution: Semantic Discovery & Self-Healing

Instead of relying solely on balance-size heuristics, the `BasketOrchestrator` now implements a **Hybrid Discovery Pattern** to reliably target trading accounts:

1.  **Semantic Hinting (New!)**: The orchestrator inspects the `Name` property of each account.
    *   **Priority 1**: Accounts containing "Trading" in the name.
    *   **Low Priority**: Accounts containing "Savings", "Staking", or "Fixed".
2.  **Heuristic Tie-Breaking**: If names are ambiguous or default, it orders by `Available` balance (ascending) to prefer active trading capital.
3.  **Dynamic Trial**: Attempt to place the order with the highest-priority account.
4.  **Automatic Fallback**: If the API returns `ErrInvalidAccount` (HTTP 400), the orchestrator catches the exception and **automatically retries** with the next available account.
5.  **Deterministic Success**: The order only fails if ALL available accounts for that asset reject the request.

## Impact
This approach combines user intent (names) with behavioral evidence (balances) and a final safety net (retries). Mission Control is now "name-aware" and "battle-hardened" against Luno's multi-account architecture.

## Future SDK Improvement
The SDK should be updated to "unmask" 400 error codes into typed exceptions (e.g., `LunoInvalidAccountException`) to eliminate string-parsing logic in the orchestrator.

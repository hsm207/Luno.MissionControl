# RFC: System Resilience & Concurrency Mandate

## 🏛️ Overview
This RFC establishes the mandatory standards for handling concurrent state transitions and infrastructure error propagation within the Luno Mission Control ecosystem. These patterns are designed to ensure financial integrity and prevent race conditions inherent in the Blazor and ASP.NET Core threading models.

## Problem Statement
As identified in the *Architectural Debt Audit Report (2024-05-05)*, the current implementation lacks explicit protection against:
1.  **Asynchronous Re-entrancy**: Multiple UI events or background tasks interleaving during long-running API calls.
2.  **Cross-Circuit Parallelism**: Shared state access from multiple user circuits (tabs) or API endpoints.
3.  **Exception Leakage**: Infrastructure-specific SDK exceptions (e.g., `Luno.Sdk`) polluting the Application and Core layers.

## 🛠️ Proposed Standards

### 1. Concurrency Management (The Async-Lock Pattern)
To protect shared mutable state in Singleton or Scoped services, the `SemaphoreSlim(1, 1)` pattern must be utilized for asynchronous mutual exclusion.

**Implementation Standard:**
```csharp
private readonly SemaphoreSlim _lock = new(1, 1);

public async Task ExecuteAtomicActionAsync(CancellationToken ct)
{
    await _lock.WaitAsync(ct);
    try
    {
        // Protected state transition
    }
    finally
    {
        _lock.Release();
    }
}
```

### 2. Infrastructure Error Mapping (The Translation Pattern)
All infrastructure-level exceptions must be caught in the **Infrastructure Layer** and mapped to typed **Domain Exceptions** defined in the **Core Layer**.

**Example Mapping:**
- `429 Too Many Requests` → `ExchangeRateLimitExceededException`
- `ErrInvalidAccount` → `ExchangeInsolventAccountException`

## 📚 Sources & Verification
The following official documentation supports these architectural mandates:

- **Blazor Synchronization Context & Re-entrancy**: [Official Microsoft Docs](https://learn.microsoft.com/aspnet/core/blazor/components/synchronization-context?view=aspnetcore-10.0)
    - *Key Invariant*: "A component is re-entrant at any point where it awaits an incomplete Task."
- **Blazor EF Core Concurrency (Loading Flag Pattern)**: [Official Microsoft Docs](https://learn.microsoft.com/aspnet/core/blazor/blazor-ef-core?view=aspnetcore-10.0#database-access)
- **Dependency Injection Thread Safety**: [Official Microsoft Docs](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection/guidelines#thread-safety)
    - *Key Invariant*: "Any service (especially singletons) that holds shared mutable state must implement its own synchronization logic if accessed concurrently."
- **SemaphoreSlim Class Recommendation**: [Official Microsoft Docs](https://learn.microsoft.com/dotnet/api/system.threading.semaphoreslim?view=net-10.0#remarks)
    - *Key Invariant*: "The SemaphoreSlim class is the recommended semaphore for synchronization within a single app."

> [!NOTE]
> **Alternative Pattern: Atomic State Snapshots**
> For high-frequency state managers like `ServerBasketState`, consider implementing **Immutability** and **Atomic Swaps** via `Interlocked.Exchange` as an alternative to `SemaphoreSlim`. This pattern ensures that readers always see a consistent, point-in-time snapshot of the state without the overhead of asynchronous locking.

---
*Status: Proposed (Deferred to Post-Architecture-Refactor Sprint)*

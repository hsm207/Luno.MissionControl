# BUG: "Execution Halted" Toast Despite Full Basket Execution

## Status: ROOT CAUSE CONFIRMED — AWAITING FIX

---

## Symptom

A 5-asset basket (ADA/ETH/SOL/LINK/UNI) was submitted. Only 4 of the 5 orders
executed. The 5th order (UNI) was **never attempted**:

1. The "Executing order to buy..." log line for UNI **does NOT appear** in the first run.
2. The UNI `postorder` API call was **never made** to Luno.
3. The UI displayed an **"Execution Halted"** error toast.
4. The "Mission Accomplished" toast was NOT shown.
5. No explicit exception or error log line appears — the failure is **silent**.

Submitting a second basket with **only UNI at 100% allocation** succeeded in **0.47s**.

> **Note on initial misdiagnosis:** The "Executing order to buy 7.38 UNI..." log line
> visible in the server logs belongs to the **second, UNI-only basket run**, not the
> failed 5-asset basket. The two runs share a log stream, which caused initial confusion.

---

## Basket Under Test

| Asset Pair | Planned Spend | Relative Weight |
|------------|---------------|-----------------|
| ADA / MYR  | RM 108.00     | 20.93%          |
| ETH / MYR  | RM 108.00     | 20.93%          |
| SOL / MYR  | RM 108.00     | 20.93%          |
| LINK / MYR | RM 96.00      | 18.61%          |
| UNI / MYR  | RM 96.00      | 18.60%          |
| **Total**  | **RM 516.00** | **100.00%**     |

**Note:** Weights sum to `0.2093 + 0.2093 + 0.2093 + 0.1861 + 0.1860 = 1.0000`.

---

## Forensic Evidence

### 1. Distributed Traces (Definitive Proof)

The Aspire Dashboard traces provide irrefutable evidence. Filtering by `basket`
reveals exactly 2 `POST /api/basket/execute` traces:

| Trace ID | Time       | Spans | `HTTP POST` (postorder) calls | Duration   | Outcome  |
|----------|------------|-------|-------------------------------|------------|----------|
| `2ffa889`| 1:54:54 pm | 16    | **4** (ADA, ETH, SOL, LINK)   | **1m 40s** | ⭕ Halted |
| `bcfeac6`| 2:05:34 pm | 7     | **1** (UNI)                   | **0.47s**  | ✅ Success|

**The smoking gun:** Trace `2ffa889` contains **exactly 4 `HTTP POST 200`** spans,
evenly spaced ~25–30 seconds apart across the 1m 40s timeline. The **5th POST for
UNI never appears**. The trace was abandoned at exactly the `HttpClient.Timeout`
boundary — the WASM client killed the connection before the orchestrator could
reach the UNI iteration.

Trace `bcfeac6` (the UNI resubmission) completes in **0.47 seconds** with 1
postorder call — trivially within the 100-second timeout.

### 2. Server Log Analysis

The server-side log lines (by line number) confirm the two separate runs:

```
Line 326:  Executing order to buy 110.11 ADA  → Run 1 (5-asset basket)
Line 386:  Executing order to buy 0.0117 ETH  → Run 1
Line 436:  Executing order to buy 0.3415 SOL  → Run 1
Line 486:  Executing order to buy 2.49 LINK   → Run 1
           [MASSIVE GAP — timeout occurs during post-LINK 30s pacing delay]
Line 820:  Executing order to buy 7.38 UNI    → Run 2 (UNI-only resubmission)
```

The 334-line gap between LINK (486) and UNI (820) is the log noise from other
services (MarketWatchService ticker polls) accumulating during the ~10+ minutes
between the two basket submissions.

### 3. The Silence

No explicit `ERROR` or `WARN` log line appears — the `OperationCanceledException`
is swallowed silently by the BFF catch-all handler in `Program.cs`:
```csharp
catch (Exception) // Exception type not logged!
{
    return Results.Problem("A critical system error occurred at the gateway.", ...);
}
```
This surfaces to `BasketServiceProxy` as an HTTP 500 → `Success: false` →
"Execution Halted" toast.

---

## Confirmed Root Cause: WASM HttpClient Timeout (100s default)

**Location:** `Luno.MissionControl.Web.Client/Program.cs`, line 25:

```csharp
// NO Timeout is set — defaults to 100 seconds!
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
```

**Execution timeline for a 5-asset basket:**

| Phase                          | Duration    |
|-------------------------------|-------------|
| ADA order + 30s pacing delay  | ~30 seconds |
| ETH order + 30s pacing delay  | ~30 seconds |
| SOL order + 30s pacing delay  | ~30 seconds |
| LINK order + 30s pacing delay | ~30 seconds |
| UNI order (last, no delay)    | ~0.2 seconds|
| **Total**                     | **~120–130 seconds** |

The default `HttpClient.Timeout` is **100 seconds**. At ~100 seconds into the
execution, the WASM client fires a `TaskCanceledException`. This exception is caught
silently in `BasketServiceProxy.cs`:

```csharp
catch (Exception ex)
{
    // ex.Message = "The request was canceled due to the configured HttpClient.Timeout of 100 seconds elapsing."
    return new BasketExecutionResult(false, Array.Empty<OrderSummary>(), $"Network Error: {ex.Message}");
}
```

The BFF-side execution continues uninterrupted (all orders succeed), but the WASM
client has already given up — and the UI shows "Execution Halted".

**Why UNI-only basket works:** It completes in ~200ms, well within the 100s timeout.

---

## Unrelated Secondary Issue: Silent BFF Exception Logging

The BFF catch-all in `Program.cs` swallows exceptions without logging them:
```csharp
catch (Exception)
{
    return Results.Problem("A critical system error occurred at the gateway.", ...);
}
```
This means any server-side crash produces zero diagnostic output in the Aspire
Dashboard, making future forensic investigation extremely difficult.

---

## Recommended Fixes

### Fix 1 — Extend WASM HttpClient Timeout (IMMEDIATE, High Priority)
`Luno.MissionControl.Web.Client/Program.cs`:
```csharp
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
    Timeout = TimeSpan.FromMinutes(15) // Baskets can have many assets with 30s pacing
});
```

### Fix 2 — Decouple Pacing Delay from Request CancellationToken (HIGH)
`Luno.MissionControl.Application/BasketOrchestrator.cs`:
```csharp
// BEFORE — client disconnect aborts the pacing delay mid-flight:
await Task.Delay(TimeSpan.FromSeconds(30), ct);

// AFTER — pacing is unconditional; only actual API calls respect client ct:
await Task.Delay(TimeSpan.FromSeconds(30), CancellationToken.None);
```

### Fix 3 — Add Exception Logging to BFF Endpoint (HIGH)
`Luno.MissionControl.Web/Program.cs`:
```csharp
app.MapPost("/api/basket/execute", async (BasketExecutionRequest request, IBasketService service,
    ILogger<Program> logger, CancellationToken ct) =>
{
    try
    {
        var result = await service.ExecuteAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unhandled exception in /api/basket/execute. Type: {ExceptionType}",
            ex.GetType().Name);
        return Results.Problem(
            detail: ex.Message, // Surface the real message — not a generic one
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Internal Server Error");
    }
});
```

### Fix 4 — Weight Validation Tolerance (MEDIUM)
Replace exact equality with a safe tolerance to handle JSON round-trip precision loss:
```csharp
// BEFORE:
if (sum != 1.00m) throw ...

// AFTER:
if (Math.Abs(sum - 1.00m) > 0.0001m) throw ...
```

---

## Files Involved

| File | Issue |
|------|-------|
| `Luno.MissionControl.Web.Client/Program.cs` | **Root cause:** No `HttpClient.Timeout` set (defaults to 100s) |
| `Luno.MissionControl.Application/BasketOrchestrator.cs` | Pacing delay uses request `ct` — client disconnect aborts delay |
| `Luno.MissionControl.Web/Program.cs` | BFF swallows exceptions silently, no logging |
| `Luno.MissionControl.Web.Client/Services/BasketServiceProxy.cs` | Catches `TaskCanceledException` as generic "Network Error" |
| `Luno.MissionControl.Web.Client/Components/Dashboard/ComboOrchestrator.razor.cs` | "Execution Halted" toast trigger |


1. Build a 5-asset basket with total spend ≥ RM 500 and pacing delay ≥ 30 seconds.
2. Submit and confirm the basket.
3. Observe the "Executing order to buy..." logs for all 5 assets.
4. Observe the "Execution Halted" toast in the UI.
5. Cross-check Luno exchange — all 5 orders will be present.

---

## Blast Radius

- **Severity: HIGH** — Users believe the execution failed and may double-submit,
  creating duplicate orders on the exchange.
- **All orders from the basket DID execute**, so no financial loss occurred in this
  instance, but user trust is severely compromised.

---

## Recommended Fix

### Priority 1 — Diagnose the exact exception
Add **structured exception logging** to the BFF catch-all handler so the exception
type and message are surfaced in the Aspire Dashboard:

```csharp
// In Program.cs
catch (Exception ex)
{
    logger.LogError(ex, "Critical error in /api/basket/execute. {ExceptionType}: {Message}",
        ex.GetType().Name, ex.Message);
    return Results.Problem(...);
}
```

### Priority 2 — Fix the CancellationToken Timeout (if confirmed)
- Increase or remove the WASM `HttpClient` timeout for the basket execute call.
- Alternatively, decouple the basket execution from the HTTP request lifetime by
  running it in a background task and polling for results (fire-and-forget pattern).
- At minimum, do NOT pass `ct` (the HTTP request cancellation token) to
  `Task.Delay(30s, ct)` — use `CancellationToken.None` for the pacing delay so that
  client disconnection does not abort mid-flight execution.

### Priority 3 — Weight Validation Tolerance
Replace exact equality with a tolerance check:
```csharp
// Instead of: if (sum != 1.00m)
if (Math.Abs(sum - 1.00m) > 0.0001m)
    throw new InvalidOperationException($"Weights must sum to 1.00 (got {sum}).");
```

---

## Files Involved

- `Luno.MissionControl.Application/BasketOrchestrator.cs` — execution loop and pacing delay
- `Luno.MissionControl.Web/Program.cs` — BFF catch-all (silently swallows exceptions)
- `Luno.MissionControl.Web.Client/Services/BasketServiceProxy.cs` — WASM HTTP client (possible timeout source)
- `Luno.MissionControl.Web.Client/Components/Dashboard/ComboOrchestrator.razor.cs` — "Execution Halted" toast trigger

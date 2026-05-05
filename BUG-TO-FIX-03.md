# BUG: Weight Input Rounds to Integers (Decimals Blocked)

## Status: ROOT CAUSE IDENTIFIED — RE-RENDER RACE CONDITION

---

## Symptom

When a user tries to enter a decimal weight (e.g., 28.5%) in the basket
orchestrator, the input field effectively blocks the decimal portion. 
- Typing "28.5" often results in the field resetting to "28.00" or "29.00".
- It appears that only integer weights are allowed.
- The UI display (Total Allocation and Review Gate) also rounds these values to 
  0 decimal places (e.g., displaying "29%" for an underlying "28.5%" value).

---

## Confirmed Root Cause: The "Partial Parse" Re-render Loop

The `WeightInput` component suffers from a classic Blazor state-synchronization race
condition.

### 1. The Input Logic (`WeightInput.razor.cs`)
```csharp
protected async Task OnInputChanged(string? value)
{
    _rawValue = value; // 1. User types "28."
    if (decimal.TryParse(value, out var result)) // 2. "28." parses as 28m
    {
        await WeightChanged.InvokeAsync(result / 100m); // 3. Notify parent of 0.28m
    }
}
```

### 2. The Parent Loop (`ComboOrchestrator.razor`)
```razor
<WeightInput Weight="@context.Weight" 
             WeightChanged="@(val => UpdateWeight(context.Pair, val))" />
```
1. `UpdateWeight` is called with `0.28m`.
2. The `_allocations` list is updated.
3. Blazor detects a state change in the parent and **re-renders the grid**.
4. The new `Weight` (0.28m) is pushed back down to the `WeightInput`.

### 3. The Overwrite (`WeightInput.razor.cs`)
```csharp
protected override void OnParametersSet()
{
    // 4. Parameter 'Weight' is now 0.28m. 
    // This overwrites the user's active typing ("28.") with formatted text!
    _rawValue = (Weight * 100).ToString("F2"); // Result: "28.00"
}
```

**The user experience:** 
The moment the user types the decimal point (`.`), `decimal.TryParse` interprets 
it as the integer prefix. The parent re-renders, and `OnParametersSet` nukes 
the user's cursor and "unfinished" input with a formatted string like `"28.00"`.
The user is effectively trapped in "Integer Land". 🏰🚫

---

## Secondary Issue: Display Rounding

Even if the value is successfully stored as a decimal (e.g., via a lucky paste or
fast typing), the UI components round the display:

- **ComboOrchestrator.razor:** `@_totalWeight.ToString("P0")` → Rounds 100.4% to 100% or 28.5% to 29%.
- **ReviewGate.razor:** `@(alloc.Weight.ToString("P0"))` → Rounds 28.5% to 29%.

This creates a discrepancy where the "Planned Spend" (calculated with the real 
decimal) doesn't match the "Weight" shown.

---

## Recommended Fixes

### Fix 1 — Intelligent Parameter Sync in `WeightInput`
Only overwrite `_rawValue` if the incoming `Weight` parameter has actually 
changed significantly from the current `_rawValue`'s parsed value. This prevents 
the "re-render overwrite" while the user is mid-typing.

```csharp
protected override void OnParametersSet()
{
    // Only update _rawValue if it's null or the underlying value changed externally
    if (decimal.TryParse(_rawValue, out var current) && current == Weight * 100)
    {
        return; 
    }
    _rawValue = (Weight * 100).ToString("F2");
}
```

### Fix 2 — Update Display Formats
Change `P0` to `P2` (or a custom format) in all summary components to respect 
the precision the user expects.

```razor
@_totalWeight.ToString("P2") // Displays 100.00%
```

### Fix 3 — Use `step="any"` or `FluentNumberField` (if available)
Ensure the underlying HTML input doesn't enforce integer steps.
```razor
<FluentTextInput ... AdditionalAttributes="@(new Dictionary<string, object> { ["step"] = "any" })" />
```

---

## Files Involved

- `Luno.MissionControl.Web.Client/Components/Dashboard/WeightInput.razor.cs`
- `Luno.MissionControl.Web.Client/Components/Dashboard/ComboOrchestrator.razor`
- `Luno.MissionControl.Web.Client/Components/Dashboard/ReviewGate.razor`

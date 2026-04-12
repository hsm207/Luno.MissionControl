# Lesson 06: InteractiveAuto State Hydration and UI Stability Mandates

## The Incident

During the modernization of the `Luno.MissionControl` dashboard (targeting .NET 10 and Aspire 13), the "MARKET-SYNC LIMIT BUY" UI experienced multiple critical failure modes unique to the **Blazor InteractiveAuto** render mode. These failures resulted in "frozen" price labels, fatal JavaScript-driven UI paralysis, and inconsistent visual alignment.

Specific regressions included:
1.  **WASM State Starvation**: The client-side WebAssembly (WASM) components remained in a "Loading..." state indefinitely. While the Server-side pre-rendering had access to market data, the WASM runtime—operating on a separate circuit—was never "hydrated" with the initial state, causing it to wait for a SignalR broadcast that it had already missed.
2.  **Render-Tree Paralysis (JS Crash)**: The use of high-complexity, JavaScript-backed components (specifically `FluentAutocomplete`) introduced a fatal `TypeError` (`Cannot read properties of null (reading 'toLowerCase')`) within the Blazor lifecycle. Because the error occurred in the underlying JS interop, it paralyzed the entire C# render loop, stopping SignalR heartbeats and freezing all interactive elements.
3.  **Shadow DOM Styling Resistance**: Standard CSS rules for text alignment were ignored by Fluent UI input components. The internal `<input>` elements were shielded by the Shadow DOM, requiring specific "piercing" selectors to achieve professional financial formatting (right-alignment).

## Root Cause Analysis

1.  **Asynchronous Circuit Divergence**: In `InteractiveAuto`, the application effectively boots twice. The first (Server) pass generates HTML; the second (WASM) pass takes over interactivity. If the "truth" (Market Prices) is only pushed via events, the second pass starts with an empty state and no mechanism to "pull" the current snapshot.
2.  **The Autocomplete "One-Way Door"**: `FluentAutocomplete` and similar components rely on complex JS state management that is brittle when bound to high-frequency C# data updates. A single null-reference in the JS layer is a catastrophic failure for the Blazor circuit.
3.  **Encapsulation Blindness**: Attempting to style the *container* of a Web Component does not affect the *internals* of the control. Financial UIs require precise control over the inner `<input>` element which is only accessible via the `::part()` CSS pseudo-element.

## The Mandate: Stability and Hydration Patterns

### 1. The "Push-on-Connect" Hydration Pattern
Any service providing real-time data to an `InteractiveAuto` UI **MUST** implement a mechanism to push the current state immediately upon connection. 
- **Mechanism**: Use a Singleton "Inventory" or "Cache" on the server.
- **Trigger**: Intercept the SignalR `OnConnectedAsync` event to emit the full current state (e.g., all market prices) to the specific caller before moving to a broadcast-only model.

### 2. The "Managed Combo" over Autocomplete
For mission-critical search and selection, avoid JS-heavy Autocomplete components. 
- **Pattern**: Use a standard `<FluentSearch>` for input and a reactive `<FluentListbox>` for results. 
- **Enforcement**: Perform all filtering logic in **C#** (using `IEnumerable.Where`). This ensures that even if the search is high-frequency, the Blazor circuit remains stable and debuggable within the .NET runtime.

### 3. Shadow DOM Piercing for Financial Formatting
To align text within encapsulated controls, CSS must explicitly target the exported parts of the component.
- **Example**: 
  ```css
  fluent-number-field::part(control), 
  fluent-text-field::part(control) {
      text-align: right !important;
  }
  ```

### 4. Component-Level State Isolation
High-frequency data updates (e.g., live prices) must be isolated into "Humble View" components (e.g., `PriceLabel.razor`). This prevents a price tick from triggering a full `StateHasChanged()` on the parent component, which would otherwise reset user input focus or interrupt the search listbox.

## Actionable Guardrails

1.  **Check the Browser Console First**: If prices are "Loading..." or buttons are unresponsive in an `InteractiveAuto` app, audit the console for `TypeError` or `NullReference` exceptions in the JS interop layer.
2.  **Validate WASM Hydration**: Use `Console.WriteLine` or Tracing within `OnInitializedAsync` on the client project to verify that the initial data payload has actually arrived from the server.
3.  **Standardize Input Alignment**: All financial inputs in Mission Control MUST use the `right-align-text` CSS class, which is backed by the `::part(control)` piercing rule.

# Lesson 06: InteractiveAuto State Hydration and UI Stability Mandates

## The Incident

During the modernization of the `Luno.MissionControl` dashboard, the "MARKET-SYNC LIMIT BUY" UI experienced failure modes unique to **Blazor InteractiveAuto**:

1.  **WASM State Starvation**: Client-side WASM components remained in a "Loading..." state because the initial state (fetched during Server pre-rendering) was lost during the handover to the browser runtime.
2.  **Referential Instability**: The use of `FluentAutocomplete` resulted in previously selected items "disappearing" or checked states failing to persist when the search results were refreshed from the API.
3.  **Shadow DOM Styling Resistance**: Alignment rules were ignored by encapsulated Web Components, leading to unprofessional financial formatting.

## Root Cause Analysis

1.  **Hydration Handover**: In `InteractiveAuto`, the application boots twice. If the server-side state is not "baked" into the initial HTML, the WASM runtime starts empty.
2.  **Object Instance Mismatch**: If `OnOptionsSearch` returns new object instances from an API, the `FluentAutocomplete` component cannot match them by reference to the existing `SelectedItems` collection.
3.  **Encapsulation**: Internal `<input>` elements of Web Components are shielded by the Shadow DOM, requiring specific `::part()` selectors for styling.

## The Mandate: Stability and Hydration Patterns

### 1. The Persistence Bridge Pattern
Use the framework-native **`PersistentComponentState`** to synchronize data between Server and Client during hydration. 
- **Guideline**: Implement a `PersistenceBridge` service to "bake" the initial market data snapshot into the pre-rendered HTML, ensuring the WASM runtime has immediate access to the "current truth" without an extra network roundtrip.

### 2. Referential Stability for Autocomplete
When using `FluentAutocomplete` with async data sources, you **MUST** ensure referential equality for selected items.
- **Guideline**: Use the **`OptionSelectedComparer`** parameter to provide an `IEqualityComparer<TOption>` that matches items by a unique identifier (e.g., `UserId` or `PairId`) rather than by object reference.

### 3. Modern Styling (No !important)
To align text within encapsulated controls, use the `::part(control)` selector combined with professional design tokens.
- **Guideline**: Use standard CSS properties within the `::part` scope. **NEVER** use `!important` flags; if a style isn't applying, verify the specificity of your selector or use the `:root:root` bypass.

## Actionable Guardrails

1.  **Verify Hydration with Bridge**: Audit the `PersistenceBridge` registration to ensure it is correctly capturing and restoring state during the `InteractiveAuto` transition.
2.  **Standardize Autocomplete Comparers**: Any `FluentAutocomplete` bound to an external API MUST implement a custom `OptionSelectedComparer`.
3.  **Audit for !important**: Any use of `!important` in CSS is considered an architectural failure and must be remediated using token-based overrides.

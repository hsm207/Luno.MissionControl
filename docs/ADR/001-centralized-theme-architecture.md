# ADR-001: Centralized Theme Architecture for Fluent UI V5

## Status
Accepted / Implemented

## Context
The Luno Mission Control dashboard requires a premium, high-contrast "Wall Street Titan" aesthetic (Brushed Gold on Obsidian). Previous implementation attempts using Fluent UI V4 paradigms (like `<FluentDesignTheme />`) or imperative C# token overrides led to:
1.  **Hydration Flickers**: Components defaulting to blue during pre-rendering before JS-interop applied the gold (FOUC).
2.  **Shadow DOM Isolation**: Global CSS being blocked by Web Component boundaries in certain configurations.
3.  **Architectural Fragility**: Coupling layout logic to temporary theme state and JS-interop availability.

## Decision
We will adopt the **Native CSS Variable-First Architecture** introduced in Fluent UI Blazor V5. 

### Implementation Details:
1.  **CSS Global Overrides**: Directly override the official Fluent UI V5 CSS variables in the project's global `:root` selector within `app.css`.
2.  **Provider Sibling Pattern**: Place `<FluentProviders />` in `App.razor` as a **sibling** to `<Routes />`. 
    > [!IMPORTANT]
    > Do NOT wrap `<Routes />` with `<FluentProviders />` in `App.razor` as this causes pre-rendering serialization errors for `ChildContent`.
3.  **Static Shell Mandate**: The shell-level `<FluentProviders />` must remain **static** (no `@rendermode`) to ensure error-free initial rendering on the server.
4.  **Imperative Logic Removal**: Purge all usage of `AccentBaseColor.SetValueFor` and related C# imperative theme modifications.

### Key Justifications:
- **Official Recommendation**: Fluent UI V5 has explicitly shifted away from component-based providers in favor of CSS variables for performance and simplicity. (Ref: [V5 Migration Guide](https://www.fluentui-blazor.net/Migration/DesignTheme))
- **Performance**: Static CSS variables are resolved by the browser instantly during the CSSOM construction phase, eliminating the "Flash of Unstyled Content" (FOUC) during pre-rendering and hydration.
- **Shadow DOM Integration**: Managed V5 components are designed to consume specific CSS Custom Properties from the parent scope, ensuring styles pierce Shadow DOM boundaries without complex workarounds. (Ref: [V5 General Migration Overview](https://www.fluentui-blazor.net/Migration/General))

## Consequences
- **Positive**: Instant, flicker-free themed rendering across both Static and Interactive render modes.
- **Positive**: Reduced architectural complexity and elimination of brittle JS-interop dependencies for core styling.
- **Neutral**: Requires strict adherence to the official Fluent UI token naming conventions for any future styling extensions.

## Verification Plan

### Phase 1: Environment Reset
1.  **Kill Existing Processes**: Find and terminate all running `Luno.MissionControl.AppHost` PIDs.
2.  **Clean State**: Run `dotnet clean && dotnet build` to ensure architecture-wide state synchronization.

### Phase 2: Execution & Monitoring
1.  **Start AppHost**: Launch the Aspire AppHost and monitor the console for healthy resource status.
2.  **Resource Discovery**: Use `aspire list resources` to identify the active `web` project endpoint.

### Phase 3: Forensic UI Verification
1.  **Dynamic Rendering Check**: Navigate to the Basket Architect URL.
2.  **CSS Variable Inheritance**: Use browser dev tools to confirm that `<FluentButton>` is inheriting `#D4AF37` via `--colorBrandBackground`.
3.  **FOUC Suppression**: Perform hard reloads (Ctrl+F5) to verify the absence of the "Corporate Blue" flash during initial render.

## References
- [Fluent UI Blazor V5 Migration Guide](https://www.fluentui-blazor.net/Migration/DesignTheme)
- [Fluent UI Blazor Installation & Setup](https://www.fluentui-blazor.net/installation)

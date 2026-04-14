# Right Architecture for Fluent UI Blazor V5

This document outlines the stabilized, production-ready architecture for integrating Fluent UI Blazor V5 into a modern .NET 10 Blazor Web App using **InteractiveAuto** render modes. It addresses critical failures observed during migration from V4, including hydration crashes ("Blackouts") and service resolution errors ("Frozen Dialogs").

## 1. Decentralized vs. Programmatic Theming

### The Legacy Failure (V4 Approach)
In V4, theming often relied on the `FluentDesignTheme` component or imperative C# calls like `AccentBaseColor.SetValueFor`. In the context of **InteractiveAuto**, this causes architectural friction:
*   **DI Mismatch**: Setting a global theme in the Server's DI scope does not propagate to the WebAssembly (Client) DI scope.
*   **Hydration Flicker**: Programmatic changes during the transition from SSR to WASM cause visual jarring or "flickers."

### The V5 Standard: CSS-Variable First
Fluent UI V5 moved away from component-based theming in favor of native CSS Custom Properties (Design Tokens). 

**Best Practice**: Define the theme exclusively in `app.css` using the `:root` pseudo-class. 
*   **Scalability**: One file controls every component (Buttons, Grids, Dialogs).
*   **Stability**: Design tokens are resolved by the browser immediately, before Blazor hydration begins.
*   **Agnosticism**: Policy remains decoupled from implementation details.

```css
:root {
    color-scheme: dark;
    --colorBrandBackground: #D4AF37; /* Titan Gold */
    --colorNeutralBackground1: #080808; /* Titan Obsidian */
    /* ... other tokens */
}
```

## 2. The Single Provider Pattern

In InteractiveAuto apps, services like `IDialogService` and `IToastService` require a corresponding provider component in the render tree.

### The "Sibling Provider" Shell
The `<FluentProviders />` component must be placed at the **root of the interactive layout** (e.g., `MainLayout.razor` in the `.Client` project).

**Critical Rules:**
1.  **Never Wrap**: `<FluentProviders />` is a self-closing component. Wrapping the `@Body` or `<Routes />` inside it is a syntax error that crashes the Blazor component tree (BSOD).
2.  **Interactive Alignment**: The Provider must live in the same assembly/DI scope as the components calling its services. If your components are in the `.Client` project, the Provider should be in the `.Client` project.
3.  **App.razor Isolation**: Keep the static `App.razor` shell clean. Do not place Providers there unless the entire application is statically rendered.

```razor
@* MainLayout.razor (Client Project) *@
<div class="page">
    <main>
        @Body
    </main>
</div>

<FluentProviders /> @* Flat sibling at the end *@
```

## 3. Global Interactivity Strategy

For complex dashboards (like Mission Control), **Global Interactivity** is preferred over per-page interactivity to maintain a stable user experience.

*   **Router Placement**: Set `@rendermode="InteractiveAuto"` on the `<Routes />` component within `App.razor`.
*   **Assembly Visibility**: When the Router is interactive, the `Routes` component and its Layouts must be in the `.Client` project so the WASM runtime can locate the component types during hydration.

## 4. Forensic Verification Methodology

UI frameworks are highly sensitive to pre-rendering and serialization. Never assume an architecture is correct without verification in an isolated laboratory environment.

1.  **The Lab test**: Create a fresh, template-based project to verify the provider/theme interaction before applying to the main codebase.
2.  **The Titan Stress Test**: Verify interactivity (Dialogs/Toasts) immediately after a hard cache reload to ensure hydration successfully links services to the UI providers.

> [!IMPORTANT]
> A "Black Screen" in Blazor usually indicates a severed SignalR connection caused by a critical C# exception during the component tree's initial build or serialization. Always check the browser console for "Root component could not be found" or serialization errors when this occurs.

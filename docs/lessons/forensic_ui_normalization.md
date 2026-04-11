# Lesson: Forensic Normalization over Architectural Assumption

## The Incident

During the restoration of the Luno.MissionControl dashboard (targeting .NET 10 and Aspire 13), multiple critical failures were introduced into the web frontend and orchestration layers. This resulted in recurring 500 Internal Server Errors, dependency injection crashes, and asset resolution failures (404).

Specific regressions included:
1. **Dependency Collision**: Injecting a manual `LoggerFactory` into the Luno SDK client, which conflicted with the Aspire telemetry pipeline and caused AppHost crashes.
2. **Component/Service Mismatch**: Utilizing archaic property names (e.g., `PrimaryColor`) for Fluent UI components and missing required service registrations (`BasketState`) on the server-side pre-rendering engine.
3. **Asset 404s**: Failures in resolving CSS/JS files due to a reliance on legacy static paths instead of the .NET 10 `@Assets` manifest system.

The restoration was only successful after a user-mandated "Laboratory-First" protocol was enforced, requiring an isolated, standard-compliant project to be built and verified against official documentation before attempting remediation in the main codebase.

## Root Cause Analysis

1. **Reliance on Generalized Templates**: The initial implementation relied on generalized "good architecture" assumptions and outdated templates rather than the specific, non-negotiable requirements of the Aspire 13 / .NET 10 stack.
2. **Failure of Isolation**: Debugging and implementation were attempted within the complex, stateful Mission Control project. The lack of a "known-good" baseline made identifying simple structural errors (like middleware order or parameter names) nearly impossible.
3. **Implicit Over-Engineering**: Attempting to customize logging and dependency injection early in the restoration process bypassed the "Service Defaults" provided by the framework, leading to a brittle orchestration layer that was incompatible with the dashboard telemetry.

## The Mandate: Laboratory-First Development

For any task involving cross-project orchestration, major dependency upgrades, or complex UI frameworks, development must follow the **Laboratory-First** workflow. 

Normalization of the environment must occur in an isolated `labs/` project before any code is modified in the production repository.

## Actionable Guardrails

1. **The Green Path Verification**: Before implementing a new feature or fixing an orchestration error, create a minimal "Aspire Starter" or "Blazor Web App" in the `labs/` directory. Verify that the "Green Path" (e.g., simple data fetching, standard component rendering) is fully operational.
2. **Documentation Grounding**: Every major architectural choice (e.g., asset resolution, DI patterns) must be grounded in a specific citation from the official framework documentation (e.g., learn.microsoft.com or aspire.dev).
3. **Side-by-Side Forensic Audit**: Remediation must be performed via a line-by-line comparison between the "Known Good" Lab project and the "Broken" target project. If a line in the target project deviates from the verified lab project without a documented reason, it must be normalized.
4. **Middleware & Asset Strictness**: Adhere strictly to the default middleware order and asset resolution patterns provided by the current .NET version templates. Avoid "legacy" path mappings or manual fingerprinted resolution scripts.

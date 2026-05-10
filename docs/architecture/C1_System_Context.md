# C1: System Context Diagram

This diagram provides a high-level overview of the **Luno Mission Control** system and its interactions with users, the specialized **Luno.SDK**, and the external exchange.

```mermaid
C4Context
    title System Context diagram for Luno Mission Control

    Person(user, "Trader", "A user of the Luno Mission Control system who manages multi-asset portfolios.")
    System(lunoMC, "Luno Mission Control", "Orchestrates complex multi-asset trading baskets and provides real-time market telemetry.")
    System(lunoSDK, "Luno.SDK", "Shared library/platform providing standardized access to the Luno Exchange API.")

    System_Ext(lunoAPI, "Luno API", "External cryptocurrency exchange used for market data and trade execution.")

    Rel(user, lunoMC, "Uses to manage baskets and monitor market telemetry", "HTTPS")
    Rel(lunoMC, lunoSDK, "Delegates exchange communication and account logic", "In-Process")
    Rel(lunoSDK, lunoAPI, "Fetches prices and executes basket trades", "REST/WebSockets")
```

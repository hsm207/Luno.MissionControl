# Feature Architecture: Sequence Flows

This document visualizes the high-fidelity flows for the core features of **Luno Mission Control** using Sequence Diagrams for maximum clarity.

## 🧺 Feature: Placing Combo Orders (Allocation)
This flow describes how a Trader orchestrates a multi-asset allocation (Combo Order) across the exchange.

```mermaid
sequenceDiagram
    autonumber
    actor U as Trader
    
    box "Luno Mission Control" #222
        participant W as Web Application
        participant D as Postgres
    end
    
    participant S as Luno.SDK
    participant A as Luno API

    U->>+W: Submit allocation weights
    W->>W: Calculate trade sizes
    W->>D: Persist 'Sticky' state
    W->>+S: Delegate trade execution
    S->>+A: Execute buy/sell orders
    A-->>-S: Order Confirmation
    S-->>-W: Execution Success
    W-->>-U: Stream telemetry (SignalR)
```

## 📌 Feature: Pinning Wallets
This flow describes how a Trader manages their focus by pinning specific wallets for trading.

```mermaid
sequenceDiagram
    autonumber
    actor U as Trader
    
    box "Luno Mission Control" #222
        participant W as Web Application
        participant D as Postgres
    end

    U->>+W: Toggle wallet 'Pinned' status
    W->>W: Validate orchestrator state
    W->>D: Update wallet preferences
    D-->>W: Persistence Confirmation
    W-->>-U: Confirm UI synchronization
```

---

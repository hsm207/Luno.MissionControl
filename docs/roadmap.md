# Product Roadmap: Luno Mission Control

**Vision**: To democratize quantitative portfolio orchestration by providing a high-performance, fee-free bridge between retail investors and institutional-grade exchange execution.

**Value Proposition**: Eliminate the "Retail Tax" of forced spreads and service fees by enabling precision, exchange-direct limit order baskets at zero platform cost.

---

## 🚀 Execution Phases

### Phase 1: Ticker Turbo (Real-Time Precision)
*   **Objective**: Reduce price latency to <100ms.
*   **Key Features**:
    *   Transition from 30s polling to persistent **WebSocket** streaming.
    *   Sub-second order book depth analysis for slippage prediction.
*   **Value**: Ensures order execution occurs at the intended price point, minimizing capital erosion during volatility.

### Phase 2: Trade Integrity & Secure Bridge
*   **Objective**: Zero-fail authentication and order routing integrity.
*   **Key Features**:
    *   Secure integration of `ICredentialProvider` vault for Luno API keys.
    *   Implementation of **Resilient Retry Logic** for transient network/API failures.
*   **Value**: Reliable, non-custodial trade execution that users can trust with their capital.

### Phase 3: Portfolio Forensics (Insights)
*   **Objective**: Actionable observability of portfolio health and trade history.
*   **Key Features**:
    *   **Live Valuation Engine**: Real-time NAV (Net Asset Value) computation.
    *   **Gilded Audit Trail**: High-fidelity trade history leveraging OpenTelemetry tracing.
*   **Value**: Provides institutional-grade transparency into portfolio performance and execution costs.

### Phase 4: Active Orchestration (Smart Rebalancing)
*   **Objective**: Automated capital preservation and profit taking.
*   **Key Features**:
    *   **DCA (Dollar Cost Averaging) Engine**: Automated recurring basket purchases.
    *   **Drift-Based Rebalancing**: Automatic order generation to return portfolio to target weights.
*   **Value**: Hands-off portfolio management that adheres strictly to the user’s long-term quantitative strategy.

---

## 📊 Success Metrics (KPIs)
1.  **Execution Accuracy**: Delta between target allocation and settled allocation (Target: <0.1%).
2.  **Cost Savings**: Total platform fees saved vs. "Instant Buy" equivalents.
3.  **UI Latency**: Response time for market state updates (Target: <200ms).

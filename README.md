# 🌌 Luno Mission Control

A utility dashboard for Luno investors designed to fix the friction points of the official interface, providing precision execution and custom portfolio combinations.

## ⚔️ Why This Exists: Solving Luno UI Friction

Mission Control was built to address specific limitations in the standard Luno experience, starting with the "Coin Combo" feature.

| Feature | Luno "Coin Combo" | 🌌 Mission Control |
| :--- | :--- | :--- |
| **Asset Weighting** | ❌ Forced Equal Split (33/33/33) | ✅ **Precision % Allocation** |
| **Asset Selection** | ❌ Limited Bundles | ✅ **Unlimited Custom Combinations** |
| **Execution Engine** | ❌ Retail "Instant Buy" (High Spread) | ✅ **Exchange-Direct Limit Orders** |
| **Platform Fees** | ❌ Calculated as a Service Fee | ✅ **100% Free** |

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [.NET Aspire CLI](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling) (`dotnet tool install -g Aspire.Cli`)

### 🧪 Development (Local)
To run the application locally for testing and development:

```bash
# Clone the repository
git clone https://github.com/hsm207/Luno.MissionControl.git
cd Luno.MissionControl

# Run the application using the Aspire AppHost
aspire start Luno.MissionControl.AppHost/Luno.MissionControl.AppHost.csproj
```

Once running, the **Aspire Dashboard** will provide the URLs for the Web Frontend and telemetry.

### 🏗️ Local Production (Deployment)
For production-like local deployments using Docker Compose, use the provided executive orchestrator:

```bash
# Execute deployment with Luno credentials
./scripts/deploy.sh --id <YOUR_KEY_ID> --secret <YOUR_KEY_SECRET>
```

This script leverages **.NET Aspire** to generate a deterministic Docker Compose manifest in `./aspire-output/`. 

> [!NOTE]
> **Data Persistence**: In production mode, PostgreSQL uses a named Docker volume (`mission-control-postgres-data`) to ensure your wallet preferences survive deployment cycles.

> [!IMPORTANT]
> **Credential Management**: You must pass your own credentials (API keys, etc.) as arguments to the script. Managing these secrets is the responsibility of the user.

---

## 📜 License
Distributed under the **MIT License**. See `LICENSE` for more information.

## 🤝 Contributing
Suggestions and friction-point reports are welcome! Feel free to open an issue or submit a pull request.

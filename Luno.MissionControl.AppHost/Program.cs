using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Register the Web Frontend (Server project which hosts the WASM client)
// Service Discovery allows the MarketWatchService to be traced by the Dashboard.
builder.AddProject<Projects.Luno_MissionControl_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

builder.Build().Run();

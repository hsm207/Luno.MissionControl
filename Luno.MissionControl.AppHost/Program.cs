using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var apiKeyId = builder.AddParameter("luno-api-key-id");
var apiKeySecret = builder.AddParameter("luno-api-key-secret", secret: true);

// Register the Web Frontend (Server project which hosts the WASM client)
// Service Discovery allows the MarketWatchService to be traced by the Dashboard.
builder.AddProject<Projects.Luno_MissionControl_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithEnvironment("OTEL_SERVICE_NAME", "Luno.MissionControl.BFF")
    .WithEnvironment("Luno__ApiKeyId", apiKeyId)
    .WithEnvironment("Luno__ApiKeySecret", apiKeySecret);

builder.Build().Run();

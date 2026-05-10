using Aspire.Hosting;
using Aspire.Hosting.Docker;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// We use port 0 to let the OS assign a random free port, avoiding sticky port conflicts.
builder.AddDockerComposeEnvironment("env")
    .WithDashboard(d => d.WithHostPort(0));

var apiKeyId = builder.AddParameter("luno-api-key-id",
    builder.Configuration["Luno:ApiKeyId"] ?? builder.Configuration["Parameters:luno-api-key-id"] ?? "MISSING_ID");

var apiKeySecret = builder.AddParameter("luno-api-key-secret",
    builder.Configuration["Luno:ApiKeySecret"] ?? builder.Configuration["Parameters:luno-api-key-secret"] ?? "MISSING_SECRET",
    secret: true);

var postgres = builder.AddPostgres("postgres");

if (!builder.Environment.IsDevelopment())
{
    postgres.WithDataVolume("mission-control-postgres-data");
}
else
{
    // [ARCHITECTURAL MANDATE] Ephemeral storage for Dev to ensure clean state on every F5
}

var settingsDb = postgres.AddDatabase("settingsdb");

var webfrontend = builder.AddProject<Projects.Luno_MissionControl_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithEnvironment("Luno__ApiKeyId", apiKeyId)
    .WithEnvironment("Luno__ApiKeySecret", apiKeySecret)
    .WithReference(settingsDb);

var migrations = builder.AddProject<Projects.Luno_MissionControl_MigrationService>("settings-migrations")
    .WithReference(settingsDb);

webfrontend.WaitForCompletion(migrations);

if (!builder.Environment.IsDevelopment())
{
    var endpoint = builder.Configuration["ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL"] ?? "http://env-dashboard:18890";
    webfrontend.WithEnvironment("ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL", endpoint);
}

builder.Build().Run();

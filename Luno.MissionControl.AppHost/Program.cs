using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Register the Web Frontend
builder.AddProject<Projects.Luno_MissionControl_Web>("webfrontend");

// Enable the Dashboard with explicit unsecured transport to resolve environmental validation errors
builder.Build().Run();

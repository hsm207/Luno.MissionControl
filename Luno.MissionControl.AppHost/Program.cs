var builder = DistributedApplication.CreateBuilder(args);

// Register the Web Frontend with Aspire orchestration
builder.AddProject<Projects.Luno_MissionControl_Web>("webfrontend");

builder.Build().Run();

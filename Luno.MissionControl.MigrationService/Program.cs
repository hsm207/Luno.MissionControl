using Luno.MissionControl.Infrastructure.Persistence;
using Luno.MissionControl.MigrationService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<SettingsDbContext>("settingsdb");

builder.Services.AddHostedService<MigrationWorker>();

var host = builder.Build();
await host.RunAsync();

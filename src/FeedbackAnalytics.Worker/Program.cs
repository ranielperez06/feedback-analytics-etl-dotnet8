using FeedbackAnalytics.Application.Services;
using FeedbackAnalytics.Infrastructure;
using FeedbackAnalytics.Worker;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<ExtractionOrchestrator>();
builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();
await host.RunAsync();

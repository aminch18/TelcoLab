using TelcoLab.Api.Features.ClearingWebhook;
using TelcoLab.Api.Infrastructure;
using TelcoLab.Domain.Subscriptions;

var builder = WebApplication.CreateBuilder(args);

// Co-host the Orleans silo inside the web API. The API is the edge (webhooks + demo
// controls); the silo is the distributed actor runtime. Storage/clustering/reminders
// are Postgres when configured, in-memory otherwise.
builder.Host.UseOrleans(silo => silo.ConfigureStorage(builder.Configuration));

// The grain's outbound port to the third party, as a typed HttpClient.
builder.Services.AddHttpClient<IPortingClient, HttpPortingClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["TelcoLab:ClearingHouseUrl"]
        ?? throw new InvalidOperationException("TelcoLab:ClearingHouseUrl not configured")));

builder.Services.AddSingleton<IPortingResultPublisher, PortingResultPublisher>();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Vertical-slice endpoints (one record per feature) are discovered automatically.
app.MapEndpoints();

// Liveness and readiness for container orchestrators.
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");

app.Run();

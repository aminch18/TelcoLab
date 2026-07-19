using TelcoLab.Abstractions;
using TelcoLab.Grains;
using TelcoLab.Silo;

var builder = WebApplication.CreateBuilder(args);

// Co-host the Orleans silo inside the same process as the web API. The API is the
// edge (inbound webhooks + demo controls); the silo is the distributed actor runtime.
builder.Host.UseOrleans(silo =>
{
    silo.UseLocalhostClustering();
    silo.AddMemoryGrainStorage("subscriptionStore");
    silo.UseInMemoryReminderService();
    // In-memory streams for the demo. A production silo would use a persistent stream
    // provider (Azure Queue / Event Hubs). PubSubStore holds stream subscriptions.
    silo.AddMemoryStreams(StreamConstants.ProviderName);
    silo.AddMemoryGrainStorage("PubSubStore");
});

// The grain's outbound port to the third party, as a typed HttpClient.
builder.Services.AddHttpClient<IPortingClient, HttpPortingClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["TelcoLab:ClearingHouseUrl"]
        ?? throw new InvalidOperationException("TelcoLab:ClearingHouseUrl not configured")));

builder.Services.AddSingleton<IPortingResultPublisher, PortingResultPublisher>();

var app = builder.Build();

app.MapPortingEndpoints();

app.Run();

using Orleans.Runtime;
using Orleans.Streams;
using TelcoLab.Abstractions;
using TelcoLab.ClearingHouse.Contracts;
using TelcoLab.Grains;
using TelcoLab.Silo;
using ContractsReason = TelcoLab.ClearingHouse.Contracts.PortingRejectionReason;
using DomainReason = TelcoLab.Abstractions.PortingRejectionReason;

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

// Outbound edge to the third party.
builder.Services.AddHttpClient<IPortingClient, HttpPortingClient>(client =>
{
    var url = builder.Configuration["TelcoLab:ClearingHouseUrl"]
        ?? throw new InvalidOperationException("TelcoLab:ClearingHouseUrl not configured");
    client.BaseAddress = new Uri(url);
});

var app = builder.Build();

// --- Demo controls -------------------------------------------------------------

app.MapPost("/subscriptions/{msisdn}/activate", async (string msisdn, IClusterClient cluster) =>
{
    var grain = cluster.GetGrain<ISubscriptionGrain>(msisdn);
    await grain.ActivateAsync();
    return Results.Ok(await grain.GetStateAsync());
});

app.MapPost("/subscriptions/{msisdn}/port", async (string msisdn, PortBody body, IClusterClient cluster) =>
{
    var grain = cluster.GetGrain<ISubscriptionGrain>(msisdn);
    await grain.RequestPortingAsync(body.DonorOperator);
    // 202: the port is now in flight; the real outcome arrives later via webhook.
    return Results.Accepted(value: await grain.GetStateAsync());
});

app.MapGet("/subscriptions/{msisdn}", async (string msisdn, IClusterClient cluster) =>
    Results.Ok(await cluster.GetGrain<ISubscriptionGrain>(msisdn).GetStateAsync()));

// --- Inbound edge from the third party ----------------------------------------

app.MapPost("/webhooks/clearing", async (
    PortingResultEvent evt, HttpRequest request,
    IClusterClient cluster, IConfiguration configuration, ILogger<Program> logger) =>
{
    // Authenticate the caller. This uses a shared secret for brevity; a production
    // webhook would verify an HMAC signature computed over the raw request body.
    var expectedSecret = configuration["TelcoLab:WebhookSecret"];
    if (!string.IsNullOrEmpty(expectedSecret) &&
        !string.Equals(request.Headers["X-Webhook-Secret"], expectedSecret, StringComparison.Ordinal))
    {
        logger.LogWarning("Rejected unauthenticated webhook for {Msisdn}", evt.Msisdn);
        return Results.Unauthorized();
    }

    logger.LogInformation("Webhook received: {Outcome} for {Msisdn}", evt.Outcome, evt.Msisdn);

    // Anti-corruption layer: translate the third party's wire contract into our
    // own domain vocabulary before it ever reaches the grain.
    var result = new PortingResult
    {
        RequestId = evt.RequestId,
        Succeeded = evt.Outcome == PortingOutcome.Completed,
        Cancelled = evt.Outcome == PortingOutcome.Cancelled,
        RejectionReason = evt.Reason is null ? null : MapReason(evt.Reason.Value)
    };

    // Publish to the porting-results stream instead of calling the grain directly.
    // The subscription grain and the audit grain both react to it.
    var stream = cluster.GetStreamProvider(StreamConstants.ProviderName)
        .GetStream<PortingResult>(StreamId.Create(StreamConstants.PortingResults, evt.Msisdn));
    await stream.OnNextAsync(result);

    return Results.Accepted();
});

app.MapGet("/subscriptions/{msisdn}/audit", async (string msisdn, IClusterClient cluster) =>
    Results.Ok(await cluster.GetGrain<IPortingAuditGrain>(msisdn).GetAuditAsync()));

app.Run();

static DomainReason MapReason(ContractsReason reason) => reason switch
{
    ContractsReason.DonorRefused => DomainReason.DonorRefused,
    ContractsReason.InvalidDocumentation => DomainReason.InvalidDocumentation,
    ContractsReason.NumberNotPortable => DomainReason.NumberNotPortable,
    ContractsReason.DebtOnAccount => DomainReason.DebtOnAccount,
    _ => DomainReason.Unknown
};

record PortBody(string DonorOperator);

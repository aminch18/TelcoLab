using TelcoLab.Abstractions;
using TelcoLab.ClearingHouse.Contracts;

namespace TelcoLab.Silo;

public record PortBody(string DonorOperator);

public static class PortingEndpoints
{
    public static IEndpointRouteBuilder MapPortingEndpoints(this IEndpointRouteBuilder app)
    {
        // Demo controls.
        var subs = app.MapGroup("/subscriptions");

        subs.MapPost("/{msisdn}/activate", async (string msisdn, IClusterClient cluster) =>
        {
            var grain = cluster.GetGrain<ISubscriptionGrain>(msisdn);
            await grain.ActivateAsync();
            return Results.Ok(await grain.GetStateAsync());
        });

        subs.MapPost("/{msisdn}/port", async (string msisdn, PortBody body, IClusterClient cluster) =>
        {
            var grain = cluster.GetGrain<ISubscriptionGrain>(msisdn);
            await grain.RequestPortingAsync(body.DonorOperator);
            // 202: the port is now in flight; the real outcome arrives later via webhook.
            return Results.Accepted(value: await grain.GetStateAsync());
        });

        subs.MapGet("/{msisdn}", async (string msisdn, IClusterClient cluster) =>
            Results.Ok(await cluster.GetGrain<ISubscriptionGrain>(msisdn).GetStateAsync()));

        subs.MapGet("/{msisdn}/audit", async (string msisdn, IClusterClient cluster) =>
            Results.Ok(await cluster.GetGrain<IPortingAuditGrain>(msisdn).GetAuditAsync()));

        // Inbound edge from the third party: authenticate, translate, publish.
        app.MapPost("/webhooks/clearing", async (PortingResultEvent evt, IPortingResultPublisher publisher) =>
        {
            await publisher.PublishAsync(evt.Msisdn, evt.ToDomain());
            return Results.Accepted();
        })
        .AddEndpointFilter<WebhookAuthFilter>();

        return app;
    }
}

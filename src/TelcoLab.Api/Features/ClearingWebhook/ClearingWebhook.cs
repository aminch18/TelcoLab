using TelcoLab.ClearingHouse.Contracts;

namespace TelcoLab.Api.Features.ClearingWebhook;

/// <summary>
/// Inbound edge from the third party: authenticate, translate the wire contract to the
/// domain, and publish onto the porting-results stream. The endpoint knows nothing about
/// who consumes the result.
/// </summary>
public record ClearingWebhook() : Post("/webhooks/clearing")
{
    public async Task<IResult> HandleAsync(
        PortingResultEvent evt,
        HttpRequest request,
        IConfiguration configuration,
        IPortingResultPublisher publisher)
    {
        // Shared-secret auth for brevity; production would verify an HMAC over the body.
        var expected = configuration["TelcoLab:WebhookSecret"];
        if (!string.IsNullOrEmpty(expected) &&
            !string.Equals(request.Headers["X-Webhook-Secret"], expected, StringComparison.Ordinal))
        {
            return Results.Unauthorized();
        }

        await publisher.PublishAsync(evt.Msisdn, evt.ToDomain());
        return Results.Accepted();
    }
}

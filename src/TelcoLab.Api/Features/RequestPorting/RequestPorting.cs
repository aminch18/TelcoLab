using Orleans;
using TelcoLab.Domain.Subscriptions;

namespace TelcoLab.Api.Features.RequestPorting;

public record PortBody(string DonorOperator);

public record RequestPorting() : Post("/subscriptions/{msisdn}/port")
{
    public async Task<IResult> HandleAsync(string msisdn, PortBody body, IClusterClient cluster)
    {
        var grain = cluster.GetGrain<ISubscriptionGrain>(msisdn);
        await grain.RequestPortingAsync(body.DonorOperator);
        // 202: the port is now in flight; the real outcome arrives later via webhook.
        return Results.Accepted(value: await grain.GetStateAsync());
    }
}

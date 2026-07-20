using Orleans;
using TelcoLab.Domain.Subscriptions;

namespace TelcoLab.Api.Features.ActivateSubscription;

public record ActivateSubscription() : Post("/subscriptions/{msisdn}/activate")
{
    public async Task<IResult> HandleAsync(string msisdn, IClusterClient cluster)
    {
        var grain = cluster.GetGrain<ISubscriptionGrain>(msisdn);
        await grain.ActivateAsync();
        return Results.Ok(await grain.GetStateAsync());
    }
}

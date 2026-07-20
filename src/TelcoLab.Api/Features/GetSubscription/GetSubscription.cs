using Orleans;
using TelcoLab.Domain.Subscriptions;

namespace TelcoLab.Api.Features.GetSubscription;

public record GetSubscription() : Get("/subscriptions/{msisdn}")
{
    public async Task<IResult> HandleAsync(string msisdn, IClusterClient cluster)
        => Results.Ok(await cluster.GetGrain<ISubscriptionGrain>(msisdn).GetStateAsync());
}

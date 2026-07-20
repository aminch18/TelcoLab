using Orleans;
using TelcoLab.Domain.Billing;

namespace TelcoLab.Api.Features.GetBalance;

public record GetBalance() : Get("/accounts/{msisdn}/balance")
{
    public async Task<IResult> HandleAsync(string msisdn, IClusterClient cluster)
        => Results.Ok(new { msisdn, balance = await cluster.GetGrain<IAccountGrain>(msisdn).GetBalance() });
}

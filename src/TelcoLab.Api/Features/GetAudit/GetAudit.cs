using Orleans;
using TelcoLab.Domain.Auditing;

namespace TelcoLab.Api.Features.GetAudit;

public record GetAudit() : Get("/subscriptions/{msisdn}/audit")
{
    public async Task<IResult> HandleAsync(string msisdn, IClusterClient cluster)
        => Results.Ok(await cluster.GetGrain<IPortingAuditGrain>(msisdn).GetAuditAsync());
}

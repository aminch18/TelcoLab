using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans.Streams;
using TelcoLab.Domain.Subscriptions;

namespace TelcoLab.Domain.Auditing;

// A second, independent consumer of the same porting-results stream. The webhook edge
// does not know this grain exists — adding it cost nothing on the producer side. This
// is the payoff of streams over a direct grain call.
[ImplicitStreamSubscription(StreamConstants.PortingResults)]
public class PortingAuditGrain(
    [PersistentState("audit", "subscriptionStore")] IPersistentState<PortingAudit> audit,
    ILogger<PortingAuditGrain> logger)
    : Grain, IPortingAuditGrain
{
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var stream = this.GetStreamProvider(StreamConstants.ProviderName)
            .GetStream<PortingResult>(StreamId.Create(StreamConstants.PortingResults, this.GetPrimaryKeyString()));
        await stream.SubscribeAsync(OnResultAsync);
        await base.OnActivateAsync(cancellationToken);
    }

    public Task<PortingAudit> GetAuditAsync() => Task.FromResult(audit.State);

    private async Task OnResultAsync(PortingResult result, StreamSequenceToken? token)
    {
        var outcome = result switch
        {
            { Succeeded: true } => "Completed",
            { Cancelled: true } => "Cancelled",
            _ => $"Rejected ({result.RejectionReason})"
        };

        audit.State = audit.State with
        {
            ResolvedCount = audit.State.ResolvedCount + 1,
            LastOutcome = outcome
        };
        await audit.WriteStateAsync();

        logger.LogInformation("AUDIT {Msisdn}: porting {Outcome} (total {Count})",
            this.GetPrimaryKeyString(), outcome, audit.State.ResolvedCount);
    }
}

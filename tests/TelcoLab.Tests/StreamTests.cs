using System.Diagnostics;
using Orleans.Runtime;
using Orleans.Streams;
using Orleans.TestingHost;
using TelcoLab.Abstractions;

namespace TelcoLab.Tests;

[Collection(nameof(ClusterCollection))]
public class StreamTests(ClusterFixture fixture)
{
    private readonly TestCluster _cluster = fixture.Cluster;

    [Fact]
    public async Task Result_published_to_stream_resolves_both_the_subscription_and_the_audit()
    {
        const string msisdn = "+34600000010";
        var grain = _cluster.GrainFactory.GetGrain<ISubscriptionGrain>(msisdn);
        await grain.ActivateAsync();
        await grain.RequestPortingAsync("ACME");
        var requestId = (await grain.GetStateAsync()).PendingPortingRequestId!.Value;

        // Activate the audit grain so it is subscribed before we publish.
        await _cluster.GrainFactory.GetGrain<IPortingAuditGrain>(msisdn).GetAuditAsync();

        var stream = _cluster.Client.GetStreamProvider(StreamConstants.ProviderName)
            .GetStream<PortingResult>(StreamId.Create(StreamConstants.PortingResults, msisdn));
        await stream.OnNextAsync(new PortingResult { RequestId = requestId, Succeeded = true, Cancelled = false });

        // Stream delivery is asynchronous — poll for the effect.
        await WaitUntil(async () => (await grain.GetStateAsync()).Status == SubscriptionStatus.Active);

        Assert.Equal(SubscriptionStatus.Active, (await grain.GetStateAsync()).Status);

        var audit = await _cluster.GrainFactory.GetGrain<IPortingAuditGrain>(msisdn).GetAuditAsync();
        Assert.Equal(1, audit.ResolvedCount);   // the second consumer reacted too
        Assert.Equal("Completed", audit.LastOutcome);
    }

    private static async Task WaitUntil(Func<Task<bool>> condition, int timeoutMs = 5000)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (await condition()) return;
            await Task.Delay(50);
        }
        throw new TimeoutException("Condition not met within timeout");
    }
}

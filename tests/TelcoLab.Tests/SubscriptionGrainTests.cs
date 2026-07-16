using Orleans.TestingHost;
using TelcoLab.Abstractions;

namespace TelcoLab.Tests;

[Collection(nameof(ClusterCollection))]
public class SubscriptionGrainTests(ClusterFixture fixture)
{
    private readonly TestCluster _cluster = fixture.Cluster;

    private ISubscriptionGrain Grain(string msisdn) => _cluster.GrainFactory.GetGrain<ISubscriptionGrain>(msisdn);

    [Fact]
    public async Task Activate_moves_to_Active()
    {
        var grain = Grain("+34600000001");
        await grain.ActivateAsync();

        var state = await grain.GetStateAsync();
        Assert.Equal(SubscriptionStatus.Active, state.Status);
    }

    [Fact]
    public async Task RequestPorting_from_Active_moves_to_Porting_with_pending_request()
    {
        var grain = Grain("+34600000002");
        await grain.ActivateAsync();

        await grain.RequestPortingAsync("ACME");

        var state = await grain.GetStateAsync();
        Assert.Equal(SubscriptionStatus.Porting, state.Status);
        Assert.NotNull(state.PendingPortingRequestId);
        Assert.Equal("ACME", state.DonorOperator);
    }

    [Fact]
    public async Task RequestPorting_from_non_Active_throws()
    {
        var grain = Grain("+34600000003"); // still Inactive
        await Assert.ThrowsAsync<InvalidOperationException>(() => grain.RequestPortingAsync("ACME"));
    }

    [Fact]
    public async Task Completed_result_moves_back_to_Active()
    {
        var grain = Grain("+34600000004");
        await grain.ActivateAsync();
        await grain.RequestPortingAsync("ACME");
        var requestId = (await grain.GetStateAsync()).PendingPortingRequestId!.Value;

        await grain.ApplyPortingResultAsync(new PortingResult
        {
            RequestId = requestId, Succeeded = true, Cancelled = false
        });

        var state = await grain.GetStateAsync();
        Assert.Equal(SubscriptionStatus.Active, state.Status);
        Assert.Null(state.PendingPortingRequestId);
        Assert.Null(state.LastRejectionReason);
    }

    [Fact]
    public async Task Rejected_result_moves_to_PortingRejected_with_reason()
    {
        var grain = Grain("+34600000005");
        await grain.ActivateAsync();
        await grain.RequestPortingAsync("ACME");
        var requestId = (await grain.GetStateAsync()).PendingPortingRequestId!.Value;

        await grain.ApplyPortingResultAsync(new PortingResult
        {
            RequestId = requestId, Succeeded = false, Cancelled = false,
            RejectionReason = PortingRejectionReason.NumberNotPortable
        });

        var state = await grain.GetStateAsync();
        Assert.Equal(SubscriptionStatus.PortingRejected, state.Status);
        Assert.Equal(PortingRejectionReason.NumberNotPortable, state.LastRejectionReason);
    }

    [Fact]
    public async Task Result_with_stale_RequestId_is_ignored()
    {
        var grain = Grain("+34600000006");
        await grain.ActivateAsync();
        await grain.RequestPortingAsync("ACME");

        // A webhook from an earlier attempt carries a different request id.
        await grain.ApplyPortingResultAsync(new PortingResult
        {
            RequestId = Guid.NewGuid(), Succeeded = true, Cancelled = false
        });

        var state = await grain.GetStateAsync();
        Assert.Equal(SubscriptionStatus.Porting, state.Status); // unchanged — the stale result was rejected
        Assert.NotNull(state.PendingPortingRequestId);
    }

    [Fact]
    public async Task Result_when_not_Porting_is_ignored()
    {
        var grain = Grain("+34600000007");
        await grain.ActivateAsync(); // Active, no port in flight

        await grain.ApplyPortingResultAsync(new PortingResult
        {
            RequestId = Guid.NewGuid(), Succeeded = true, Cancelled = false
        });

        Assert.Equal(SubscriptionStatus.Active, (await grain.GetStateAsync()).Status);
    }
}

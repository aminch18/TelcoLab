using TelcoLab.Domain.Billing;

namespace TelcoLab.Tests;

[Collection(nameof(ClusterCollection))]
public class TransactionTests(ClusterFixture fixture)
{
    private readonly Orleans.TestingHost.TestCluster _cluster = fixture.Cluster;

    [Fact]
    public async Task Transfer_moves_credit_atomically()
    {
        var billing = _cluster.GrainFactory.GetGrain<IBillingGrain>(0);
        await _cluster.GrainFactory.GetGrain<IAccountGrain>("+34600000201").Deposit(100);

        await billing.Transfer("+34600000201", "+34600000202", 30);

        Assert.Equal(70, await _cluster.GrainFactory.GetGrain<IAccountGrain>("+34600000201").GetBalance());
        Assert.Equal(30, await _cluster.GrainFactory.GetGrain<IAccountGrain>("+34600000202").GetBalance());
    }

    [Fact]
    public async Task Transfer_with_insufficient_balance_rolls_back_both_sides()
    {
        var billing = _cluster.GrainFactory.GetGrain<IBillingGrain>(0);
        await _cluster.GrainFactory.GetGrain<IAccountGrain>("+34600000203").Deposit(10);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            billing.Transfer("+34600000203", "+34600000204", 50));

        // Neither side changed — the debit that would have overdrawn aborted the whole
        // transaction, so the deposit on the target never committed either.
        Assert.Equal(10, await _cluster.GrainFactory.GetGrain<IAccountGrain>("+34600000203").GetBalance());
        Assert.Equal(0, await _cluster.GrainFactory.GetGrain<IAccountGrain>("+34600000204").GetBalance());
    }
}

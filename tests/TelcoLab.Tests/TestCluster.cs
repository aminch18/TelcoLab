using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Orleans.TestingHost;
using TelcoLab.Domain;
using TelcoLab.Domain.Subscriptions;

namespace TelcoLab.Tests;

/// <summary>
/// A no-op porting client so the grain's state machine can be tested without a real
/// clearing house. The outbound call is verified separately in the end-to-end demo.
/// </summary>
public class FakePortingClient : IPortingClient
{
    public Task SubmitPortingRequestAsync(Guid requestId, string msisdn, string donorOperator)
        => Task.CompletedTask;
}

public class TestSiloConfigurator : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder
            .AddMemoryGrainStorage("subscriptionStore")
            .AddMemoryGrainStorage("transactionalStore")
            .UseInMemoryReminderService()
            .UseTransactions()
            .AddMemoryStreams(StreamConstants.ProviderName)
            .AddMemoryGrainStorage("PubSubStore")
            .ConfigureServices(services => services.AddSingleton<IPortingClient, FakePortingClient>());
    }
}

public class TestClientConfigurator : IClientBuilderConfigurator
{
    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
        => clientBuilder.AddMemoryStreams(StreamConstants.ProviderName);
}

[CollectionDefinition(nameof(ClusterCollection))]
public class ClusterCollection : ICollectionFixture<ClusterFixture>;

public sealed class ClusterFixture : IDisposable
{
    public TestCluster Cluster { get; }

    public ClusterFixture()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurator>();
        builder.AddClientBuilderConfigurator<TestClientConfigurator>();
        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public void Dispose() => Cluster.StopAllSilos();
}

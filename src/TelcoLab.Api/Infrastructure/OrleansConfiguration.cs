using System.Data.Common;
using System.Net;
using Npgsql;
using Orleans.Configuration;
using TelcoLab.Domain;

namespace TelcoLab.Api.Infrastructure;

public static class OrleansConfiguration
{
    private const string Invariant = "Npgsql";

    /// <summary>
    /// Wires storage, reminders and clustering. With a Postgres connection string it uses
    /// the ADO.NET providers (durable, cluster-shared — required for multi-silo); without
    /// one it falls back to in-memory so the demo and tests run with no infrastructure.
    /// The grain code is identical either way — this is the "config, not redesign" claim.
    /// </summary>
    public static ISiloBuilder ConfigureStorage(this ISiloBuilder silo, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return silo
                .UseLocalhostClustering()
                .AddMemoryGrainStorage("subscriptionStore")
                .AddMemoryGrainStorage("PubSubStore")
                .UseInMemoryReminderService()
                .AddMemoryStreams(StreamConstants.ProviderName);
        }

        DbProviderFactories.RegisterFactory(Invariant, NpgsqlFactory.Instance);

        silo.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "telcolab";
            options.ServiceId = "telcolab";
        });

        // Distinct ports let several silos run on one host and form a real cluster. In a
        // container orchestrator the advertised IP must be the container's own address,
        // so it is configurable rather than hard-coded to loopback.
        silo.Configure<EndpointOptions>(options =>
        {
            options.SiloPort = configuration.GetValue("Orleans:SiloPort", 11111);
            options.GatewayPort = configuration.GetValue("Orleans:GatewayPort", 30000);

            // "auto" resolves the container's own IP on the orchestrator network; an
            // explicit value is honoured; empty falls back to loopback for local runs.
            var advertised = configuration["Orleans:AdvertisedIP"];
            options.AdvertisedIPAddress = advertised switch
            {
                null or "" => IPAddress.Loopback,
                "auto" => ResolveHostIPv4(),
                _ => IPAddress.Parse(advertised)
            };
        });

        return silo
            .UseAdoNetClustering(o => { o.Invariant = Invariant; o.ConnectionString = connectionString; })
            .AddAdoNetGrainStorage("subscriptionStore", o => { o.Invariant = Invariant; o.ConnectionString = connectionString; })
            .AddAdoNetGrainStorage("PubSubStore", o => { o.Invariant = Invariant; o.ConnectionString = connectionString; })
            .UseAdoNetReminderService(o => { o.Invariant = Invariant; o.ConnectionString = connectionString; })
            .AddMemoryStreams(StreamConstants.ProviderName);
    }

    private static IPAddress ResolveHostIPv4()
    {
        var addresses = Dns.GetHostAddresses(Dns.GetHostName());
        return Array.Find(addresses, ip =>
                   ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                   && !IPAddress.IsLoopback(ip))
               ?? IPAddress.Loopback;
    }
}

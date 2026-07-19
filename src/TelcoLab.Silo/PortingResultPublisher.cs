using Orleans.Runtime;
using Orleans.Streams;
using TelcoLab.Abstractions;

namespace TelcoLab.Silo;

public interface IPortingResultPublisher
{
    Task PublishAsync(string msisdn, PortingResult result);
}

/// <summary>
/// Publishes a porting result onto the per-MSISDN stream. Whoever subscribes — the
/// subscription grain, the audit grain, anything added later — reacts to it; the edge
/// that calls this has no idea who they are.
/// </summary>
public class PortingResultPublisher(IClusterClient cluster, ILogger<PortingResultPublisher> logger)
    : IPortingResultPublisher
{
    public Task PublishAsync(string msisdn, PortingResult result)
    {
        logger.LogInformation("Publishing porting result for {Msisdn}", msisdn);

        var stream = cluster.GetStreamProvider(StreamConstants.ProviderName)
            .GetStream<PortingResult>(StreamId.Create(StreamConstants.PortingResults, msisdn));

        return stream.OnNextAsync(result);
    }
}

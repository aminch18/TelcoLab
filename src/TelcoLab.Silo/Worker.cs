using TelcoLab.Abstractions;

namespace TelcoLab.Silo;

public class Worker(IGrainFactory grainFactory, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var grain = grainFactory.GetGrain<ISubscriptionGrain>(Guid.NewGuid());

        await grain.ActivateAsync(msisdn: "+34600000000");
        var state = await grain.GetStateAsync();

        logger.LogInformation(
            "Subscription grain activated: status={Status} msisdn={Msisdn}",
            state.Status,
            state.Msisdn);
    }
}

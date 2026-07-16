using System.Net.Http.Json;
using TelcoLab.ClearingHouse.Contracts;

namespace TelcoLab.ClearingHouse;

/// <summary>
/// Delivers a porting outcome back to the caller after a delay, simulating the
/// real-world lag while donor operator and national clearing house coordinate.
/// </summary>
public class CallbackScheduler(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<CallbackScheduler> logger)
{
    private TimeSpan Delay =>
        TimeSpan.FromSeconds(configuration.GetValue("ClearingHouse:CallbackDelaySeconds", 3));

    public void ScheduleCallback(string callbackUrl, PortingResultEvent result)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(Delay);
            try
            {
                var client = httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync(callbackUrl, result);
                logger.LogInformation(
                    "Delivered {Outcome} for {Msisdn} to {Url} -> {Status}",
                    result.Outcome, result.Msisdn, callbackUrl, (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to deliver porting result for {Msisdn} to {Url}",
                    result.Msisdn, callbackUrl);
            }
        });
    }
}

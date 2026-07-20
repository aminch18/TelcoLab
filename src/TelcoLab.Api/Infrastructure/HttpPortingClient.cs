using System.Net.Http.Json;
using TelcoLab.ClearingHouse.Contracts;
using TelcoLab.Domain.Subscriptions;

namespace TelcoLab.Api.Infrastructure;

/// <summary>
/// HTTP adapter that turns the grain's <see cref="IPortingClient"/> port into a
/// real call to the external clearing house, supplying our own webhook URL as the
/// callback address.
/// </summary>
public class HttpPortingClient(HttpClient http, IConfiguration configuration) : IPortingClient
{
    public async Task SubmitPortingRequestAsync(Guid requestId, string msisdn, string donorOperator)
    {
        var callbackBase = configuration["TelcoLab:CallbackBaseUrl"]
            ?? throw new InvalidOperationException("TelcoLab:CallbackBaseUrl not configured");

        var request = new PortingRequest
        {
            RequestId = requestId,
            Msisdn = msisdn,
            DonorOperator = donorOperator,
            CallbackUrl = $"{callbackBase.TrimEnd('/')}/webhooks/clearing"
        };

        var response = await http.PostAsJsonAsync("/v1/porting-requests", request);
        response.EnsureSuccessStatusCode();
    }
}

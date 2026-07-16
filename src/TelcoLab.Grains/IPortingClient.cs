namespace TelcoLab.Grains;

/// <summary>
/// Outbound port of the grain to the external clearing house. The grain says
/// "start a port for me"; the implementation (in the host) knows the URL and the
/// callback address. Keeping this behind an interface keeps the grain testable
/// and free of HTTP concerns.
/// </summary>
public interface IPortingClient
{
    Task SubmitPortingRequestAsync(Guid requestId, string msisdn, string donorOperator);
}

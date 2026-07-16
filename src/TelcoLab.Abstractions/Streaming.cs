namespace TelcoLab.Abstractions;

public static class StreamConstants
{
    public const string ProviderName = "StreamProvider";

    /// <summary>Namespace for porting-result events, keyed by MSISDN.</summary>
    public const string PortingResults = "porting-results";
}

/// <summary>
/// A second consumer of porting results, added without the publisher knowing it exists.
/// Keeps a small running audit per MSISDN.
/// </summary>
public interface IPortingAuditGrain : IGrainWithStringKey
{
    Task<PortingAudit> GetAuditAsync();
}

[GenerateSerializer]
public record PortingAudit
{
    [Id(0)]
    public int ResolvedCount { get; init; }

    [Id(1)]
    public string? LastOutcome { get; init; }
}

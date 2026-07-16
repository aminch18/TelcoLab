namespace TelcoLab.Abstractions;

public enum SubscriptionStatus
{
    Active,
    Porting,
    PortingRejected,
    PortingCancelled
}

[GenerateSerializer]
public record SubscriptionState
{
    [Id(0)]
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    [Id(1)]
    public string? Msisdn { get; set; }
}

public interface ISubscriptionGrain : IGrainWithGuidKey
{
    Task<SubscriptionState> GetStateAsync();

    Task ActivateAsync(string msisdn);
}

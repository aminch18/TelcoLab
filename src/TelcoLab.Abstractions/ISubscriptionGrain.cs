namespace TelcoLab.Abstractions;

public enum SubscriptionStatus
{
    Inactive,
    Active,
    Porting,
    PortingRejected,
    PortingCancelled
}

/// <summary>Domain-level rejection reasons, decoupled from any third party's wire contract.</summary>
public enum PortingRejectionReason
{
    DonorRefused,
    InvalidDocumentation,
    NumberNotPortable,
    DebtOnAccount,
    /// <summary>The clearing house never returned an outcome within the allowed window.</summary>
    TimedOut,
    Unknown
}

[GenerateSerializer]
public record SubscriptionState
{
    [Id(0)]
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Inactive;

    [Id(1)]
    public string? DonorOperator { get; set; }

    [Id(2)]
    public PortingRejectionReason? LastRejectionReason { get; set; }

    /// <summary>Idempotency key of the porting request currently in flight, if any.</summary>
    [Id(3)]
    public Guid? PendingPortingRequestId { get; set; }

    /// <summary>How many times the current port has been submitted, for the retry cap.</summary>
    [Id(4)]
    public int PortingAttempts { get; set; }
}

/// <summary>Outcome of a porting request, already translated into domain terms.</summary>
[GenerateSerializer]
public record PortingResult
{
    /// <summary>Correlates the result with the porting request currently in flight.</summary>
    [Id(0)]
    public required Guid RequestId { get; init; }

    [Id(1)]
    public required bool Succeeded { get; init; }

    [Id(2)]
    public required bool Cancelled { get; init; }

    [Id(3)]
    public PortingRejectionReason? RejectionReason { get; init; }
}

/// <summary>
/// A single phone subscription, keyed by its MSISDN. Owns its own lifecycle state
/// machine and coordinates number porting with an external clearing house.
/// </summary>
public interface ISubscriptionGrain : IGrainWithStringKey
{
    Task<SubscriptionState> GetStateAsync();

    Task ActivateAsync();

    /// <summary>Active → Porting. Submits the request to the clearing house; result arrives later.</summary>
    Task RequestPortingAsync(string donorOperator);

    /// <summary>Porting → Active / PortingRejected / PortingCancelled, once the clearing house replies.</summary>
    Task ApplyPortingResultAsync(PortingResult result);
}

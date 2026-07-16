namespace TelcoLab.ClearingHouse.Contracts;

/// <summary>
/// Request sent by an operator TO the clearing house to start a number port.
/// Async: the clearing house answers 202 and later calls back <see cref="CallbackUrl"/>.
/// </summary>
public record PortingRequest
{
    /// <summary>Idempotency key chosen by the caller; a retry with the same value is a no-op.</summary>
    public required Guid RequestId { get; init; }

    public required string Msisdn { get; init; }

    public required string DonorOperator { get; init; }

    /// <summary>Where the clearing house POSTs the outcome once it is known.</summary>
    public required string CallbackUrl { get; init; }
}

public enum PortingOutcome
{
    Completed,
    Rejected,
    Cancelled
}

public enum PortingRejectionReason
{
    DonorRefused,
    InvalidDocumentation,
    NumberNotPortable,
    DebtOnAccount
}

/// <summary>
/// Result the clearing house POSTs back to the caller's callback URL.
/// </summary>
public record PortingResultEvent
{
    public required Guid RequestId { get; init; }

    public required string Msisdn { get; init; }

    public required PortingOutcome Outcome { get; init; }

    /// <summary>Only populated when <see cref="Outcome"/> is <see cref="PortingOutcome.Rejected"/>.</summary>
    public PortingRejectionReason? Reason { get; init; }
}

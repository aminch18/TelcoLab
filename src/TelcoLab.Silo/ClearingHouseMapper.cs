using TelcoLab.Abstractions;
using TelcoLab.ClearingHouse.Contracts;
using ContractsReason = TelcoLab.ClearingHouse.Contracts.PortingRejectionReason;
using DomainReason = TelcoLab.Abstractions.PortingRejectionReason;

namespace TelcoLab.Silo;

/// <summary>
/// Anti-corruption layer: translates the clearing house's wire contract into our own
/// domain vocabulary, so the grain never sees a third party's types.
/// </summary>
public static class ClearingHouseMapper
{
    public static PortingResult ToDomain(this PortingResultEvent evt) => new()
    {
        RequestId = evt.RequestId,
        Succeeded = evt.Outcome == PortingOutcome.Completed,
        Cancelled = evt.Outcome == PortingOutcome.Cancelled,
        RejectionReason = evt.Reason is null ? null : MapReason(evt.Reason.Value)
    };

    private static DomainReason MapReason(ContractsReason reason) => reason switch
    {
        ContractsReason.DonorRefused => DomainReason.DonorRefused,
        ContractsReason.InvalidDocumentation => DomainReason.InvalidDocumentation,
        ContractsReason.NumberNotPortable => DomainReason.NumberNotPortable,
        ContractsReason.DebtOnAccount => DomainReason.DebtOnAccount,
        _ => DomainReason.Unknown
    };
}

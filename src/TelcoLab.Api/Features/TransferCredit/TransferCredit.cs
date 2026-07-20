using Orleans;
using TelcoLab.Domain.Billing;

namespace TelcoLab.Api.Features.TransferCredit;

public record TransferBody(string From, string To, int Amount);

/// <summary>
/// Moves prepaid credit between two lines atomically. The debit and the credit happen in
/// one Orleans transaction — if the source has insufficient balance, neither side changes.
/// </summary>
public record TransferCredit() : Post("/transfers")
{
    public async Task<IResult> HandleAsync(TransferBody body, IClusterClient cluster)
    {
        try
        {
            await cluster.GetGrain<IBillingGrain>(0).Transfer(body.From, body.To, body.Amount);
            return Results.Ok(new { transferred = body.Amount, body.From, body.To });
        }
        catch (Exception ex)
        {
            // The transaction aborted (e.g. insufficient balance): both accounts are unchanged.
            return Results.BadRequest(new { error = ex.GetBaseException().Message });
        }
    }
}

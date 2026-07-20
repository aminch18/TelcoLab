using Orleans;
using TelcoLab.Domain.Billing;

namespace TelcoLab.Api.Features.Deposit;

public record DepositBody(int Amount);

public record Deposit() : Post("/accounts/{msisdn}/deposit")
{
    public async Task<IResult> HandleAsync(string msisdn, DepositBody body, IClusterClient cluster)
    {
        var account = cluster.GetGrain<IAccountGrain>(msisdn);
        await account.Deposit(body.Amount);
        return Results.Ok(new { msisdn, balance = await account.GetBalance() });
    }
}

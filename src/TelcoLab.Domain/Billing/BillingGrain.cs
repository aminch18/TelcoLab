namespace TelcoLab.Domain.Billing;

// Stateless coordinator. Its Transfer method opens one transaction; the Withdraw and
// Deposit on the two account grains join it, so they commit together or not at all.
public class BillingGrain : Grain, IBillingGrain
{
    public async Task Transfer(string fromMsisdn, string toMsisdn, int amount)
    {
        await GrainFactory.GetGrain<IAccountGrain>(fromMsisdn).Withdraw(amount);
        await GrainFactory.GetGrain<IAccountGrain>(toMsisdn).Deposit(amount);
    }
}

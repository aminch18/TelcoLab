using Orleans.Transactions.Abstractions;

namespace TelcoLab.Domain.Billing;

public class AccountGrain(
    [TransactionalState("balance", "transactionalStore")] ITransactionalState<AccountState> state)
    : Grain, IAccountGrain
{
    public Task Deposit(int amount) =>
        state.PerformUpdate(s => s.Balance += amount);

    public Task Withdraw(int amount) =>
        state.PerformUpdate(s =>
        {
            if (s.Balance < amount)
            {
                // Throwing inside the transaction aborts it: any Deposit on the other
                // account in the same transfer is rolled back too. No compensation code.
                throw new InvalidOperationException(
                    $"Insufficient balance: have {s.Balance}, need {amount}");
            }

            s.Balance -= amount;
        });

    public Task<int> GetBalance() =>
        state.PerformRead(s => s.Balance);
}

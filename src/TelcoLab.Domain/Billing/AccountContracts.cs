using Orleans.Concurrency;

namespace TelcoLab.Domain.Billing;

/// <summary>
/// Prepaid credit for one line (keyed by MSISDN). Its balance is transactional state,
/// so moving credit between two accounts is atomic across grains — no compensation code.
/// </summary>
public interface IAccountGrain : IGrainWithStringKey
{
    [Transaction(TransactionOption.CreateOrJoin)]
    Task Deposit(int amount);

    /// <summary>Throws if the balance is insufficient, which aborts the whole transaction.</summary>
    [Transaction(TransactionOption.CreateOrJoin)]
    Task Withdraw(int amount);

    [Transaction(TransactionOption.CreateOrJoin)]
    Task<int> GetBalance();
}

/// <summary>
/// Coordinates a credit transfer as a single ACID transaction spanning two accounts.
/// </summary>
public interface IBillingGrain : IGrainWithIntegerKey
{
    [Transaction(TransactionOption.Create)]
    Task Transfer(string fromMsisdn, string toMsisdn, int amount);
}

[GenerateSerializer]
public record AccountState
{
    [Id(0)]
    public int Balance { get; set; }
}

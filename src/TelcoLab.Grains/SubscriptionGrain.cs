using Orleans.Runtime;
using TelcoLab.Abstractions;

namespace TelcoLab.Grains;

public class SubscriptionGrain(
    [PersistentState("subscription", "subscriptionStore")] IPersistentState<SubscriptionState> state)
    : Grain, ISubscriptionGrain
{
    public Task<SubscriptionState> GetStateAsync() => Task.FromResult(state.State);

    public async Task ActivateAsync(string msisdn)
    {
        state.State.Status = SubscriptionStatus.Active;
        state.State.Msisdn = msisdn;
        await state.WriteStateAsync();
    }
}

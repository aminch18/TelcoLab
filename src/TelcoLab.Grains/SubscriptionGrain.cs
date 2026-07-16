using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans.Streams;
using TelcoLab.Abstractions;

namespace TelcoLab.Grains;

// The grain subscribes to its own per-MSISDN porting-results stream. The webhook edge
// publishes to that stream instead of calling the grain directly, which decouples the
// two and lets other consumers (see PortingAuditGrain) react to the same event.
[ImplicitStreamSubscription(StreamConstants.PortingResults)]
public class SubscriptionGrain(
    [PersistentState("subscription", "subscriptionStore")] IPersistentState<SubscriptionState> state,
    IPortingClient portingClient,
    ILogger<SubscriptionGrain> logger)
    : Grain, ISubscriptionGrain, IRemindable
{
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var stream = this.GetStreamProvider(StreamConstants.ProviderName)
            .GetStream<PortingResult>(StreamId.Create(StreamConstants.PortingResults, this.GetPrimaryKeyString()));
        await stream.SubscribeAsync((result, _) => ApplyPortingResultAsync(result));
        await base.OnActivateAsync(cancellationToken);
    }

    // The watchdog both retries a submission that failed and times the port out if the
    // clearing house never replies. Reminders survive silo restarts, so a port left in
    // flight for hours or days is still eventually resolved. Orleans reminders have a
    // one-minute minimum period.
    private const string PortingWatchdog = "porting-watchdog";
    private const int MaxPortingAttempts = 3;
    private static readonly TimeSpan WatchdogDue = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan WatchdogPeriod = TimeSpan.FromMinutes(1);

    public Task<SubscriptionState> GetStateAsync() => Task.FromResult(state.State);

    public async Task ActivateAsync()
    {
        state.State.Status = SubscriptionStatus.Active;
        await state.WriteStateAsync();
        logger.LogInformation("{Msisdn} activated", this.GetPrimaryKeyString());
    }

    public async Task RequestPortingAsync(string donorOperator)
    {
        if (state.State.Status != SubscriptionStatus.Active)
        {
            throw new InvalidOperationException(
                $"Cannot start porting from status {state.State.Status}");
        }

        var requestId = Guid.NewGuid();
        state.State.Status = SubscriptionStatus.Porting;
        state.State.DonorOperator = donorOperator;
        state.State.PendingPortingRequestId = requestId;
        state.State.PortingAttempts = 1;
        state.State.LastRejectionReason = null;

        // Persist the intent to port BEFORE talking to the third party. If the submit
        // below fails, we are durably in Porting with a known request id, and the
        // watchdog will retry — we never end up believing a port is in flight when it
        // is not, nor the reverse.
        await state.WriteStateAsync();

        await this.RegisterOrUpdateReminder(PortingWatchdog, WatchdogDue, WatchdogPeriod);
        await TrySubmitAsync(requestId, donorOperator);

        logger.LogInformation("{Msisdn} porting requested from {Donor}", this.GetPrimaryKeyString(), donorOperator);
    }

    public async Task ApplyPortingResultAsync(PortingResult result)
    {
        // Guard 1: only a subscription that is actually porting can accept a result.
        if (state.State.Status != SubscriptionStatus.Porting)
        {
            logger.LogWarning(
                "{Msisdn} ignoring porting result while in status {Status}",
                this.GetPrimaryKeyString(), state.State.Status);
            return;
        }

        // Guard 2: the result must correlate to the port currently in flight. A stale
        // or duplicated webhook from an earlier attempt carries a different request id
        // and must not resolve a newer one.
        if (result.RequestId != state.State.PendingPortingRequestId)
        {
            logger.LogWarning(
                "{Msisdn} ignoring porting result for stale request {RequestId} (pending {Pending})",
                this.GetPrimaryKeyString(), result.RequestId, state.State.PendingPortingRequestId);
            return;
        }

        state.State.Status = result switch
        {
            { Succeeded: true } => SubscriptionStatus.Active,
            { Cancelled: true } => SubscriptionStatus.PortingCancelled,
            _ => SubscriptionStatus.PortingRejected
        };
        state.State.LastRejectionReason = result.Succeeded ? null : result.RejectionReason;
        state.State.PendingPortingRequestId = null;
        await state.WriteStateAsync();
        await StopWatchdogAsync();

        logger.LogInformation(
            "{Msisdn} porting resolved -> {Status} ({Reason})",
            this.GetPrimaryKeyString(), state.State.Status, state.State.LastRejectionReason);
    }

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        if (reminderName != PortingWatchdog)
        {
            return;
        }

        // The port resolved between ticks; nothing left to watch.
        if (state.State.Status != SubscriptionStatus.Porting)
        {
            await StopWatchdogAsync();
            return;
        }

        if (state.State.PortingAttempts >= MaxPortingAttempts)
        {
            // The clearing house has had enough chances. Fail into a terminal state
            // rather than leaving the subscription stuck in Porting forever.
            state.State.Status = SubscriptionStatus.PortingRejected;
            state.State.LastRejectionReason = PortingRejectionReason.TimedOut;
            state.State.PendingPortingRequestId = null;
            await state.WriteStateAsync();
            await StopWatchdogAsync();
            logger.LogWarning(
                "{Msisdn} porting timed out after {Attempts} attempts",
                this.GetPrimaryKeyString(), state.State.PortingAttempts);
            return;
        }

        // Still porting and still no result: re-submit. The clearing house dedupes on
        // the request id, so re-sending the same id is safe.
        state.State.PortingAttempts++;
        await state.WriteStateAsync();
        await TrySubmitAsync(state.State.PendingPortingRequestId!.Value, state.State.DonorOperator!);
    }

    private async Task TrySubmitAsync(Guid requestId, string donorOperator)
    {
        try
        {
            await portingClient.SubmitPortingRequestAsync(requestId, this.GetPrimaryKeyString(), donorOperator);
        }
        catch (Exception ex)
        {
            // Swallow and let the watchdog retry — the intent is already persisted.
            logger.LogWarning(ex,
                "{Msisdn} porting submission failed; watchdog will retry", this.GetPrimaryKeyString());
        }
    }

    private async Task StopWatchdogAsync()
    {
        var reminder = await this.GetReminder(PortingWatchdog);
        if (reminder is not null)
        {
            await this.UnregisterReminder(reminder);
        }
    }
}

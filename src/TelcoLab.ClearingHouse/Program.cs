using System.Collections.Concurrent;
using TelcoLab.ClearingHouse;
using TelcoLab.ClearingHouse.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<CallbackScheduler>();
// Tracks request ids already accepted, so a retried submission is a no-op — exactly
// what a real clearing house does with an idempotency key.
builder.Services.AddSingleton<ConcurrentDictionary<Guid, byte>>();

var app = builder.Build();

// Simulated third party: an operator asks us to port a number. We answer 202
// immediately and deliver the real outcome later via the caller's callback URL.
//
// forceOutcome/reason are FAKE-SERVICE affordances for deterministic demos and
// tests; they are not part of the domain contract a real clearing house exposes.
app.MapPost("/v1/porting-requests", (
    PortingRequest request,
    CallbackScheduler scheduler,
    ConcurrentDictionary<Guid, byte> seenRequestIds,
    PortingOutcome? forceOutcome,
    PortingRejectionReason? reason) =>
{
    // Idempotency: a resubmission of the same request id does not schedule a second
    // callback. The caller (the grain's watchdog) can safely retry.
    if (!seenRequestIds.TryAdd(request.RequestId, 0))
    {
        return Results.Accepted($"/v1/porting-requests/{request.RequestId}");
    }

    // Default behaviour is deterministic so demos and tests are reproducible:
    // numbers ending in "99" are treated as not portable; everything else completes.
    // forceOutcome/reason override this for ad-hoc curl testing.
    var outcome = forceOutcome ?? (request.Msisdn.EndsWith("99")
        ? PortingOutcome.Rejected
        : PortingOutcome.Completed);

    if (outcome == PortingOutcome.Rejected && forceOutcome is null)
    {
        reason ??= PortingRejectionReason.NumberNotPortable;
    }

    var result = new PortingResultEvent
    {
        RequestId = request.RequestId,
        Msisdn = request.Msisdn,
        Outcome = outcome,
        Reason = outcome == PortingOutcome.Rejected
            ? reason ?? PortingRejectionReason.DonorRefused
            : null
    };

    scheduler.ScheduleCallback(request.CallbackUrl, result);

    return Results.Accepted($"/v1/porting-requests/{request.RequestId}");
});

app.MapGet("/health", () => Results.Ok("clearing-house up"));

app.Run();

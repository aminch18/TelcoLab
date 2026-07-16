using TelcoLab.ClearingHouse;
using TelcoLab.ClearingHouse.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<CallbackScheduler>();

var app = builder.Build();

// Simulated third party: an operator asks us to port a number. We answer 202
// immediately and deliver the real outcome later via the caller's callback URL.
//
// forceOutcome/reason are FAKE-SERVICE affordances for deterministic demos and
// tests; they are not part of the domain contract a real clearing house exposes.
app.MapPost("/v1/porting-requests", (
    PortingRequest request,
    CallbackScheduler scheduler,
    PortingOutcome? forceOutcome,
    PortingRejectionReason? reason) =>
{
    var outcome = forceOutcome ?? PortingOutcome.Completed;

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

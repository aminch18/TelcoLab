namespace TelcoLab.Silo;

/// <summary>
/// Rejects webhook calls that don't present the shared secret. This is deliberately
/// simple; a production webhook would verify an HMAC signature over the raw body.
/// </summary>
public class WebhookAuthFilter(IConfiguration configuration, ILogger<WebhookAuthFilter> logger)
    : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var expected = configuration["TelcoLab:WebhookSecret"];
        var provided = context.HttpContext.Request.Headers["X-Webhook-Secret"].ToString();

        if (!string.IsNullOrEmpty(expected) && !string.Equals(provided, expected, StringComparison.Ordinal))
        {
            logger.LogWarning("Rejected unauthenticated webhook");
            return Results.Unauthorized();
        }

        return await next(context);
    }
}

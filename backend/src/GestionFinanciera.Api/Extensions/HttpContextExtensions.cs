namespace GestionFinanciera.Api.Extensions;

/// <summary>
/// Best-effort client IP for audit logging. When <c>ForwardedHeaders</c> is
/// enabled the middleware already rewrites <see cref="ConnectionInfo.RemoteIpAddress"/>,
/// and we additionally prefer <c>X-Forwarded-For</c> (first hop = the real client)
/// as a defensive fallback for proxies that do not run through that pipeline.
/// </summary>
public static class HttpContextExtensions
{
    public static string? GetClientIpAddress(this HttpContext context)
    {
        // X-Forwarded-For: "client, proxy1, proxy2" — the leftmost is the real client.
        string? forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();

        return context.Connection.RemoteIpAddress?.ToString();
    }
}

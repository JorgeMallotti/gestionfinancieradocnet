namespace GestionFinanciera.Api.Middleware;

/// <summary>
/// Equivalent of the old project's Helmet: hardens HTTP responses with security headers.
/// HSTS is added only in production (dev uses http://localhost).
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment environment)
{
    private static readonly string Csp = string.Join(
        "; ",
        "default-src 'self'",
        "base-uri 'self'",
        "frame-ancestors 'none'",
        "object-src 'none'",
        "img-src 'self' data:",
        "style-src 'self' 'unsafe-inline'",
        "script-src 'self'");

    public async Task InvokeAsync(HttpContext context)
    {
        IHeaderDictionary headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Content-Security-Policy"] = Csp;
        headers["X-Permitted-Cross-Domain-Policies"] = "none";

        if (environment.IsProduction())
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        await next(context);
    }
}

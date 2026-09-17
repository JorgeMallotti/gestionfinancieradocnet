namespace GestionFinanciera.Api.Extensions;

/// <summary>
/// CSRF defence for the cookie-authenticated endpoints (AGENTS.md §13): the
/// browser states where the request comes from, and only an allowlisted origin
/// may proceed.
///
/// The header is fully attacker-controlled and it is NOT always a URI: browsers
/// send the literal string "null" for an OPAQUE origin (a sandboxed iframe, a
/// cross-origin redirect). Parsing it with <c>new Uri(...)</c> threw a
/// UriFormatException that surfaced as a 500 with a stack trace — from a public
/// endpoint, so anyone could trigger it at will and flood the logs (observed in
/// production on 2026-09-17). An origin we cannot parse is simply not an allowed
/// origin, so the caller fails closed with 403.
/// </summary>
public static class OriginPolicy
{
    /// <summary>
    /// True when the request may proceed. A MISSING origin is allowed because
    /// non-browser clients (curl, the deployment smoke test) do not send one;
    /// the comparison against the allowlist is what actually protects the
    /// endpoint, and it is done here for every value that is present.
    /// </summary>
    public static bool IsAllowed(string? origin, IEnumerable<string> allowedOrigins)
    {
        if (string.IsNullOrEmpty(origin))
            return true;

        // Fail closed: anything that is not a well-formed absolute URI (including
        // the opaque origin "null") is not an allowed origin — and must never
        // throw, because the caller is a public endpoint.
        if (!Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri))
            return false;

        // The allowlist is written by hand, so a given host may appear with or
        // without its port (dev serves the frontend on :4200 while production
        // uses the default port). Accept either form.
        string withPort = $"{uri.Scheme}://{uri.Host}:{uri.Port}";
        string withoutPort = $"{uri.Scheme}://{uri.Host}";

        return allowedOrigins.Contains(withPort, StringComparer.OrdinalIgnoreCase)
            || allowedOrigins.Contains(withoutPort, StringComparer.OrdinalIgnoreCase);
    }
}

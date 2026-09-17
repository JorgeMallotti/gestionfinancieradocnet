using GestionFinanciera.Api.Extensions;

namespace GestionFinanciera.UnitTests;

/// <summary>
/// The Origin header is attacker-controlled and is not always a URI, so the CSRF
/// check must never throw.
///
/// Regression test for the production 500 of 2026-09-17: <c>new Uri("null")</c>
/// threw a UriFormatException on every <c>POST /api/auth/refresh</c> coming from
/// an opaque origin (a sandboxed iframe), which turned a routine "this origin is
/// not allowed" into a 500 plus a stack trace in the log.
/// </summary>
public sealed class OriginPolicyTests
{
    private static readonly string[] Allowed =
    [
        "https://finanzas.mallottidigital.com",
        "http://localhost:4200",
    ];

    [Fact]
    public void MissingOrigin_IsAllowed_ForNonBrowserClients()
    {
        // curl and the deployment smoke test send no Origin header at all.
        Assert.True(OriginPolicy.IsAllowed(null, Allowed));
        Assert.True(OriginPolicy.IsAllowed(string.Empty, Allowed));
    }

    [Fact]
    public void OpaqueOrigin_IsRejected_InsteadOfThrowing()
    {
        // Browsers send the literal string "null" for an opaque origin.
        Assert.False(OriginPolicy.IsAllowed("null", Allowed));
    }

    [Theory]
    [InlineData("not a uri")]
    [InlineData("://missing-scheme")]
    [InlineData("file:///etc/passwd")]
    [InlineData("javascript:alert(1)")]
    public void MalformedOrNonHttpOrigin_IsRejected(string origin)
    {
        Assert.False(OriginPolicy.IsAllowed(origin, Allowed));
    }

    [Theory]
    [InlineData("https://finanzas.mallottidigital.com")]
    [InlineData("https://FINANZAS.mallottidigital.com")]
    [InlineData("http://localhost:4200")]
    public void AllowlistedOrigin_IsAllowed(string origin)
    {
        Assert.True(OriginPolicy.IsAllowed(origin, Allowed));
    }

    [Fact]
    public void AllowlistedHost_IsAcceptedWithTheDefaultPortToo()
    {
        // The allowlist entry has no port, so the explicit default port must
        // still match the same host.
        Assert.True(OriginPolicy.IsAllowed("https://finanzas.mallottidigital.com:443", Allowed));
    }

    [Theory]
    [InlineData("https://evil.example.com")]
    [InlineData("https://finanzas.mallottidigital.com.evil.com")]
    [InlineData("http://localhost:4300")]
    public void ForeignOrigin_IsRejected(string origin)
    {
        Assert.False(OriginPolicy.IsAllowed(origin, Allowed));
    }

    [Fact]
    public void EmptyAllowlist_RejectsEveryPresentOrigin()
    {
        Assert.False(OriginPolicy.IsAllowed("https://finanzas.mallottidigital.com", []));
    }
}

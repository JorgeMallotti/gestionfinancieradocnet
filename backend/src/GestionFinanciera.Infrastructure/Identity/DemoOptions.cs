namespace GestionFinanciera.Infrastructure.Identity;

/// <summary>
/// Demo quick-access configuration (MVP demo only).
/// The password is intentionally a documented demo credential — the whole
/// point of the demo is one-click access. Disable via Demo:Enabled=false
/// (App Setting in production) to remove the feature entirely.
/// </summary>
public sealed class DemoOptions
{
    public const string SectionName = "Demo";

    public bool Enabled { get; set; } = true;

    /// <summary>Password shared by every demo account. Never exposed to the client.</summary>
    public string Password { get; set; } = "Passw0rd!123";
}

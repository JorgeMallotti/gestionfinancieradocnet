namespace GestionFinanciera.Infrastructure.Services;

/// <summary>
/// SMTP settings bound from configuration. Real values live in Azure App
/// Settings / user-secrets — never committed (AGENTS.md §13).
/// </summary>
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string From { get; set; } = string.Empty;

    public string FromName { get; set; } = string.Empty;

    /// <summary>True = implicit TLS (SMTPS); false = STARTTLS when available.</summary>
    public bool UseSsl { get; set; }
}

using System.Text.Json;

namespace GestionFinanciera.Application.Common.Audit;

/// <summary>
/// Serializes entities for the audit trail (Before/After JSON). CamelCase to
/// match the API JSON contract.
/// </summary>
public static class AuditJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string Serialize(object value) => JsonSerializer.Serialize(value, Options);
}

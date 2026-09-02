using GestionFinanciera.Application.Features.Movements.DTOs;

namespace GestionFinanciera.Application.Features.Reports.DTOs;

/// <summary>
/// The data model consumed by the PDF/Excel generators and the email service.
/// Built by the report service from the account/movement repositories (data
/// access) — generators never touch EF Core directly.
/// </summary>
public sealed record ReportDataDto(
    string CompanyName,
    string AccountDisplayName,
    IReadOnlyList<MovementDto> Movements,
    decimal TotalIncoming,
    decimal TotalOutgoing,
    DateTimeOffset? From,
    DateTimeOffset? To)
{
    public decimal Balance => TotalIncoming - TotalOutgoing;
}


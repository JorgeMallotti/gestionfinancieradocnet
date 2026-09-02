namespace GestionFinanciera.Application.Features.Dashboard.DTOs;

/// <summary>One point of the monthly incoming/outgoing series (for charts).</summary>
public sealed record MonthlyPointDto(
    int Year,
    int Month,
    decimal Incoming,
    decimal Outgoing,
    decimal Net);

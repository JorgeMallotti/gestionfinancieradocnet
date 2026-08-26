namespace GestionFinanciera.Application.Features.Dashboard.DTOs;

/// <summary>One point of the monthly income/expense series (for charts).</summary>
public sealed record MonthlyPointDto(
    int Year,
    int Month,
    decimal Income,
    decimal Expenses,
    decimal Balance);

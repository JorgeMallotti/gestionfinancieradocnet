namespace GestionFinanciera.Application.Features.Dashboard.DTOs;

/// <summary>Financial overview of an account for a period (incoming vs outgoing).</summary>
public sealed record DashboardSummaryDto(
    decimal Balance,
    decimal TotalIncoming,
    decimal TotalOutgoing,
    int MovementCount,
    DateTimeOffset? From,
    DateTimeOffset? To);

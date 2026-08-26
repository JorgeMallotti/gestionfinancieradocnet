namespace GestionFinanciera.Application.Features.Reports.DTOs;

/// <summary>Request to send a PDF financial report by email.</summary>
public sealed record EmailReportDto(
    string Email,
    DateTimeOffset? From,
    DateTimeOffset? To);

using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Movements.DTOs;

/// <summary>Query for the paginated movement list of an account.</summary>
public sealed record MovementQueryDto(
    int Page = 1,
    int PageSize = 20,
    MovementType? Type = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null);

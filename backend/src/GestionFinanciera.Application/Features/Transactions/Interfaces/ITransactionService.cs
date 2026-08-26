using GestionFinanciera.Application.Common.Pagination;
using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Transactions.DTOs;

namespace GestionFinanciera.Application.Features.Transactions.Interfaces;

/// <summary>
/// Transaction business logic contract. The controller resolves companyId/userId/role
/// from the JWT and passes them here. Mutations return the full resource (§12).
/// </summary>
public interface ITransactionService
{
    Task<Result<PagedResult<TransactionDto>>> GetByCompanyAsync(
        Guid companyId, TransactionQueryDto query, CancellationToken ct);

    Task<Result<TransactionDto>> GetByIdAsync(
        Guid id, Guid companyId, CancellationToken ct);

    Task<Result<TransactionDto>> CreateAsync(
        CreateTransactionDto dto, Guid companyId, Guid userId, string role, string? ipAddress, CancellationToken ct);

    Task<Result<TransactionDto>> UpdateAsync(
        Guid id, UpdateTransactionDto dto, Guid companyId, Guid userId, string role, string? ipAddress, CancellationToken ct);

    Task<Result> DeleteAsync(
        Guid id, Guid companyId, Guid userId, string role, string? ipAddress, CancellationToken ct);
}

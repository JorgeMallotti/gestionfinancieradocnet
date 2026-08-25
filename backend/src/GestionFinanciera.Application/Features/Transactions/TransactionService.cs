using FluentValidation;

using GestionFinanciera.Application.Common.Audit;
using GestionFinanciera.Application.Common.Pagination;
using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Categories.Interfaces;
using GestionFinanciera.Application.Features.Transactions.DTOs;
using GestionFinanciera.Application.Features.Transactions.Interfaces;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Transactions;

/// <summary>
/// Transaction business rules: category must belong to the company, role
/// enforcement and audit trail. All validation contracts live in the validators.
/// </summary>
public sealed class TransactionService(
    ITransactionRepository repository,
    ICategoryRepository categories,
    IAuditService audit,
    IValidator<CreateTransactionDto> createValidator,
    IValidator<UpdateTransactionDto> updateValidator) : ITransactionService
{
    private const int MaxPageSize = 100;

    public async Task<Result<PagedResult<TransactionDto>>> GetByCompanyAsync(
        Guid companyId, TransactionQueryDto query, CancellationToken ct)
    {
        if (query.PageSize > MaxPageSize)
            query = query with { PageSize = MaxPageSize };

        var result = await repository.GetByCompanyAsync(companyId, query, ct);
        var items = result.Items.Select(TransactionDto.FromEntity).ToList();
        return Result<PagedResult<TransactionDto>>.Success(
            new PagedResult<TransactionDto>(items, result.TotalCount, result.Page, result.PageSize));
    }

    public async Task<Result<TransactionDto>> GetByIdAsync(
        Guid id, Guid companyId, CancellationToken ct)
    {
        var transaction = await repository.GetByIdAsync(id, companyId, ct);
        if (transaction is null)
            return Result<TransactionDto>.Failure("Transaction not found.");

        return Result<TransactionDto>.Success(TransactionDto.FromEntity(transaction));
    }

    public async Task<Result<TransactionDto>> CreateAsync(
        CreateTransactionDto dto, Guid companyId, Guid userId, string role, string? ipAddress, CancellationToken ct)
    {
        if (!CanMutate(role))
            return Result<TransactionDto>.Failure("You do not have permission to create transactions.");

        var validation = await createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return Result<TransactionDto>.Failure(validation.Errors.First().ErrorMessage);

        if (await categories.GetByIdAsync(dto.CategoryId, companyId, ct) is null)
            return Result<TransactionDto>.Failure("Category does not exist.");

        var transaction = new Transaction
        {
            CompanyId = companyId,
            CreatedByUserId = userId,
            CategoryId = dto.CategoryId,
            Type = dto.Type,
            Amount = dto.Amount,
            Currency = dto.Currency.ToUpperInvariant(),
            Date = dto.Date,
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
        };

        await repository.AddAsync(transaction, ct);
        await audit.RecordAsync(
            companyId, userId, AuditAction.Create, nameof(Transaction), transaction.Id,
            beforeJson: null, afterJson: AuditJson.Serialize(transaction.ToAuditSnapshot()), ipAddress, ct);

        var saved = await repository.GetByIdAsync(transaction.Id, companyId, ct);
        if (saved is null)
            return Result<TransactionDto>.Failure("Transaction was created but could not be read back.");

        return Result<TransactionDto>.Success(TransactionDto.FromEntity(saved));
    }

    public async Task<Result<TransactionDto>> UpdateAsync(
        Guid id, UpdateTransactionDto dto, Guid companyId, Guid userId, string role, string? ipAddress, CancellationToken ct)
    {
        if (!CanMutate(role))
            return Result<TransactionDto>.Failure("You do not have permission to update transactions.");

        var validation = await updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return Result<TransactionDto>.Failure(validation.Errors.First().ErrorMessage);

        var transaction = await repository.GetByIdAsync(id, companyId, ct);
        if (transaction is null)
            return Result<TransactionDto>.Failure("Transaction not found.");

        if (await categories.GetByIdAsync(dto.CategoryId, companyId, ct) is null)
            return Result<TransactionDto>.Failure("Category does not exist.");

        string beforeJson = AuditJson.Serialize(transaction.ToAuditSnapshot());

        transaction.CategoryId = dto.CategoryId;
        transaction.Type = dto.Type;
        transaction.Amount = dto.Amount;
        transaction.Currency = dto.Currency.ToUpperInvariant();
        transaction.Date = dto.Date;
        transaction.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

        await repository.UpdateAsync(transaction, ct);
        await audit.RecordAsync(
            companyId, userId, AuditAction.Update, nameof(Transaction), transaction.Id,
            beforeJson, AuditJson.Serialize(transaction.ToAuditSnapshot()), ipAddress, ct);

        return Result<TransactionDto>.Success(TransactionDto.FromEntity(transaction));
    }

    public async Task<Result> DeleteAsync(
        Guid id, Guid companyId, Guid userId, string role, string? ipAddress, CancellationToken ct)
    {
        if (!CanMutate(role))
            return Result.Failure("You do not have permission to delete transactions.");

        var transaction = await repository.GetByIdAsync(id, companyId, ct);
        if (transaction is null)
            return Result.Failure("Transaction not found.");

        string beforeJson = AuditJson.Serialize(transaction.ToAuditSnapshot());

        await repository.DeleteAsync(transaction, ct);
        await audit.RecordAsync(
            companyId, userId, AuditAction.Delete, nameof(Transaction), id,
            beforeJson, afterJson: null, ipAddress, ct);

        return Result.Success();
    }

    private static bool CanMutate(string role) =>
        role is nameof(UserRole.Admin) or nameof(UserRole.Finance);
}

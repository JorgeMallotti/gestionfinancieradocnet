using GestionFinanciera.Application.Features.Loans.Interfaces;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.UnitTests.Fakes;

/// <summary>In-memory loan repository for unit tests.</summary>
internal sealed class InMemoryLoanRepository : ILoanRepository
{
    private readonly List<Loan> _items = [];

    public List<Loan> Items => _items;

    public Task<Loan?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct) =>
        Task.FromResult(_items.FirstOrDefault(l => l.Id == id && l.CompanyId == companyId));

    public Task<IReadOnlyList<Loan>> GetByClientAsync(
        Guid companyId, Guid clientAccountId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Loan>>(_items
            .Where(l => l.CompanyId == companyId && l.ClientAccountId == clientAccountId)
            .OrderByDescending(l => l.CreatedAt)
            .ToList());

    public Task<IReadOnlyList<Loan>> GetByCompanyAsync(
        Guid companyId, LoanStatus? status, CancellationToken ct)
    {
        IEnumerable<Loan> query = _items.Where(l => l.CompanyId == companyId);
        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        return Task.FromResult<IReadOnlyList<Loan>>(
            query.OrderByDescending(l => l.CreatedAt).ToList());
    }

    public Task AddAsync(Loan loan, CancellationToken ct)
    {
        _items.Add(loan);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Loan loan, CancellationToken ct)
    {
        loan.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }

    /// <summary>Test helper: seeds a loan and returns it.</summary>
    public Loan Seed(Guid companyId, Guid clientAccountId, decimal amount,
        LoanStatus status = LoanStatus.Pending, decimal repaid = 0m)
    {
        var loan = new Loan
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ClientAccountId = clientAccountId,
            Amount = amount,
            RepaidAmount = repaid,
            Currency = "EUR",
            Reason = "Working capital",
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _items.Add(loan);
        return loan;
    }
}

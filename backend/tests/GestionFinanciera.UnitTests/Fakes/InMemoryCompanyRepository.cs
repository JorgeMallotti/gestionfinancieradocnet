using GestionFinanciera.Application.Features.Companies.Interfaces;

namespace GestionFinanciera.UnitTests.Fakes;

/// <summary>In-memory company repository returning a configurable name.</summary>
internal sealed class InMemoryCompanyRepository : ICompanyRepository
{
    public string? Name { get; set; } = "Test Company";

    public Task<string?> GetNameAsync(Guid companyId, CancellationToken ct) =>
        Task.FromResult(Name);
}

namespace GestionFinanciera.Application.Features.Companies.Interfaces;

/// <summary>
/// Minimal data access for the company (tenant) itself. Used by reports to
/// render the company name in the document header. CompanyId still comes
/// only from the JWT.
/// </summary>
public interface ICompanyRepository
{
    Task<string?> GetNameAsync(Guid companyId, CancellationToken ct);
}

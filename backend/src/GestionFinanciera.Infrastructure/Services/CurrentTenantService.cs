using GestionFinanciera.Application.Abstractions;

namespace GestionFinanciera.Infrastructure.Services;

/// <summary>
/// Ambient tenant resolution backed by AsyncLocal, so it flows through async calls
/// without touching HttpContext directly. Set by the API tenant middleware from the JWT.
/// </summary>
public sealed class CurrentTenantService : ICurrentTenant
{
    private static readonly AsyncLocal<Guid?> _companyId = new();

    public Guid? CompanyId => _companyId.Value;

    public void SetCompanyId(Guid companyId) => _companyId.Value = companyId;
}

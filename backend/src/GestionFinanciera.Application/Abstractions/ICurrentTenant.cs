namespace GestionFinanciera.Application.Abstractions;

/// <summary>
/// Resolves the current tenant (company) for EF Core global query filters.
/// The value is set by the API tenant middleware from the JWT claim — never from client input.
/// </summary>
public interface ICurrentTenant
{
    Guid? CompanyId { get; }

    void SetCompanyId(Guid companyId);
}

/// <summary>
/// Resolves the current authenticated user for auditing and ownership.
/// Set by the API from the JWT — never from client input.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    void SetUserId(Guid userId);
}

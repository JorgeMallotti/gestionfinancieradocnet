using GestionFinanciera.Application.Abstractions;

namespace GestionFinanciera.Infrastructure.Services;

/// <summary>
/// Ambient current-user resolution backed by AsyncLocal. Set by the API middleware
/// from the JWT — never from client input.
/// </summary>
public sealed class CurrentUserService : ICurrentUser
{
    private static readonly AsyncLocal<Guid?> _userId = new();

    public Guid? UserId => _userId.Value;

    public void SetUserId(Guid userId) => _userId.Value = userId;
}

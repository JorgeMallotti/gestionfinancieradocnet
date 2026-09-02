using GestionFinanciera.Api.Extensions;
using GestionFinanciera.Application.Abstractions;

namespace GestionFinanciera.Api.Middleware;

/// <summary>
/// Resolves the tenant and user from the JWT claims and exposes them through the
/// ambient services consumed by EF Core query filters and audit logging.
/// Runs only when the request carries an authenticated identity.
/// </summary>
public sealed class TenantMiddleware(RequestDelegate next, ICurrentTenant currentTenant, ICurrentUser currentUser)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            if (context.User.HasCompanyId())
                currentTenant.SetCompanyId(context.User.GetCompanyId());

            Guid userId = context.User.GetUserId();
            if (userId != Guid.Empty)
                currentUser.SetUserId(userId);
        }

        await next(context);
    }
}

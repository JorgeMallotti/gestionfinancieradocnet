using Microsoft.AspNetCore.Identity;

namespace GestionFinanciera.Infrastructure.Identity;

/// <summary>
/// Creates the well-known roles (Admin, User) if they do not exist. Bank model:
/// Admin = bank operator/mediator, User = client. Finance was removed 2026-09-02.
/// Shared by the auth service (client registration) and the demo seeder.
/// </summary>
public static class RoleSeeder
{
    public static readonly string[] DefaultRoles = ["Admin", "User"];

    public static async Task EnsureRolesAsync(
        RoleManager<IdentityRole<Guid>> roleManager, CancellationToken ct)
    {
        foreach (string role in DefaultRoles)
        {
            ct.ThrowIfCancellationRequested();

            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }
}

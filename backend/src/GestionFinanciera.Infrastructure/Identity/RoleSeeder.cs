using Microsoft.AspNetCore.Identity;

namespace GestionFinanciera.Infrastructure.Identity;

/// <summary>
/// Creates the well-known roles (Admin, Finance, User) if they do not exist.
/// Shared by the auth service (registration) and the demo seeder.
/// </summary>
public static class RoleSeeder
{
    public static readonly string[] DefaultRoles = ["Admin", "Finance", "User"];

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

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using SimpleAdmin.Api.Data;
using SimpleAdmin.Api.Models;

namespace SimpleAdmin.Api.Workers;

public class OpenIddictWorker : IHostedService
{
    private readonly IServiceProvider _provider;
    public OpenIddictWorker(IServiceProvider provider) => _provider = provider;

    public async Task StartAsync(CancellationToken ct)
    {
        await using var scope = _provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync(ct);

        // Seed admin user
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        const string adminEmail = "admin@simpleadmin.local";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var user = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user, "Admin1234!");
        }

        // Register SPA client
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        const string clientId = "simple-admin-spa";
        if (await manager.FindByClientIdAsync(clientId, ct) is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = clientId,
                ClientType = OpenIddictConstants.ClientTypes.Public,
                RedirectUris = { new Uri("http://localhost:5173/callback") },
                PostLogoutRedirectUris = { new Uri("http://localhost:5173/") },
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Authorization,
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.ResponseTypes.Code,
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "api"
                }
            }, ct);
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}

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

        // Seed test users
        var testUsers = new[]
        {
            ("alice@simpleadmin.local", "Alice1234!"),
            ("bob@simpleadmin.local", "Bob1234!")
        };

        foreach (var (email, password) in testUsers)
        {
            if (await userManager.FindByEmailAsync(email) is null)
            {
                var testUser = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(testUser, password);
            }
        }

        // Register standard OIDC scopes (required for scope validation in OpenIddict v7)
        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
        // Register standard OIDC scopes + custom 'api' scope (required for OpenIddict v7 scope validation)
        foreach (var scopeName in new[] { OpenIddictConstants.Scopes.Email, OpenIddictConstants.Scopes.Profile, "api" })
        {
            if (await scopeManager.FindByNameAsync(scopeName, ct) is null)
            {
                await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                {
                    Name = scopeName,
                    DisplayName = scopeName
                }, ct);
            }
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
                    OpenIddictConstants.Permissions.Endpoints.EndSession,
                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.ResponseTypes.Code,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "openid",
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "api"
                }
            }, ct);
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}

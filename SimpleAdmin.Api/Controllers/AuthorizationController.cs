using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;

namespace SimpleAdmin.Api.Controllers;

[ApiController]
public class AuthorizationController : ControllerBase
{
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict server request cannot be retrieved.");

        // Try to authenticate with the cookie scheme
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // If not authenticated, challenge the cookie scheme to redirect to login
        if (!result.Succeeded)
        {
            var redirectUri = Request.PathBase + Request.Path + QueryString.Create(Request.Query.ToList());
            return Challenge(
                new AuthenticationProperties { RedirectUri = redirectUri },
                CookieAuthenticationDefaults.AuthenticationScheme);
        }

        // Build a ClaimsIdentity from the authenticated cookie principal
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role);

        var cookiePrincipal = result.Principal!;

        identity.SetClaim(OpenIddictConstants.Claims.Subject,
            cookiePrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("Missing NameIdentifier claim."));

        var email = cookiePrincipal.FindFirstValue(ClaimTypes.Email);
        if (email is not null)
            identity.SetClaim(OpenIddictConstants.Claims.Email, email);

        var name = cookiePrincipal.FindFirstValue(ClaimTypes.Name);
        if (name is not null)
            identity.SetClaim(OpenIddictConstants.Claims.Name, name);

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/token")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict server request cannot be retrieved.");

        if (request.IsAuthorizationCodeGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            return SignIn(result.Principal!, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new InvalidOperationException("Unsupported grant type.");
    }
}

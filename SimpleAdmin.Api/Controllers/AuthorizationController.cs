using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
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

        // Authenticate using Identity's application scheme (IdentityConstants.ApplicationScheme = "Identity.Application"),
        // not CookieAuthenticationDefaults ("Cookies"). Identity sets the ".AspNetCore.Identity.Application" cookie;
        // "Cookies" sets ".AspNetCore.Cookies" — they are different schemes with different cookie names.
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

        // If not authenticated, redirect explicitly to the login page.
        // Using Challenge(Cookie) inside OpenIddict's passthrough context returns 401 instead of 302;
        // an explicit Redirect ensures a proper 302 that browsers and test clients can follow.
        if (!result.Succeeded)
        {
            var currentUrl = Request.PathBase + Request.Path + QueryString.Create(Request.Query.ToList());
            return Redirect($"/Account/Login?ReturnUrl={Uri.EscapeDataString(currentUrl)}");
        }

        // Build a ClaimsIdentity from the authenticated cookie principal
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role);

        var cookiePrincipal = result.Principal!;

        // Set Subject claim — required for all tokens
        identity.SetClaim(OpenIddictConstants.Claims.Subject,
            cookiePrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("Missing NameIdentifier claim."));

        // Set email claim. Identity includes ClaimTypes.Email (emailaddress) on the cookie principal.
        var email = cookiePrincipal.FindFirstValue(ClaimTypes.Email)
                    ?? cookiePrincipal.FindFirstValue(ClaimTypes.Name);
        if (email is not null)
            identity.SetClaim(OpenIddictConstants.Claims.Email, email);

        // Set name claim
        var name = cookiePrincipal.FindFirstValue(ClaimTypes.Name);
        if (name is not null)
            identity.SetClaim(OpenIddictConstants.Claims.Name, name);

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());

        // Set destinations for each claim so they appear in the access token.
        // In OpenIddict v7, claims without explicit destinations are dropped from tokens.
        identity.SetDestinations(claim => claim.Type switch
        {
            OpenIddictConstants.Claims.Subject => [OpenIddictConstants.Destinations.AccessToken],
            OpenIddictConstants.Claims.Email => [OpenIddictConstants.Destinations.AccessToken],
            OpenIddictConstants.Claims.Name => [OpenIddictConstants.Destinations.AccessToken],
            _ => [OpenIddictConstants.Destinations.AccessToken]
        });

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

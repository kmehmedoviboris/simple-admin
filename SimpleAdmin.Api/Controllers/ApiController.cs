using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using System.Security.Claims;

namespace SimpleAdmin.Api.Controllers;

[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public IActionResult Me()
    {
        var sub = User.FindFirstValue(OpenIddictConstants.Claims.Subject);
        var email = User.FindFirstValue(OpenIddictConstants.Claims.Email);
        return Ok(new { sub, email });
    }
}

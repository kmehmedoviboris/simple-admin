using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using SimpleAdmin.Api.Dtos;
using SimpleAdmin.Api.Models;

namespace SimpleAdmin.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    [ProducesResponseType<IEnumerable<UserListDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetAll()
    {
        var users = _userManager.Users
            .Select(u => new UserListDto(u.Id, u.Email!, u.EmailConfirmed))
            .ToList();
        return Ok(users);
    }

    [HttpPost]
    [ProducesResponseType<UserListDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return CreatedAtAction(nameof(GetAll), new UserListDto(user.Id, user.Email!, user.EmailConfirmed));
    }

    [HttpPut("{id}")]
    [ProducesResponseType<UserListDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        if (dto.Email is not null)
        {
            var emailResult = await _userManager.SetEmailAsync(user, dto.Email);
            if (!emailResult.Succeeded)
                return BadRequest(emailResult.Errors);

            var userNameResult = await _userManager.SetUserNameAsync(user, dto.Email);
            if (!userNameResult.Succeeded)
                return BadRequest(userNameResult.Errors);
        }

        if (dto.NewPassword is not null)
        {
            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
                return BadRequest(removeResult.Errors);

            var addResult = await _userManager.AddPasswordAsync(user, dto.NewPassword);
            if (!addResult.Succeeded)
                return BadRequest(addResult.Errors);
        }

        return Ok(new UserListDto(user.Id, user.Email!, user.EmailConfirmed));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        await _userManager.DeleteAsync(user);
        return NoContent();
    }
}

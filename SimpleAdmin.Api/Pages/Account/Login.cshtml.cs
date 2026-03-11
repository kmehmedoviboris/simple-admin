using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SimpleAdmin.Api.Models;

namespace SimpleAdmin.Api.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public LoginModel(SignInManager<ApplicationUser> signInManager)
        => _signInManager = signInManager;

    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }

    public void OnGet(string? returnUrl = null) => ReturnUrl = returnUrl;

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= ReturnUrl;
        var result = await _signInManager.PasswordSignInAsync(
            Email, Password, isPersistent: false, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            ErrorMessage = "Invalid email or password";
            ReturnUrl = returnUrl;
            return Page();
        }

        return Redirect(returnUrl ?? "/");
    }
}

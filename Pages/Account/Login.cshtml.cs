using System.ComponentModel.DataAnnotations;
using AcademyNet.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AcademyNet.Pages.Account;

public class LoginModel(SignInManager<AppUser> signIn, UserManager<AppUser> users) : PageModel
{
    [BindProperty, Required, EmailAddress] public string Email { get; set; } = "";
    [BindProperty, Required] public string Password { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = await users.FindByEmailAsync(Email.Trim());
        if (user == null) { ModelState.AddModelError("", "Wrong email or password."); return Page(); }

        var result = await signIn.PasswordSignInAsync(user, Password, isPersistent: true, lockoutOnFailure: true);
        if (result.IsLockedOut) { ModelState.AddModelError("", "Too many attempts. Try again in a few minutes."); return Page(); }
        if (!result.Succeeded) { ModelState.AddModelError("", "Wrong email or password."); return Page(); }

        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl)) return LocalRedirect(ReturnUrl);
        return Redirect(await users.IsInRoleAsync(user, Roles.Admin) ? "/Admin" : "/Learn");
    }
}

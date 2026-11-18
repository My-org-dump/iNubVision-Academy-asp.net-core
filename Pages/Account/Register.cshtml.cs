using System.ComponentModel.DataAnnotations;
using AcademyNet.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AcademyNet.Pages.Account;

public class RegisterModel(UserManager<AppUser> users, SignInManager<AppUser> signIn) : PageModel
{
    public class InputModel
    {
        [Required, StringLength(80)] public string FullName { get; set; } = "";
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required, Phone, StringLength(20)] public string Phone { get; set; } = "";
        [Required, StringLength(100, MinimumLength = 8)] public string Password { get; set; } = "";
    }

    [BindProperty] public InputModel Input { get; set; } = new();
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = new AppUser
        {
            UserName = Input.Email.Trim(), Email = Input.Email.Trim(),
            FullName = Input.FullName.Trim(), PhoneNumber = Input.Phone.Trim()
        };
        var result = await users.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return Page();
        }
        await users.AddToRoleAsync(user, Roles.Student);
        await signIn.SignInAsync(user, isPersistent: true);
        TempData["Flash"] = "Welcome! Your account is ready.";
        return LocalRedirect(!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/Courses");
    }
}

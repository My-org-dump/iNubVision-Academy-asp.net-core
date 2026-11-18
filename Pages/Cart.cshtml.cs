using System.Security.Claims;
using AcademyNet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AcademyNet.Pages;

public class CartModel(CartService cart) : PageModel
{
    public CartSummary Summary { get; private set; } = null!;

    public async Task OnGetAsync()
        => Summary = await cart.SummaryAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));

    public IActionResult OnPostAdd(int id, bool checkout = false)
    {
        cart.Add(id);
        if (checkout) return Redirect("/Checkout");
        TempData["Flash"] = "Added to cart.";
        return RedirectToPage();
    }

    public IActionResult OnPostRemove(int id)
    {
        cart.Remove(id);
        return RedirectToPage();
    }

    public IActionResult OnPostCoupon(string? code)
    {
        cart.SetCoupon(code);
        return RedirectToPage();
    }
}

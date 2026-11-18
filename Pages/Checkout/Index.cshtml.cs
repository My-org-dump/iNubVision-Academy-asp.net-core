using System.Security.Claims;
using AcademyNet.Data;
using AcademyNet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AcademyNet.Pages.Checkout;

public class IndexModel(CartService cart, AppDbContext db, OrderService orders) : PageModel
{
    public CartSummary Summary { get; private set; } = null!;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> OnGetAsync()
    {
        Summary = await cart.SummaryAsync(UserId);
        if (Summary.Lines.Count == 0) return Redirect("/Cart");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Summary = await cart.SummaryAsync(UserId);
        if (Summary.Lines.Count == 0) return Redirect("/Cart");

        var order = new Order
        {
            UserId = UserId,
            SubtotalInr = Summary.SubtotalInr,
            DiscountInr = Summary.DiscountInr,
            TotalInr = Summary.TotalInr,
            CouponCode = Summary.Coupon?.Code,
            Status = OrderStatus.Pending,
            Items = Summary.Lines.Select(l => new OrderItem { CourseId = l.Course.Id, PriceInr = l.PriceInr }).ToList()
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        // 100% coupon or only free courses: no payment needed
        if (order.TotalInr <= 0)
        {
            await orders.MarkPaidAsync(order, "FREE");
            cart.Clear();
            TempData["Flash"] = "You're enrolled. Happy learning!";
            return Redirect("/Learn");
        }
        return RedirectToPage("/Checkout/Pay", new { id = order.Id });
    }
}

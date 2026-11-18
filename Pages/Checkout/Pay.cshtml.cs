using System.Security.Claims;
using AcademyNet.Data;
using AcademyNet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AcademyNet.Pages.Checkout;

public class PayModel(AppDbContext db, RazorpayService rzp, OrderService orders, CartService cart, ILogger<PayModel> logger) : PageModel
{
    public Order Order { get; private set; } = null!;
    public AppUser Me { get; private set; } = null!;
    public bool DemoMode => !rzp.Options.IsConfigured;
    public string KeyId => rzp.Options.KeyId;
    public string? Problem { get; private set; }

    private async Task<bool> LoadAsync(int id)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var o = await db.Orders.Include(x => x.Items).ThenInclude(i => i.Course).FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
        if (o == null) return false;
        Order = o;
        Me = (await db.Users.FindAsync(uid))!;
        return true;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await LoadAsync(id)) return NotFound();
        if (Order.Status == OrderStatus.Paid) return Redirect("/Learn");

        if (!DemoMode && Order.RazorpayOrderId == null)
        {
            try
            {
                Order.RazorpayOrderId = await rzp.CreateOrderAsync(Order.TotalInr, $"order_{Order.Id}");
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Razorpay order creation failed for {Id}", id);
                Problem = "We couldn't start the payment. Please try again in a minute.";
            }
        }
        return Page();
    }

    /// <summary>Razorpay calls back here (via the browser) after a successful payment.</summary>
    public async Task<IActionResult> OnPostVerifyAsync(int id,
        [FromForm(Name = "razorpay_payment_id")] string paymentId,
        [FromForm(Name = "razorpay_order_id")] string rzpOrderId,
        [FromForm(Name = "razorpay_signature")] string signature)
    {
        if (!await LoadAsync(id)) return NotFound();
        if (Order.RazorpayOrderId != rzpOrderId || !rzp.VerifyPayment(rzpOrderId, paymentId, signature))
        {
            Order.Status = OrderStatus.Failed;
            await db.SaveChangesAsync();
            TempData["Error"] = "Payment could not be verified. If money was deducted, contact us with your payment ID: " + paymentId;
            return RedirectToPage(new { id });
        }
        await orders.MarkPaidAsync(Order, paymentId);
        cart.Clear();
        TempData["Flash"] = "Payment successful! Your courses are unlocked.";
        return Redirect("/Learn");
    }

    /// <summary>Only when Razorpay keys are not set: simulate a successful payment.</summary>
    public async Task<IActionResult> OnPostDemoAsync(int id)
    {
        if (!DemoMode) return BadRequest();
        if (!await LoadAsync(id)) return NotFound();
        await orders.MarkPaidAsync(Order, "DEMO-" + Guid.NewGuid().ToString("N")[..10]);
        cart.Clear();
        TempData["Flash"] = "Demo payment done. Courses unlocked. (Add Razorpay keys for real payments.)";
        return Redirect("/Learn");
    }
}

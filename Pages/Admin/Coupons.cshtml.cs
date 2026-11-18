using AcademyNet.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AcademyNet.Pages.Admin;

public class CouponsModel(AppDbContext db) : PageModel
{
    public List<Coupon> Coupons { get; private set; } = [];

    public async Task OnGetAsync() => Coupons = await db.Coupons.OrderBy(c => c.Code).ToListAsync();

    public async Task<IActionResult> OnPostCreateAsync(string code, string kind, int value, DateTime? validUntil, int? maxUses)
    {
        code = (code ?? "").Trim().ToUpperInvariant();
        if (code.Length < 3 || value <= 0) { TempData["Error"] = "Enter a code (3+ characters) and a value."; return RedirectToPage(); }
        if (await db.Coupons.AnyAsync(c => c.Code == code)) { TempData["Error"] = "That code already exists."; return RedirectToPage(); }
        if (kind == "percent" && value > 100) value = 100;

        db.Coupons.Add(new Coupon
        {
            Code = code,
            PercentOff = kind == "percent" ? value : null,
            AmountOffInr = kind == "flat" ? value : null,
            // valid until the end of the chosen day (IST)
            ValidUntilUtc = validUntil?.Date.AddDays(1).AddHours(-5.5),
            MaxUses = maxUses is > 0 ? maxUses : null,
            IsActive = true
        });
        await db.SaveChangesAsync();
        TempData["Flash"] = $"Coupon {code} created.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var c = await db.Coupons.FindAsync(id);
        if (c != null) { c.IsActive = !c.IsActive; await db.SaveChangesAsync(); }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var c = await db.Coupons.FindAsync(id);
        if (c != null) { db.Coupons.Remove(c); await db.SaveChangesAsync(); }
        return RedirectToPage();
    }
}

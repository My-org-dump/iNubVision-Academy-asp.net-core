using System.Text.Json;
using AcademyNet.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademyNet.Services;

public record CartLine(Course Course, int PriceInr);
public record CartSummary(List<CartLine> Lines, int SubtotalInr, int DiscountInr, int TotalInr, Coupon? Coupon, string? CouponError);

/// <summary>Session cart (course ids + coupon). Works for guests; login is needed only at checkout.</summary>
public class CartService(IHttpContextAccessor http, AppDbContext db)
{
    private const string Key = "cart";
    private record State(List<int> CourseIds, string? Coupon);

    private ISession Session => http.HttpContext!.Session;

    private State Load()
    {
        var json = Session.GetString(Key);
        return json == null ? new State([], null) : JsonSerializer.Deserialize<State>(json) ?? new State([], null);
    }

    private void Save(State s) => Session.SetString(Key, JsonSerializer.Serialize(s));

    public int Count => Load().CourseIds.Count;

    public void Add(int courseId)
    {
        var s = Load();
        if (!s.CourseIds.Contains(courseId)) s.CourseIds.Add(courseId);
        Save(s);
    }

    public void Remove(int courseId)
    {
        var s = Load();
        s.CourseIds.Remove(courseId);
        Save(s);
    }

    public void SetCoupon(string? code) => Save(Load() with { Coupon = string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant() });

    public void Clear() => Session.Remove(Key);

    /// <summary>Prices the cart. Courses the user already owns are left out.</summary>
    public async Task<CartSummary> SummaryAsync(string? userId = null)
    {
        var s = Load();
        var courses = await db.Courses.Where(c => s.CourseIds.Contains(c.Id) && c.IsPublished).OrderBy(c => c.SortOrder).ToListAsync();
        if (userId != null)
        {
            var owned = await db.Enrollments.Where(e => e.UserId == userId).Select(e => e.CourseId).ToListAsync();
            courses = courses.Where(c => !owned.Contains(c.Id)).ToList();
        }

        var lines = courses.Select(c => new CartLine(c, c.PriceInr)).ToList();
        var subtotal = lines.Sum(l => l.PriceInr);

        Coupon? coupon = null; string? error = null; var discount = 0;
        if (s.Coupon != null && subtotal > 0)
        {
            coupon = await db.Coupons.FirstOrDefaultAsync(c => c.Code == s.Coupon);
            error = Pricing.Validate(coupon);
            if (error == null) discount = Pricing.Discount(coupon!, subtotal);
            else coupon = null;
        }
        return new CartSummary(lines, subtotal, discount, subtotal - discount, coupon, error);
    }
}

public static class Pricing
{
    public static string? Validate(Coupon? c)
    {
        if (c == null || !c.IsActive) return "This coupon code is not valid.";
        if (c.ValidUntilUtc != null && c.ValidUntilUtc < DateTime.UtcNow) return "This coupon has expired.";
        if (c.MaxUses != null && c.UsedCount >= c.MaxUses) return "This coupon has been fully used.";
        return null;
    }

    public static int Discount(Coupon c, int subtotal)
    {
        var d = c.PercentOff is > 0 ? (int)Math.Round(subtotal * c.PercentOff.Value / 100.0) : c.AmountOffInr ?? 0;
        return Math.Clamp(d, 0, subtotal);
    }
}

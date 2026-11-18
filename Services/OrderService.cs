using AcademyNet.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademyNet.Services;

/// <summary>Turns a paid order into course access. Safe to call twice (browser callback + webhook).</summary>
public class OrderService(AppDbContext db)
{
    public async Task MarkPaidAsync(Order order, string? paymentId)
    {
        if (order.Status == OrderStatus.Paid) return;

        order.Status = OrderStatus.Paid;
        order.RazorpayPaymentId = paymentId ?? order.RazorpayPaymentId;
        order.PaidUtc = DateTime.UtcNow;

        var items = await db.OrderItems.Where(i => i.OrderId == order.Id).ToListAsync();
        foreach (var i in items) await EnrollAsync(order.UserId, i.CourseId, "purchase");

        if (order.CouponCode != null)
        {
            var coupon = await db.Coupons.FirstOrDefaultAsync(c => c.Code == order.CouponCode);
            if (coupon != null) coupon.UsedCount++;
        }
        await db.SaveChangesAsync();
    }

    public async Task EnrollAsync(string userId, int courseId, string source)
    {
        var exists = await db.Enrollments.AnyAsync(e => e.UserId == userId && e.CourseId == courseId)
                     || db.Enrollments.Local.Any(e => e.UserId == userId && e.CourseId == courseId);
        if (!exists) db.Enrollments.Add(new Enrollment { UserId = userId, CourseId = courseId, Source = source });
    }
}

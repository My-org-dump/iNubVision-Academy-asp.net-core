using AcademyNet.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AcademyNet.Pages.Admin;

public class IndexModel(AppDbContext db) : PageModel
{
    public int Revenue, PaidOrders, Students, Enrollments, NewMessages;
    public List<Order> Recent { get; private set; } = [];
    public bool RazorpayLive { get; private set; }

    public async Task OnGetAsync([Microsoft.AspNetCore.Mvc.FromServices] Services.RazorpayService rzp)
    {
        // Sum on the client: SQLite can't SUM some types server-side reliably across providers
        var paid = await db.Orders.Where(o => o.Status == OrderStatus.Paid).Select(o => o.TotalInr).ToListAsync();
        Revenue = paid.Sum();
        PaidOrders = paid.Count;
        Students = await db.Users.CountAsync();
        Enrollments = await db.Enrollments.CountAsync();
        NewMessages = await db.ContactMessages.CountAsync(m => !m.IsHandled);
        Recent = await db.Orders.Include(o => o.User).Include(o => o.Items).ThenInclude(i => i.Course)
            .OrderByDescending(o => o.CreatedUtc).Take(10).ToListAsync();
        RazorpayLive = rzp.Options.IsConfigured;
    }
}

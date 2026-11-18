using AcademyNet.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AcademyNet.Pages.Admin;

public class OrdersModel(AppDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }
    public List<Order> Orders { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var q = db.Orders.Include(o => o.User).Include(o => o.Items).ThenInclude(i => i.Course).AsQueryable();
        if (Enum.TryParse<OrderStatus>(Status, true, out var s)) q = q.Where(o => o.Status == s);
        Orders = await q.OrderByDescending(o => o.CreatedUtc).Take(500).ToListAsync();
    }
}

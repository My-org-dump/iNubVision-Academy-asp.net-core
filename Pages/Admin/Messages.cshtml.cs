using AcademyNet.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AcademyNet.Pages.Admin;

public class MessagesModel(AppDbContext db) : PageModel
{
    public List<ContactMessage> Messages { get; private set; } = [];

    public async Task OnGetAsync()
        => Messages = await db.ContactMessages.OrderBy(m => m.IsHandled).ThenByDescending(m => m.CreatedUtc).Take(500).ToListAsync();

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var m = await db.ContactMessages.FindAsync(id);
        if (m != null) { m.IsHandled = !m.IsHandled; await db.SaveChangesAsync(); }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var m = await db.ContactMessages.FindAsync(id);
        if (m != null) { db.ContactMessages.Remove(m); await db.SaveChangesAsync(); }
        return RedirectToPage();
    }
}

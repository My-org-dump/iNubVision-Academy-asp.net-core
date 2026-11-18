using AcademyNet.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AcademyNet.Pages.Admin;

/// <summary>Add real student reviews and placement stories (with the student's permission).</summary>
public class TestimonialsModel(AppDbContext db) : PageModel
{
    public List<Testimonial> Items { get; private set; } = [];

    public async Task OnGetAsync() => Items = await db.Testimonials.OrderBy(t => t.SortOrder).ToListAsync();

    public async Task<IActionResult> OnPostCreateAsync(string name, string? headline, string text, int rating, bool isSuccessStory)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(text)) { TempData["Error"] = "Name and text are required."; return RedirectToPage(); }
        db.Testimonials.Add(new Testimonial
        {
            Name = name.Trim(), Headline = headline?.Trim(), Text = text.Trim(),
            Rating = Math.Clamp(rating, 1, 5), IsSuccessStory = isSuccessStory,
            SortOrder = (await db.Testimonials.MaxAsync(t => (int?)t.SortOrder) ?? 0) + 1
        });
        await db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var t = await db.Testimonials.FindAsync(id);
        if (t != null) { t.IsPublished = !t.IsPublished; await db.SaveChangesAsync(); }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var t = await db.Testimonials.FindAsync(id);
        if (t != null) { db.Testimonials.Remove(t); await db.SaveChangesAsync(); }
        return RedirectToPage();
    }
}

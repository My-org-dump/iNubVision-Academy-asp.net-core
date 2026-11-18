using System.Security.Claims;
using AcademyNet.Data;
using AcademyNet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AcademyNet.Pages.Courses;

public class DetailsModel(AppDbContext db, OrderService orders) : PageModel
{
    public Course Course { get; private set; } = null!;
    public bool Owned { get; private set; }
    public int LessonCount { get; private set; }

    private async Task<bool> LoadAsync(string slug)
    {
        var c = await db.Courses
            .Include(x => x.Modules.OrderBy(m => m.SortOrder)).ThenInclude(m => m.Lessons.OrderBy(l => l.SortOrder))
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug && (x.IsPublished || User.IsInRole(Roles.Admin)));
        if (c == null) return false;
        Course = c;
        LessonCount = c.Modules.Sum(m => m.Lessons.Count);
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Owned = uid != null && await db.Enrollments.AnyAsync(e => e.UserId == uid && e.CourseId == c.Id);
        return true;
    }

    public async Task<IActionResult> OnGetAsync(string slug) => await LoadAsync(slug) ? Page() : NotFound();

    /// <summary>Free courses: one click to enroll (login required).</summary>
    public async Task<IActionResult> OnPostEnrollFreeAsync(string slug)
    {
        if (User.Identity?.IsAuthenticated != true) return Redirect($"/Account/Login?returnUrl=/Courses/{slug}");
        if (!await LoadAsync(slug)) return NotFound();
        if (!Course.IsFree) return RedirectToPage(new { slug });

        await orders.EnrollAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, Course.Id, "free");
        await db.SaveChangesAsync();
        TempData["Flash"] = "You're enrolled. Happy learning!";
        return Redirect($"/Learn/{slug}");
    }
}

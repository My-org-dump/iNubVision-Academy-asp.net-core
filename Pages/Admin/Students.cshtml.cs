using AcademyNet.Data;
using System.Security.Claims;
using AcademyNet.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AcademyNet.Pages.Admin;

public class StudentsModel(AppDbContext db, OrderService orders, UserManager<AppUser> userManager) : PageModel
{
    public record Row(AppUser User, List<Enrollment> Enrollments);
    public List<Row> Rows { get; private set; } = [];
    public List<Course> AllCourses { get; private set; } = [];
    public HashSet<string> AdminIds { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public string? Q { get; set; }

    public async Task OnGetAsync()
    {
        var users = db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(Q))
        {
            var q = Q.Trim();
            users = users.Where(u => u.FullName.Contains(q) || u.Email!.Contains(q) || (u.PhoneNumber != null && u.PhoneNumber.Contains(q)));
        }
        var list = await users.OrderByDescending(u => u.CreatedUtc).Take(300).ToListAsync();
        var ids = list.Select(u => u.Id).ToList();
        var enrollments = await db.Enrollments.Include(e => e.Course).Where(e => ids.Contains(e.UserId)).ToListAsync();
        Rows = list.Select(u => new Row(u, enrollments.Where(e => e.UserId == u.Id).ToList())).ToList();
        AllCourses = await db.Courses.OrderBy(c => c.SortOrder).ToListAsync();
        AdminIds = (await userManager.GetUsersInRoleAsync(Roles.Admin)).Select(u => u.Id).ToHashSet();
    }

    /// <summary>Make another account an admin (e.g. your personal email) or remove admin rights.</summary>
    public async Task<IActionResult> OnPostRoleAsync(string userId, bool makeAdmin)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();
        if (!makeAdmin && user.Id == User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            TempData["Error"] = "You can't remove your own admin rights. Ask another admin to do it.";
            return RedirectToPage(new { Q });
        }
        if (makeAdmin)
        {
            if (!await userManager.IsInRoleAsync(user, Roles.Admin)) await userManager.AddToRoleAsync(user, Roles.Admin);
        }
        else if (await userManager.IsInRoleAsync(user, Roles.Admin))
        {
            await userManager.RemoveFromRoleAsync(user, Roles.Admin);
            if (!await userManager.IsInRoleAsync(user, Roles.Student)) await userManager.AddToRoleAsync(user, Roles.Student);
        }
        await userManager.UpdateSecurityStampAsync(user); // forces a fresh login so the change applies
        TempData["Flash"] = makeAdmin ? $"{user.Email} is now an admin. They need to log in again." : $"{user.Email} is no longer an admin.";
        return RedirectToPage(new { Q });
    }

    /// <summary>Give access manually (e.g. paid by bank transfer / offline).</summary>
    public async Task<IActionResult> OnPostGrantAsync(string userId, int courseId)
    {
        if (await db.Users.AnyAsync(u => u.Id == userId) && await db.Courses.AnyAsync(c => c.Id == courseId))
        {
            await orders.EnrollAsync(userId, courseId, "admin");
            await db.SaveChangesAsync();
            TempData["Flash"] = "Access granted.";
        }
        return RedirectToPage(new { Q });
    }

    public async Task<IActionResult> OnPostRevokeAsync(string userId, int courseId)
    {
        var e = await db.Enrollments.FindAsync(userId, courseId);
        if (e != null) { db.Enrollments.Remove(e); await db.SaveChangesAsync(); TempData["Flash"] = "Access removed."; }
        return RedirectToPage(new { Q });
    }
}

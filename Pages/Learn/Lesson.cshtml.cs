using System.Security.Claims;
using AcademyNet.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AcademyNet.Pages.Learn;

/// <summary>Course player. Enrolled students see everything; anyone can open preview lessons.</summary>
public class LessonModel(AppDbContext db) : PageModel
{
    public Course Course { get; private set; } = null!;
    public Lesson? Current { get; private set; }
    public Lesson? Next { get; private set; }
    public HashSet<int> Done { get; private set; } = [];
    public bool Enrolled { get; private set; }
    public int Total { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug, int? lessonId)
    {
        var c = await db.Courses
            .Include(x => x.Modules.OrderBy(m => m.SortOrder)).ThenInclude(m => m.Lessons.OrderBy(l => l.SortOrder))
            .AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug);
        if (c == null) return NotFound();
        Course = c;

        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Enrolled = User.IsInRole(Roles.Admin) ||
                   (uid != null && await db.Enrollments.AnyAsync(e => e.UserId == uid && e.CourseId == c.Id));

        var all = c.Modules.SelectMany(m => m.Lessons).ToList();
        Total = all.Count;
        if (uid != null)
        {
            var ids = all.Select(l => l.Id).ToList();
            Done = (await db.LessonProgress.Where(p => p.UserId == uid && ids.Contains(p.LessonId)).Select(p => p.LessonId).ToListAsync()).ToHashSet();
        }

        Current = lessonId == null
            ? all.FirstOrDefault(l => !Done.Contains(l.Id)) ?? all.FirstOrDefault()
            : all.FirstOrDefault(l => l.Id == lessonId);
        if (lessonId != null && Current == null) return NotFound();

        if (Current != null && !Enrolled && !Current.IsPreview)
        {
            if (uid == null) return Redirect($"/Account/Login?returnUrl=/Learn/{slug}");
            TempData["Error"] = "Enroll in this course to watch all lessons.";
            return Redirect($"/Courses/{slug}");
        }
        if (Current == null && !Enrolled) return Redirect($"/Courses/{slug}");

        if (Current != null)
        {
            var idx = all.FindIndex(l => l.Id == Current.Id);
            Next = idx + 1 < all.Count ? all[idx + 1] : null;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostCompleteAsync(string slug, int lessonId)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid == null) return Redirect($"/Account/Login?returnUrl=/Learn/{slug}");

        var lesson = await db.Lessons.Include(l => l.Module).ThenInclude(m => m!.Course).FirstOrDefaultAsync(l => l.Id == lessonId);
        if (lesson == null || lesson.Module!.Course!.Slug != slug) return NotFound();
        var enrolled = await db.Enrollments.AnyAsync(e => e.UserId == uid && e.CourseId == lesson.Module.CourseId);
        if (!enrolled && !User.IsInRole(Roles.Admin)) return Forbid();

        if (!await db.LessonProgress.AnyAsync(p => p.UserId == uid && p.LessonId == lessonId))
        {
            db.LessonProgress.Add(new LessonProgress { UserId = uid, LessonId = lessonId });
            await db.SaveChangesAsync();
        }

        var next = await db.Lessons.Where(l => l.Module!.CourseId == lesson.Module.CourseId)
            .OrderBy(l => l.Module!.SortOrder).ThenBy(l => l.SortOrder)
            .Select(l => l.Id).ToListAsync();
        var i = next.IndexOf(lessonId);
        return Redirect(i >= 0 && i + 1 < next.Count ? $"/Learn/{slug}/{next[i + 1]}" : $"/Learn/{slug}/{lessonId}");
    }
}

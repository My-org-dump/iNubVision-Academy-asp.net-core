using System.Security.Claims;
using AcademyNet.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AcademyNet.Pages.Learn;

public class IndexModel(AppDbContext db) : PageModel
{
    public record Row(Course Course, int Lessons, int Done);
    public List<Row> Rows { get; private set; } = [];
    public string Name { get; private set; } = "";

    public async Task OnGetAsync()
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Name = (await db.Users.FindAsync(uid))?.FullName ?? "";

        var courses = await db.Enrollments.Where(e => e.UserId == uid)
            .OrderByDescending(e => e.CreatedUtc).Select(e => e.Course!).ToListAsync();
        var done = await db.LessonProgress.Where(p => p.UserId == uid).Select(p => p.LessonId).ToListAsync();

        foreach (var c in courses)
        {
            var lessonIds = await db.Lessons.Where(l => l.Module!.CourseId == c.Id).Select(l => l.Id).ToListAsync();
            Rows.Add(new Row(c, lessonIds.Count, lessonIds.Count(done.Contains)));
        }
    }
}

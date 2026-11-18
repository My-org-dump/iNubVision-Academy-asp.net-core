using AcademyNet.Data;
using AcademyNet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AcademyNet.Pages.Admin;

public class CoursesModel(AppDbContext db) : PageModel
{
    public record Row(Course Course, int Lessons, int Students);
    public List<Row> Rows { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var courses = await db.Courses.OrderBy(c => c.SortOrder).ToListAsync();
        foreach (var c in courses)
            Rows.Add(new Row(c,
                await db.Lessons.CountAsync(l => l.Module!.CourseId == c.Id),
                await db.Enrollments.CountAsync(e => e.CourseId == c.Id)));
    }

    public async Task<IActionResult> OnPostCreateAsync(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return RedirectToPage();
        var slug = Helpers.Slugify(title);
        var n = 2;
        while (await db.Courses.AnyAsync(c => c.Slug == slug)) slug = $"{Helpers.Slugify(title)}-{n++}";

        var course = new Course
        {
            Title = title.Trim(), Slug = slug, IsPublished = false,
            SortOrder = (await db.Courses.MaxAsync(c => (int?)c.SortOrder) ?? 0) + 1,
            Modules = [new CourseModule { Title = "Module 1", SortOrder = 1 }]
        };
        db.Courses.Add(course);
        await db.SaveChangesAsync();
        TempData["Flash"] = "Course created as a draft. Fill in the details and publish it.";
        return RedirectToPage("/Admin/Course", new { id = course.Id });
    }
}

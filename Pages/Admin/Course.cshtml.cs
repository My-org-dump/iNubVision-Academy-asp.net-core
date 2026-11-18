using AcademyNet.Data;
using AcademyNet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AcademyNet.Pages.Admin;

public class CourseModel(AppDbContext db, FileStorage files) : PageModel
{
    public Course Course { get; private set; } = null!;

    public class CourseInput
    {
        public string Title { get; set; } = "";
        public string Slug { get; set; } = "";
        public string? Subtitle { get; set; }
        public string? Description { get; set; }
        public string? Highlights { get; set; }
        public int PriceInr { get; set; }
        public int MrpInr { get; set; }
        public CourseMode Mode { get; set; }
        public string? Badge { get; set; }
        public DateTime? StartDate { get; set; }
        public string? Schedule { get; set; }
        public int Hours { get; set; }
        public int Projects { get; set; }
        public int SortOrder { get; set; }
        public bool IsPublished { get; set; }
    }

    [BindProperty] public CourseInput Input { get; set; } = new();

    private async Task<bool> LoadAsync(int id)
    {
        var c = await db.Courses
            .Include(x => x.Modules.OrderBy(m => m.SortOrder)).ThenInclude(m => m.Lessons.OrderBy(l => l.SortOrder))
            .FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return false;
        Course = c;
        return true;
    }

    private IActionResult Back(int id, string? msg = null)
    {
        if (msg != null) TempData["Flash"] = msg;
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await LoadAsync(id)) return NotFound();
        var c = Course;
        Input = new CourseInput
        {
            Title = c.Title, Slug = c.Slug, Subtitle = c.Subtitle, Description = c.Description, Highlights = c.Highlights,
            PriceInr = c.PriceInr, MrpInr = c.MrpInr, Mode = c.Mode, Badge = c.Badge, StartDate = c.StartDate,
            Schedule = c.Schedule, Hours = c.Hours, Projects = c.Projects, SortOrder = c.SortOrder, IsPublished = c.IsPublished
        };
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(int id)
    {
        if (!await LoadAsync(id)) return NotFound();
        var slug = Helpers.Slugify(string.IsNullOrWhiteSpace(Input.Slug) ? Input.Title : Input.Slug);
        if (string.IsNullOrWhiteSpace(Input.Title) || slug.Length == 0)
        {
            TempData["Error"] = "Title is required.";
            return Back(id);
        }
        if (await db.Courses.AnyAsync(x => x.Slug == slug && x.Id != id))
        {
            TempData["Error"] = "Another course already uses that URL slug.";
            return Back(id);
        }
        var c = Course;
        c.Title = Input.Title.Trim(); c.Slug = slug; c.Subtitle = Input.Subtitle; c.Description = Input.Description;
        c.Highlights = Input.Highlights; c.PriceInr = Math.Max(0, Input.PriceInr); c.MrpInr = Math.Max(0, Input.MrpInr);
        c.Mode = Input.Mode; c.Badge = string.IsNullOrWhiteSpace(Input.Badge) ? null : Input.Badge.Trim();
        c.StartDate = Input.StartDate; c.Schedule = Input.Schedule; c.Hours = Input.Hours; c.Projects = Input.Projects;
        c.SortOrder = Input.SortOrder; c.IsPublished = Input.IsPublished;
        if (c.Mode == CourseMode.Free) c.PriceInr = 0;
        await db.SaveChangesAsync();
        return Back(id, "Course saved.");
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (!await LoadAsync(id)) return NotFound();
        if (await db.OrderItems.AnyAsync(i => i.CourseId == id))
        {
            TempData["Error"] = "This course has orders, so it can't be deleted. Unpublish it instead.";
            return Back(id);
        }
        foreach (var l in Course.Modules.SelectMany(m => m.Lessons)) files.Delete(l.PdfStoredName);
        db.Courses.Remove(Course);
        await db.SaveChangesAsync();
        TempData["Flash"] = "Course deleted.";
        return RedirectToPage("/Admin/Courses");
    }

    // ----- Modules -----
    public async Task<IActionResult> OnPostAddModuleAsync(int id, string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return Back(id);
        var order = (await db.Modules.Where(m => m.CourseId == id).MaxAsync(m => (int?)m.SortOrder) ?? 0) + 1;
        db.Modules.Add(new CourseModule { CourseId = id, Title = title.Trim(), SortOrder = order });
        await db.SaveChangesAsync();
        return Back(id, "Module added.");
    }

    public async Task<IActionResult> OnPostEditModuleAsync(int id, int moduleId, string title, int sortOrder)
    {
        var m = await db.Modules.FirstOrDefaultAsync(x => x.Id == moduleId && x.CourseId == id);
        if (m == null) return Back(id);
        if (!string.IsNullOrWhiteSpace(title)) m.Title = title.Trim();
        m.SortOrder = sortOrder;
        await db.SaveChangesAsync();
        return Back(id, "Module updated.");
    }

    public async Task<IActionResult> OnPostDeleteModuleAsync(int id, int moduleId)
    {
        var m = await db.Modules.Include(x => x.Lessons).FirstOrDefaultAsync(x => x.Id == moduleId && x.CourseId == id);
        if (m == null) return Back(id);
        foreach (var l in m.Lessons) files.Delete(l.PdfStoredName);
        db.Modules.Remove(m);
        await db.SaveChangesAsync();
        return Back(id, "Module deleted.");
    }

    // ----- Lessons -----
    public async Task<IActionResult> OnPostSaveLessonAsync(int id, int moduleId, int? lessonId, string title, string? videoUrl,
        int durationMinutes, bool isPreview, int sortOrder, IFormFile? pdf, bool removePdf)
    {
        if (!await db.Modules.AnyAsync(m => m.Id == moduleId && m.CourseId == id)) return NotFound();
        if (string.IsNullOrWhiteSpace(title)) { TempData["Error"] = "Lesson title is required."; return Back(id); }

        Lesson? lesson;
        if (lessonId is > 0)
        {
            lesson = await db.Lessons.FirstOrDefaultAsync(l => l.Id == lessonId && l.Module!.CourseId == id);
            if (lesson == null) return NotFound();
        }
        else
        {
            lesson = new Lesson { ModuleId = moduleId };
            db.Lessons.Add(lesson);
            if (sortOrder <= 0) sortOrder = (await db.Lessons.Where(l => l.ModuleId == moduleId).MaxAsync(l => (int?)l.SortOrder) ?? 0) + 1;
        }

        lesson.Title = title.Trim();
        lesson.VideoUrl = string.IsNullOrWhiteSpace(videoUrl) ? null : videoUrl.Trim();
        lesson.DurationMinutes = Math.Max(0, durationMinutes);
        lesson.IsPreview = isPreview;
        lesson.SortOrder = sortOrder;
        lesson.ModuleId = moduleId;

        try
        {
            if (removePdf || pdf is { Length: > 0 })
            {
                files.Delete(lesson.PdfStoredName);
                lesson.PdfStoredName = null; lesson.PdfOriginalName = null;
            }
            if (pdf is { Length: > 0 })
            {
                lesson.PdfStoredName = await files.SaveAsync(pdf);
                lesson.PdfOriginalName = Path.GetFileName(pdf.FileName);
            }
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; return Back(id); }

        await db.SaveChangesAsync();
        return Back(id, "Lesson saved.");
    }

    public async Task<IActionResult> OnPostDeleteLessonAsync(int id, int lessonId)
    {
        var l = await db.Lessons.FirstOrDefaultAsync(x => x.Id == lessonId && x.Module!.CourseId == id);
        if (l == null) return Back(id);
        files.Delete(l.PdfStoredName);
        db.LessonProgress.RemoveRange(db.LessonProgress.Where(p => p.LessonId == lessonId));
        db.Lessons.Remove(l);
        await db.SaveChangesAsync();
        return Back(id, "Lesson deleted.");
    }
}

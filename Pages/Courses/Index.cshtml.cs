using AcademyNet.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AcademyNet.Pages.Courses;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<Course> Courses { get; private set; } = [];

    public async Task OnGetAsync()
        => Courses = await db.Courses.Where(c => c.IsPublished).OrderBy(c => c.SortOrder).ToListAsync();
}

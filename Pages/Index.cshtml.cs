using System.ComponentModel.DataAnnotations;
using AcademyNet.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AcademyNet.Pages;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<Course> Courses { get; private set; } = [];
    public List<Testimonial> Stories { get; private set; } = [];
    public List<Testimonial> Reviews { get; private set; } = [];

    public class ContactInput
    {
        [Required, StringLength(80)] public string Name { get; set; } = "";
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Phone, StringLength(20)] public string? Phone { get; set; }
        [Required, StringLength(2000, MinimumLength = 5)] public string Message { get; set; } = "";
    }

    [BindProperty] public ContactInput Contact { get; set; } = new();

    public async Task OnGetAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        Courses = await db.Courses.Where(c => c.IsPublished).OrderBy(c => c.SortOrder).ToListAsync();
        var t = await db.Testimonials.Where(x => x.IsPublished).OrderBy(x => x.SortOrder).ToListAsync();
        Stories = t.Where(x => x.IsSuccessStory).ToList();
        Reviews = t.Where(x => !x.IsSuccessStory).ToList();
    }

    public async Task<IActionResult> OnPostContactAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            TempData["Error"] = "Please fill your name, a valid email and a message.";
            return Page();
        }
        db.ContactMessages.Add(new ContactMessage
        {
            Name = Contact.Name.Trim(), Email = Contact.Email.Trim(),
            Phone = Contact.Phone?.Trim(), Message = Contact.Message.Trim()
        });
        await db.SaveChangesAsync();
        TempData["Flash"] = "Thanks! We'll get back to you within one working day.";
        return Redirect("/#contact");
    }
}

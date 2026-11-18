using Microsoft.AspNetCore.Identity;

namespace AcademyNet.Data;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Student = "Student";
}

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public enum CourseMode { Live = 0, SelfPaced = 1, Free = 2 }

public class Course
{
    public int Id { get; set; }
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    /// <summary>One highlight per line, shown as ticks on the course card.</summary>
    public string? Highlights { get; set; }
    public int PriceInr { get; set; }
    public int MrpInr { get; set; }
    public CourseMode Mode { get; set; }
    public string? Badge { get; set; }
    public DateTime? StartDate { get; set; }
    public string? Schedule { get; set; }
    public int Hours { get; set; }
    public int Projects { get; set; }
    public bool IsPublished { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public List<CourseModule> Modules { get; set; } = [];

    public bool IsFree => PriceInr <= 0;
    public int DiscountPercent => MrpInr > PriceInr && MrpInr > 0 ? (int)Math.Round(100.0 * (MrpInr - PriceInr) / MrpInr) : 0;
    public IEnumerable<string> HighlightList => (Highlights ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public class CourseModule
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    public string Title { get; set; } = "";
    public int SortOrder { get; set; }
    public List<Lesson> Lessons { get; set; } = [];
}

public class Lesson
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public CourseModule? Module { get; set; }
    public string Title { get; set; } = "";
    /// <summary>YouTube (unlisted), Vimeo, Cloudflare Stream iframe URL or a direct .mp4 URL.</summary>
    public string? VideoUrl { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsPreview { get; set; }
    public string? PdfStoredName { get; set; }
    public string? PdfOriginalName { get; set; }
    public int SortOrder { get; set; }
}

public class Coupon
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public int? PercentOff { get; set; }
    public int? AmountOffInr { get; set; }
    public DateTime? ValidUntilUtc { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum OrderStatus { Pending = 0, Paid = 1, Failed = 2 }

public class Order
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public AppUser? User { get; set; }
    public int SubtotalInr { get; set; }
    public int DiscountInr { get; set; }
    public int TotalInr { get; set; }
    public string? CouponCode { get; set; }
    public OrderStatus Status { get; set; }
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PaidUtc { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    public int PriceInr { get; set; }
}

public class Enrollment
{
    public string UserId { get; set; } = "";
    public AppUser? User { get; set; }
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    public string Source { get; set; } = "purchase"; // purchase | free | admin
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public class LessonProgress
{
    public string UserId { get; set; } = "";
    public int LessonId { get; set; }
    public DateTime CompletedUtc { get; set; } = DateTime.UtcNow;
}

public class ContactMessage
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string Message { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public bool IsHandled { get; set; }
}

/// <summary>Real student reviews / success stories, added by the admin.</summary>
public class Testimonial
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Headline { get; set; }   // e.g. "DevOps Engineer at Acme · 18 LPA"
    public string Text { get; set; } = "";
    public int Rating { get; set; } = 5;
    public bool IsSuccessStory { get; set; }
    public bool IsPublished { get; set; } = true;
    public int SortOrder { get; set; }
}

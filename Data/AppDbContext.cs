using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AcademyNet.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseModule> Modules => Set<CourseModule>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<LessonProgress> LessonProgress => Set<LessonProgress>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Course>().HasIndex(c => c.Slug).IsUnique();
        b.Entity<Course>().Property(c => c.Slug).HasMaxLength(120);
        b.Entity<Course>().Property(c => c.Title).HasMaxLength(200);

        b.Entity<CourseModule>().HasOne(m => m.Course).WithMany(c => c.Modules)
            .HasForeignKey(m => m.CourseId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Lesson>().HasOne(l => l.Module).WithMany(m => m.Lessons)
            .HasForeignKey(l => l.ModuleId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<Coupon>().HasIndex(c => c.Code).IsUnique();
        b.Entity<Coupon>().Property(c => c.Code).HasMaxLength(40);

        b.Entity<Order>().HasOne(o => o.User).WithMany().HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Order>().HasIndex(o => o.RazorpayOrderId);
        b.Entity<OrderItem>().HasOne<Order>().WithMany(o => o.Items).HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<OrderItem>().HasOne(i => i.Course).WithMany().HasForeignKey(i => i.CourseId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Enrollment>().HasKey(e => new { e.UserId, e.CourseId });
        b.Entity<Enrollment>().HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Enrollment>().HasOne(e => e.Course).WithMany().HasForeignKey(e => e.CourseId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<LessonProgress>().HasKey(p => new { p.UserId, p.LessonId });
    }
}

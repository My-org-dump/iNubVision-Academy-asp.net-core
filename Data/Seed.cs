using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcademyNet.Data;

public static class Seed
{
    public static async Task RunAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var r in new[] { Roles.Admin, Roles.Student })
            if (!await roles.RoleExistsAsync(r)) await roles.CreateAsync(new IdentityRole(r));

        var email = config["Admin:Email"];
        var password = config["Admin:Password"];
        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var admin = await users.FindByEmailAsync(email);
            if (admin == null)
            {
                admin = new AppUser { UserName = email, Email = email, EmailConfirmed = true, FullName = config["Admin:Name"] ?? "Admin" };
                var res = await users.CreateAsync(admin, password);
                if (!res.Succeeded) logger.LogError("Admin not created: {E}", string.Join("; ", res.Errors.Select(e => e.Description)));
            }
            if (await users.FindByIdAsync(admin.Id) != null && !await users.IsInRoleAsync(admin, Roles.Admin))
                await users.AddToRoleAsync(admin, Roles.Admin);
        }

        if (await db.Courses.AnyAsync()) return;

        // ---- Starter catalogue (edit everything from the admin panel) ----
        db.Courses.AddRange(
            new Course
            {
                Slug = "devops-cloud-ai-live-batch-1",
                Title = "DevOps + Cloud + AI — Live Bootcamp (Batch 1)",
                Subtitle = "Become a production-ready DevOps engineer with weekend live classes and real projects.",
                Description = "A complete, hands-on journey from Linux and Git to Kubernetes, Terraform, DevSecOps, observability and AI for DevOps.\n\nEvery class is live and recorded, so you can revise any time. You build real CI/CD pipelines and deploy to Azure and AWS, just like in a production team.",
                Highlights = "Weekend live classes + recordings\n40+ hands-on projects\nAzure + AWS + Kubernetes\nResume review & mock interviews\nCertificate of completion",
                PriceInr = 9999, MrpInr = 25000, Mode = CourseMode.Live, Badge = "Most popular",
                StartDate = DateTime.UtcNow.Date.AddDays(14), Schedule = "Sat & Sun · 8:00–10:30 PM IST",
                Hours = 250, Projects = 40, SortOrder = 1,
                Modules =
                [
                    new CourseModule { Title = "Phase 1 — Foundations: Linux, Networking, Git", SortOrder = 1, Lessons =
                    [
                        new Lesson { Title = "Welcome & how this bootcamp works", DurationMinutes = 15, IsPreview = true, SortOrder = 1 },
                        new Lesson { Title = "Linux essentials for DevOps", DurationMinutes = 90, SortOrder = 2 },
                        new Lesson { Title = "Git & branching strategies", DurationMinutes = 75, SortOrder = 3 },
                    ]},
                    new CourseModule { Title = "Phase 2 — CI/CD: Jenkins, GitHub Actions, Azure DevOps", SortOrder = 2, Lessons =
                    [
                        new Lesson { Title = "CI/CD concepts", DurationMinutes = 60, SortOrder = 1 },
                        new Lesson { Title = "Azure DevOps YAML pipelines", DurationMinutes = 120, SortOrder = 2 },
                    ]},
                    new CourseModule { Title = "Phase 3 — Containers & Kubernetes", SortOrder = 3, Lessons =
                    [
                        new Lesson { Title = "Docker deep dive", DurationMinutes = 100, SortOrder = 1 },
                        new Lesson { Title = "Kubernetes on AKS", DurationMinutes = 150, SortOrder = 2 },
                    ]},
                ]
            },
            new Course
            {
                Slug = "devops-self-paced-masterclass",
                Title = "DevOps Self-Paced Masterclass",
                Subtitle = "All recordings, source code and docs. Learn at your own pace.",
                Description = "Recorded classes covering DevOps, cloud, DevSecOps and MLOps with full project source code and documentation.",
                Highlights = "Lifetime access to recordings\nProject source code & docs\nDoubt support\nCertificate of completion",
                PriceInr = 6999, MrpInr = 15000, Mode = CourseMode.SelfPaced,
                Hours = 200, Projects = 35, SortOrder = 2,
                Modules = [ new CourseModule { Title = "Getting started", SortOrder = 1, Lessons = [ new Lesson { Title = "Course overview", DurationMinutes = 10, IsPreview = true, SortOrder = 1 } ] } ]
            },
            new Course
            {
                Slug = "end-to-end-cicd-project",
                Title = "End-to-End CI/CD Pipeline Project",
                Subtitle = "A free capstone: commit → build → scan → deploy to Kubernetes.",
                Description = "Build a corporate-style pipeline with GitHub Actions, SonarQube, Trivy, Docker, Helm and Argo CD.",
                Highlights = "Free forever\nFull pipeline walkthrough\nSource code included",
                PriceInr = 0, MrpInr = 0, Mode = CourseMode.Free, Badge = "Free",
                Hours = 6, Projects = 1, SortOrder = 3,
                Modules = [ new CourseModule { Title = "The project", SortOrder = 1, Lessons = [ new Lesson { Title = "Architecture walkthrough", DurationMinutes = 25, SortOrder = 1 } ] } ]
            });

        db.Coupons.Add(new Coupon { Code = "WELCOME10", PercentOff = 10, IsActive = true });
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded starter courses.");
    }
}

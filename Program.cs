using System.Security.Claims;
using System.Text.Json.Nodes;
using AcademyNet.Data;
using AcademyNet.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ---------- Database ----------
var provider = config["Database:Provider"] ?? "Sqlite";
var conn = config.GetConnectionString("Default") ?? "Data Source=App_Data/academy.db";
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "App_Data"));
builder.Services.AddDbContext<AppDbContext>(o =>
{
    if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)) o.UseSqlServer(conn);
    else o.UseSqlite(conn);
});

// ---------- Identity ----------
builder.Services.AddIdentity<AppUser, IdentityRole>(o =>
    {
        o.Password.RequiredLength = 8;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequireUppercase = false;
        o.Password.RequireLowercase = false;
        o.User.RequireUniqueEmail = true;
        o.Lockout.MaxFailedAccessAttempts = 8;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/AccessDenied";
    o.ExpireTimeSpan = TimeSpan.FromDays(30);
    o.SlidingExpiration = true;
});

builder.Services.AddAuthorization(o => o.AddPolicy("Admin", p => p.RequireRole(Roles.Admin)));
builder.Services.AddRazorPages(o =>
{
    o.Conventions.AuthorizeFolder("/Admin", "Admin");
    o.Conventions.AuthorizeFolder("/Learn");
    o.Conventions.AuthorizeFolder("/Checkout");
    o.Conventions.AllowAnonymousToPage("/Learn/Lesson"); // free preview lessons; page checks access itself
});

// ---------- App services ----------
builder.Services.Configure<SiteOptions>(config.GetSection("Site"));
builder.Services.Configure<RazorpayOptions>(config.GetSection("Razorpay"));
builder.Services.AddHttpClient<RazorpayService>(c => c.Timeout = TimeSpan.FromSeconds(20));
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddSingleton<FileStorage>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromDays(7);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});
const long maxUpload = 100L * 1024 * 1024;
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = maxUpload);
builder.Services.Configure<IISServerOptions>(o => o.MaxRequestBodySize = maxUpload);

var app = builder.Build();
await Seed.RunAsync(app.Services, config, app.Logger);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

// Health check for Azure Web Apps / load balancers / CI smoke test
app.MapGet("/health", async (AppDbContext db) =>
    await db.Database.CanConnectAsync() ? Results.Ok(new { status = "UP" }) : Results.StatusCode(503));

// Lesson notes download: enrolled students, preview lessons, or admin
app.MapGet("/files/lesson/{id:int}", async (int id, HttpContext ctx, AppDbContext db, FileStorage files) =>
{
    var lesson = await db.Lessons.Include(l => l.Module).FirstOrDefaultAsync(l => l.Id == id);
    if (lesson?.PdfStoredName == null) return Results.NotFound();

    var uid = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var allowed = lesson.IsPreview || ctx.User.IsInRole(Roles.Admin) ||
                  (uid != null && await db.Enrollments.AnyAsync(e => e.UserId == uid && e.CourseId == lesson.Module!.CourseId));
    if (!allowed) return Results.Forbid();

    var path = files.PathOf(lesson.PdfStoredName);
    if (path == null) return Results.NotFound();
    var type = path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? "application/pdf" : "application/octet-stream";
    return Results.File(path, type, lesson.PdfOriginalName ?? "notes.pdf", enableRangeProcessing: true);
});

// Razorpay webhook (backup to the browser callback): set the URL + secret in the Razorpay dashboard
app.MapPost("/api/razorpay/webhook", async (HttpRequest req, RazorpayService rzp, AppDbContext db, OrderService orders) =>
{
    using var reader = new StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();
    if (!rzp.VerifyWebhook(body, req.Headers["X-Razorpay-Signature"].ToString())) return Results.Unauthorized();

    var json = JsonNode.Parse(body);
    var evt = json?["event"]?.GetValue<string>();
    if (evt is "payment.captured" or "order.paid")
    {
        var payment = json?["payload"]?["payment"]?["entity"];
        var rzpOrderId = payment?["order_id"]?.GetValue<string>();
        var paymentId = payment?["id"]?.GetValue<string>();
        var order = await db.Orders.FirstOrDefaultAsync(o => o.RazorpayOrderId == rzpOrderId);
        if (order != null) await orders.MarkPaidAsync(order, paymentId);
    }
    return Results.Ok();
}).DisableAntiforgery();

app.Run();

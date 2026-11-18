namespace AcademyNet.Services;

/// <summary>Lesson PDFs/notes stored outside wwwroot; served only through an access-checked endpoint.</summary>
public class FileStorage
{
    private readonly string _root;
    private static readonly string[] Allowed = [".pdf", ".zip", ".docx", ".pptx", ".xlsx", ".txt", ".md", ".yaml", ".yml"];

    public FileStorage(IConfiguration config, IWebHostEnvironment env)
    {
        var p = config["Storage:FilesPath"];
        _root = string.IsNullOrWhiteSpace(p) ? Path.Combine(env.ContentRootPath, "App_Data", "files") : p;
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Allowed.Contains(ext)) throw new InvalidOperationException($"File type {ext} is not allowed.");
        var name = $"{Guid.NewGuid():N}{ext}";
        await using var fs = File.Create(Path.Combine(_root, name));
        await file.CopyToAsync(fs);
        return name;
    }

    public string? PathOf(string? stored)
    {
        if (string.IsNullOrEmpty(stored) || stored.Contains("..") || stored.Contains('/') || stored.Contains('\\')) return null;
        var p = Path.Combine(_root, stored);
        return File.Exists(p) ? p : null;
    }

    public void Delete(string? stored)
    {
        var p = PathOf(stored);
        if (p != null) File.Delete(p);
    }
}

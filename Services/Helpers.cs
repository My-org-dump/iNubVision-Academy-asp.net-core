using System.Globalization;
using System.Text.RegularExpressions;

namespace AcademyNet.Services;

public static partial class Helpers
{
    private static readonly CultureInfo India = CultureInfo.GetCultureInfo("en-IN");

    public static string Inr(int amount) => "₹" + amount.ToString("N0", India);

    public static string Slugify(string text)
    {
        var s = NonSlug().Replace(text.ToLowerInvariant(), "-").Trim('-');
        return s.Length > 100 ? s[..100].Trim('-') : s;
    }

    /// <summary>Turns a YouTube / Vimeo / Cloudflare / mp4 link into something the player can show.</summary>
    public static (string Kind, string Src) Embed(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return ("none", "");
        url = url.Trim();

        var yt = YouTube().Match(url);
        if (yt.Success) return ("iframe", $"https://www.youtube-nocookie.com/embed/{yt.Groups[1].Value}?rel=0&modestbranding=1");

        var vimeo = Vimeo().Match(url);
        if (vimeo.Success) return ("iframe", $"https://player.vimeo.com/video/{vimeo.Groups[1].Value}");

        if (url.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) || url.EndsWith(".webm", StringComparison.OrdinalIgnoreCase))
            return ("video", url);

        return ("iframe", url); // e.g. Cloudflare Stream / Bunny / any embeddable player URL
    }

    public static string Duration(int minutes) => minutes >= 60 ? $"{minutes / 60}h {minutes % 60:00}m" : $"{minutes}m";

    [GeneratedRegex("[^a-z0-9]+")] private static partial Regex NonSlug();
    [GeneratedRegex(@"(?:youtube\.com/(?:watch\?v=|embed/|live/|shorts/)|youtu\.be/)([A-Za-z0-9_-]{11})")] private static partial Regex YouTube();
    [GeneratedRegex(@"vimeo\.com/(?:video/)?(\d+)")] private static partial Regex Vimeo();
}

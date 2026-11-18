namespace AcademyNet.Services;

/// <summary>Brand and landing-page content, all editable in appsettings.json ("Site" section).</summary>
public class SiteOptions
{
    public string Name { get; set; } = "iNubVision Academy";
    public string Tagline { get; set; } = "";
    public string? Announcement { get; set; }
    public string? AnnouncementLink { get; set; }
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? WhatsApp { get; set; }
    public string? YouTube { get; set; }
    public string? LinkedIn { get; set; }
    public string? Instagram { get; set; }
    public string TrainerName { get; set; } = "";
    public string TrainerBio { get; set; } = "";
    public List<Stat> Stats { get; set; } = [];
    public List<Webinar> Webinars { get; set; } = [];
    public List<string> Technologies { get; set; } = [];

    public class Stat { public int Value { get; set; } public string Suffix { get; set; } = ""; public string Label { get; set; } = ""; }
    public class Webinar { public string Title { get; set; } = ""; public string Info { get; set; } = ""; public string Url { get; set; } = "#"; }
}

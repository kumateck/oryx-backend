namespace APP.Services.Documents;

/// <summary>
/// The company as it appears at the top of every document we send. Kept identical to the
/// desktop app's print header (oryx-live-desktop: apps/web/src/shared/print/company.ts) so an
/// emailed document and a printed one carry the same letterhead. Change both together.
/// </summary>
public static class CompanyProfile
{
    public const string Name = "Entrance Pharmaceuticals & Research Centre";
    public const string Address = "No. 16 Okpoi Gonno, Spintex Road, Accra";
    public const string Phone = "+233 302 234375 / 250948";
    public const string Email = "info@entranceindustries.com";
    public const string Website = "www.entranceindustries.com";

    /// <summary>The same image as the desktop app's assets/company-logo.png.</summary>
    public const string LogoPath = "wwwroot/images/company-logo.png";

    /// <summary>How an email refers to the logo it carries as an inline image.</summary>
    public const string LogoContentId = "company-logo";

    private static readonly Lazy<string> LogoDataUri = new(() =>
        File.Exists(LogoPath) ? $"data:image/png;base64,{Convert.ToBase64String(File.ReadAllBytes(LogoPath))}" : "");

    /// <summary>The logo embedded in the page, for PDFs.</summary>
    public static string LogoForPdf => LogoDataUri.Value;

    /// <summary>The logo as an inline email image: mail clients block data URIs.</summary>
    public static string LogoForEmail => $"cid:{LogoContentId}";
}

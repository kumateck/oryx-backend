using System.Net;
using APP.Services.Pdf;

namespace APP.Services.Documents;

/// <summary>
/// An email that carries a document: a short covering message, the document itself in the body,
/// and the same document attached as a PDF. Both come from one <see cref="DocumentSheet"/>, so what
/// the recipient reads, what they print from the attachment and what we print in the app match.
/// </summary>
public sealed class DocumentEmail
{
    public required string Subject { get; init; }

    /// <summary>Who the greeting addresses, usually the contact person.</summary>
    public required string Salutation { get; init; }

    /// <summary>The covering message, one paragraph per entry.</summary>
    public required IReadOnlyList<string> Message { get; init; }

    public required DocumentSheet Document { get; init; }

    /// <summary>The attachment's name, without the extension.</summary>
    public required string FileName { get; init; }

    private static string E(string value) => WebUtility.HtmlEncode(value ?? "");

    public List<(byte[] fileContent, string fileName, string fileType)> Attachments(IPdfService pdfService) =>
        [(pdfService.GeneratePdfFromHtml(Document.RenderPage()), $"{SafeFileName(FileName)}.pdf", "application/pdf")];

    public string RenderBody()
    {
        var paragraphs = string.Concat(Message.Select(paragraph =>
            $@"<p style=""margin:0 0 12px;"">{E(paragraph)}</p>"));

        return $@"<!DOCTYPE html>
<html lang=""en""><head><meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"" />
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
<meta name=""x-apple-disable-message-reformatting"" /><title>{E(Subject)}</title>
<style>
  /* Phones: stack the letterhead and facts, and let only the lines table scroll sideways. */
  @media only screen and (max-width: 620px) {{
    .doc-fit {{ table-layout: fixed !important; width: 100% !important; }}
    .doc-card {{ padding: 16px 12px !important; }}
    .doc-stack {{ display: block !important; width: 100% !important; padding: 0 0 8px 0 !important; text-align: left !important; }}
    .doc-empty {{ display: none !important; }}
    .doc-scroll {{ overflow-x: auto !important; -webkit-overflow-scrolling: touch; }}
    .doc-lines {{ min-width: 520px; }}
  }}
</style></head>
<body style=""margin:0;padding:0;background:#f4f5f7;"">
<table class=""doc-fit"" role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""width:100%;border-collapse:collapse;background:#f4f5f7;"">
<tr><td align=""center"" style=""padding:24px 12px;"">
  <table class=""doc-fit"" role=""presentation"" width=""760"" cellpadding=""0"" cellspacing=""0"" style=""width:100%;max-width:760px;border-collapse:collapse;"">
    <tr><td style=""padding:0 4px 16px;color:#333333;font-family:Arial, Helvetica, sans-serif;font-size:14px;line-height:1.5;"">
      <p style=""margin:0 0 12px;"">Dear {E(Salutation)},</p>
      {paragraphs}
      <p style=""margin:0;"">A PDF copy is attached for your records.</p>
    </td></tr>
    <tr><td class=""doc-card"" style=""padding:28px 32px;background:#ffffff;border:1px solid #dddddd;border-radius:4px;"">
      {Document.Render(CompanyProfile.LogoForEmail)}
    </td></tr>
    <tr><td style=""padding:16px 4px 0;color:#333333;font-family:Arial, Helvetica, sans-serif;font-size:14px;line-height:1.5;"">
      <p style=""margin:0 0 12px;"">Kind regards,<br />Supply Chain<br />{E(CompanyProfile.Name)}</p>
      <p style=""margin:0;color:#777777;font-size:11px;"">{E(CompanyProfile.Address)} &middot; {E(CompanyProfile.Phone)} &middot; {E(CompanyProfile.Email)} &middot; {E(CompanyProfile.Website)}</p>
    </td></tr>
  </table>
</td></tr>
</table>
</body></html>";
    }

    private static string SafeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(character => invalid.Contains(character) ? '-' : character).ToArray()).Trim().TrimEnd('.');
        return cleaned.Length == 0 ? "Document" : cleaned;
    }
}

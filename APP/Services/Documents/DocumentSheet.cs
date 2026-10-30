using System.Net;
using System.Text;

namespace APP.Services.Documents;

/// <summary>One cell of a document table: a main line and an optional smaller line under it (a code).</summary>
public sealed record DocumentCell(string Text, string Detail = null)
{
    public static implicit operator DocumentCell(string text) => new(text);
}

public sealed record DocumentColumn(string Header, string Width = null, bool AlignRight = false);

/// <summary>A labelled amount under a table, such as a total.</summary>
public sealed record DocumentTotal(string Label, string Value);

/// <summary>
/// A document laid out the way the desktop app prints it (oryx-live-desktop: shared/print/print-sheet.tsx
/// and shared/theme/document.css): the letterhead with the document's name, who it is addressed to,
/// labelled facts, a note, the lines, totals and lines to sign on.
///
/// It renders as tables with inline styles, the only markup that both the PDF engine (an old WebKit
/// without flexbox or grid) and mail clients display the same way, so one rendering serves as the
/// attached PDF and as the body of the email.
/// </summary>
public sealed class DocumentSheet
{
    private const string Font = "Arial, Helvetica, sans-serif";
    private const string Ink = "#111111";

    public required string Title { get; init; }
    public string Reference { get; init; }

    /// <summary>Who the document is addressed to, then their contact lines.</summary>
    public string ToName { get; init; }
    public IReadOnlyList<string> ToLines { get; init; } = [];

    /// <summary>Facts without a value are left out, as on the printed copy.</summary>
    public IReadOnlyList<(string Label, string Value)> Facts { get; init; } = [];

    public string Note { get; init; }

    public IReadOnlyList<DocumentColumn> Columns { get; init; } = [];
    public IReadOnlyList<IReadOnlyList<DocumentCell>> Rows { get; init; } = [];
    public string EmptyText { get; init; } = "No lines";
    public IReadOnlyList<DocumentTotal> Totals { get; init; } = [];

    public string AmountInWords { get; init; }
    public IReadOnlyList<string> Signatures { get; init; } = [];

    private static string E(string value) => WebUtility.HtmlEncode(value ?? "");

    /// <summary>The document itself, without a page around it.</summary>
    public string Render(string logoSrc)
    {
        var html = new StringBuilder();
        html.Append($@"<table class=""doc-fit"" role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""width:100%;border-collapse:collapse;background:#ffffff;color:{Ink};font-family:{Font};font-size:12px;line-height:1.45;"">");
        html.Append("<tr><td style=\"padding:0;\">");

        AppendHeader(html, logoSrc);
        AppendAddressee(html);
        AppendFacts(html);
        if (!string.IsNullOrWhiteSpace(Note))
            html.Append($@"<p style=""margin:12px 0 0;color:#333333;"">{E(Note)}</p>");
        if (Columns.Count > 0)
            AppendTable(html);
        if (!string.IsNullOrWhiteSpace(AmountInWords))
            html.Append($@"<p style=""margin:12px 0 0;color:#333333;""><strong>Amount in words:</strong> {E(AmountInWords)}</p>");
        AppendSignatures(html);

        html.Append("</td></tr></table>");
        return html.ToString();
    }

    /// <summary>The document as a standalone A4 page, for the PDF attachment.</summary>
    public string RenderPage()
    {
        return $@"<!DOCTYPE html>
<html lang=""en""><head><meta charset=""UTF-8"" /><title>{E(Title)}{(string.IsNullOrWhiteSpace(Reference) ? "" : $" {E(Reference)}")}</title>
<style>
  body {{ margin: 0; padding: 0; background: #ffffff; }}
  thead {{ display: table-header-group; }}
  tr {{ page-break-inside: avoid; }}
</style></head>
<body>{Render(CompanyProfile.LogoForPdf)}</body></html>";
    }

    private void AppendHeader(StringBuilder html, string logoSrc)
    {
        html.Append($@"<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""width:100%;border-collapse:collapse;border-bottom:2px solid {Ink};""><tr>");
        html.Append(@"<td class=""doc-stack"" valign=""top"" style=""padding:0 0 12px;"">");
        html.Append(@"<table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse:collapse;""><tr>");
        if (!string.IsNullOrEmpty(logoSrc))
            html.Append($@"<td class=""doc-stack"" valign=""top"" style=""padding:0 12px 0 0;""><img src=""{logoSrc}"" alt=""{E(CompanyProfile.Name)}"" height=""52"" style=""display:block;height:52px;width:auto;border:0;"" /></td>");
        html.Append($@"<td class=""doc-stack"" valign=""top"" style=""padding:0;color:#444444;font-size:10.5px;line-height:1.45;"">
<strong style=""display:block;color:{Ink};font-size:13px;"">{E(CompanyProfile.Name)}</strong>
<span style=""display:block;"">{E(CompanyProfile.Address)}</span>
<span style=""display:block;"">{E(CompanyProfile.Phone)}</span>
<span style=""display:block;"">{E(CompanyProfile.Email)} &middot; {E(CompanyProfile.Website)}</span></td>");
        html.Append("</tr></table></td>");
        html.Append($@"<td class=""doc-stack doc-title"" valign=""top"" align=""right"" style=""padding:0 0 12px 24px;text-align:right;"">
<div style=""margin:0;font-size:17px;font-weight:700;letter-spacing:0.02em;text-transform:uppercase;"">{E(Title)}</div>");
        if (!string.IsNullOrWhiteSpace(Reference))
            html.Append($@"<div style=""margin:2px 0 0;font-size:12px;font-weight:600;"">{E(Reference)}</div>");
        html.Append("</td></tr></table>");
    }

    private void AppendAddressee(StringBuilder html)
    {
        if (string.IsNullOrWhiteSpace(ToName))
            return;
        html.Append($@"<div style=""margin:12px 0 0;color:#333333;""><strong>To: {E(ToName)}</strong>");
        foreach (var line in ToLines.Where(line => !string.IsNullOrWhiteSpace(line)))
            html.Append($"<div>{E(line)}</div>");
        html.Append("</div>");
    }

    private void AppendFacts(StringBuilder html)
    {
        var shown = Facts.Where(fact => !string.IsNullOrWhiteSpace(fact.Value)).ToList();
        if (shown.Count == 0)
            return;

        html.Append(@"<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""width:100%;border-collapse:collapse;margin:14px 0;table-layout:fixed;"">");
        for (var start = 0; start < shown.Count; start += 3)
        {
            html.Append("<tr>");
            for (var column = 0; column < 3; column++)
            {
                var index = start + column;
                html.Append($@"<td class=""doc-stack{(index < shown.Count ? "" : " doc-empty")}"" valign=""top"" width=""33%"" style=""width:33.33%;padding:0 {(column < 2 ? 24 : 0)}px 8px 0;"">");
                if (index < shown.Count)
                {
                    var (label, value) = shown[index];
                    html.Append($@"<div style=""color:#555555;font-size:10px;letter-spacing:0.04em;text-transform:uppercase;"">{E(label)}</div>
<div style=""font-weight:600;word-wrap:break-word;"">{E(value)}</div>");
                }
                html.Append("</td>");
            }
            html.Append("</tr>");
        }
        html.Append("</table>");
    }

    private void AppendTable(StringBuilder html)
    {
        const string cell = "padding:5px 8px;border:1px solid #999999;vertical-align:top;word-wrap:break-word;";
        html.Append(@"<div class=""doc-scroll"" style=""margin-top:12px;"">");
        html.Append(@"<table class=""doc-lines"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""width:100%;border-collapse:collapse;"">");

        html.Append("<thead><tr>");
        foreach (var column in Columns)
        {
            var width = column.Width is null ? "" : $"width:{column.Width};";
            html.Append($@"<th scope=""col"" style=""{cell}{width}background:#eeeeee;font-size:10.5px;font-weight:700;text-align:{(column.AlignRight ? "right" : "left")};"">{E(column.Header)}</th>");
        }
        html.Append("</tr></thead><tbody>");

        if (Rows.Count == 0)
        {
            html.Append($@"<tr><td colspan=""{Columns.Count}"" style=""{cell}color:#555555;text-align:center;"">{E(EmptyText)}</td></tr>");
        }

        foreach (var row in Rows)
        {
            html.Append("<tr>");
            for (var index = 0; index < Columns.Count; index++)
            {
                var value = index < row.Count ? row[index] : null;
                html.Append($@"<td style=""{cell}text-align:{(Columns[index].AlignRight ? "right" : "left")};"">{E(value?.Text)}");
                if (!string.IsNullOrWhiteSpace(value?.Detail))
                    html.Append($@"<span style=""display:block;color:#555555;font-size:10px;"">{E(value.Detail)}</span>");
                html.Append("</td>");
            }
            html.Append("</tr>");
        }
        html.Append("</tbody>");

        if (Totals.Count > 0)
        {
            html.Append("<tfoot>");
            foreach (var total in Totals)
            {
                html.Append($@"<tr><td colspan=""{Columns.Count - 1}"" style=""{cell}font-weight:700;text-align:right;"">{E(total.Label)}</td>
<td style=""{cell}font-weight:700;text-align:right;"">{E(total.Value)}</td></tr>");
            }
            html.Append("</tfoot>");
        }
        html.Append("</table></div>");
    }

    private void AppendSignatures(StringBuilder html)
    {
        if (Signatures.Count == 0)
            return;
        html.Append(@"<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""width:100%;border-collapse:collapse;margin-top:44px;font-size:11px;page-break-inside:avoid;""><tr>");
        for (var index = 0; index < Signatures.Count; index++)
        {
            var gap = index < Signatures.Count - 1 ? "padding-right:32px;" : "";
            html.Append($@"<td valign=""top"" style=""{gap}""><div style=""height:28px;border-bottom:1px solid {Ink};margin-bottom:4px;""></div>{E(Signatures[index])}</td>");
        }
        html.Append("</tr></table>");
    }
}

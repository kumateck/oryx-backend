using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.AspNetCore.Http;

namespace APP.Services.QcWorksheets;

public sealed class StpDocxParsedStep
{
    public string Title { get; set; }
    public string Instruction { get; set; }
    public string ReferencedCode { get; set; }
}

public sealed class StpDocxParseResult : StpImportResultDto
{
    public string Area { get; set; }
    public string Purpose { get; set; }
    public string Scope { get; set; }
    public string Responsibility { get; set; }
    public string Accountability { get; set; }
    public List<StpDocxParsedStep> Steps { get; set; } = [];
}

public interface IStpDocxImportService
{
    Task<StpDocxParseResult> ParseAsync(IFormFile file);
}

/// <summary>
/// Parses a legacy STP .docx into a Draft StandardTestProcedure.
/// <para>
/// Built to the findings of the parser spike, which corrected three assumptions that made
/// the original design fail on every real file:
/// 1. the header metadata table lives in a Word <b>running header</b> part
///    (<c>word/header*.xml</c>), not the document body — and the populated part is not at a
///    fixed index, so every header part is checked;
/// 2. there is no space between the section number and the heading word (real text is
///    <c>"1.0Purpose :"</c>), so patterns use <c>\d\.0\s*Word</c>;
/// 3. the body is one large table of concatenated cell text rather than discrete
///    paragraphs, so headings are located by searching the full joined text, not line by line.
/// </para>
/// <para>
/// Import is a time-saver, not a migration requirement: anything that cannot be parsed
/// confidently is reported per-file and can still be authored by hand.
/// </para>
/// </summary>
public class StpDocxImportService : IStpDocxImportService
{
    private const long MaxBytes = 20 * 1024 * 1024;

    // Anchored to the real code format rather than a greedy character class — the spike
    // found a greedy pattern swallowed the following product name, because correction 2
    // means there is no separator there either.
    private static readonly Regex StpCodeRegex = new(
        @"(?<code>[A-Z]{2,4}/STP/(?:RM|FP)/\d{3})", RegexOptions.Compiled);

    private static readonly Regex RevisionRegex = new(
        @"Revision\s*No\.?\s*:?\s*(?<value>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex AreaRegex = new(
        @"Area\s*:?\s*(?<value>[A-Za-z0-9 &/\-]{2,60})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ReferToRegex = new(
        @"[Rr]efer\s+to\s+(?<code>[A-Z]{2,4}/STP/(?:RM|FP)/\d{3})", RegexOptions.Compiled);

    // Sub-numbering inside the Procedure section: 5.1, 5.2, 5.10 ...
    private static readonly Regex SubStepRegex = new(
        @"(?<!\d)(?<number>5\.(?<index>\d{1,2}))(?!\d)", RegexOptions.Compiled);

    private static readonly (string Key, Regex Pattern)[] SectionPatterns =
    [
        ("Purpose",        new Regex(@"1\.0\s*Purpose", RegexOptions.Compiled | RegexOptions.IgnoreCase)),
        ("Scope",          new Regex(@"2\.0\s*Scope", RegexOptions.Compiled | RegexOptions.IgnoreCase)),
        ("Responsibility", new Regex(@"3\.0\s*Responsibilit", RegexOptions.Compiled | RegexOptions.IgnoreCase)),
        ("Accountability", new Regex(@"4\.0\s*Accountabilit", RegexOptions.Compiled | RegexOptions.IgnoreCase)),
        ("Procedure",      new Regex(@"5\.0\s*Procedure", RegexOptions.Compiled | RegexOptions.IgnoreCase))
    ];

    public async Task<StpDocxParseResult> ParseAsync(IFormFile file)
    {
        var result = new StpDocxParseResult { FileName = file?.FileName, Succeeded = false };

        var guard = Validate(file);
        if (guard is not null)
        {
            result.FailureReason = guard;
            return result;
        }

        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var document = WordprocessingDocument.Open(stream, false);
            var mainPart = document.MainDocumentPart;
            if (mainPart is null)
            {
                result.FailureReason = "The document has no readable content.";
                return result;
            }

            var headerText = ExtractHeaderText(mainPart);
            var bodyText = Normalize(mainPart.Document?.Body?.InnerText ?? string.Empty);

            if (string.IsNullOrWhiteSpace(bodyText))
            {
                result.FailureReason = "The document body is empty.";
                return result;
            }

            // --- header metadata -------------------------------------------------
            var searchSpace = string.IsNullOrWhiteSpace(headerText) ? bodyText : headerText;

            var codeMatch = StpCodeRegex.Match(searchSpace);
            if (!codeMatch.Success)
                codeMatch = StpCodeRegex.Match(bodyText);

            if (codeMatch.Success)
                result.Code = codeMatch.Groups["code"].Value;
            else
                result.FlaggedForReview.Add("Code (STP No. not found)");

            var areaMatch = AreaRegex.Match(searchSpace);
            if (areaMatch.Success)
                result.Area = areaMatch.Groups["value"].Value.Trim();
            else
                result.FlaggedForReview.Add("Area");

            if (!RevisionRegex.IsMatch(searchSpace))
                result.FlaggedForReview.Add("Revision No.");

            // --- body sections ---------------------------------------------------
            var sections = SplitSections(bodyText);

            result.Purpose = sections.GetValueOrDefault("Purpose");
            result.Scope = sections.GetValueOrDefault("Scope");
            result.Responsibility = sections.GetValueOrDefault("Responsibility");
            result.Accountability = sections.GetValueOrDefault("Accountability");

            foreach (var key in new[] { "Purpose", "Scope", "Responsibility", "Accountability" })
            {
                if (string.IsNullOrWhiteSpace(sections.GetValueOrDefault(key)))
                    result.FlaggedForReview.Add(key);
            }

            var procedure = sections.GetValueOrDefault("Procedure");
            result.Steps = SplitSteps(procedure);
            if (result.Steps.Count == 0)
                result.FlaggedForReview.Add("Procedure steps");

            // Name: the document title is the text before the code in the header, falling
            // back to the file name, which a reviewer can correct.
            result.Name = DeriveName(searchSpace, result.Code)
                          ?? Path.GetFileNameWithoutExtension(file.FileName);

            if (string.IsNullOrWhiteSpace(result.Code) && result.Steps.Count == 0)
            {
                result.FailureReason =
                    "Neither the STP number nor any procedure steps could be located. "
                    + "This file does not match the expected STP layout.";
                return result;
            }

            result.Succeeded = true;
            return result;
        }
        catch (Exception ex)
        {
            result.Succeeded = false;
            result.FailureReason = $"The file could not be parsed: {ex.Message}";
            return result;
        }
    }

    private static string Validate(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return "The uploaded file is empty.";

        if (file.Length > MaxBytes)
            return $"The file exceeds the maximum allowed size of {MaxBytes / (1024 * 1024)}MB.";

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        return extension switch
        {
            ".docx" => null,
            ".docm" => "Macro-enabled documents (.docm) are not supported.",
            _ => "Only .docx files are supported."
        };
    }

    /// <summary>
    /// Correction 1: check every <c>word/header*.xml</c> part and use whichever has content.
    /// First-page and even-page headers were present but empty in every file tested.
    /// </summary>
    private static string ExtractHeaderText(MainDocumentPart mainPart)
    {
        var builder = new StringBuilder();

        foreach (var headerPart in mainPart.HeaderParts)
        {
            var text = headerPart.Header?.InnerText;
            if (!string.IsNullOrWhiteSpace(text))
                builder.Append(text).Append(' ');
        }

        return Normalize(builder.ToString());
    }

    /// <summary>
    /// Correction 3: locate every heading by searching the full joined text, then slice
    /// between consecutive headings.
    /// </summary>
    private static Dictionary<string, string> SplitSections(string text)
    {
        var found = new List<(string Key, int Start, int End)>();

        foreach (var (key, pattern) in SectionPatterns)
        {
            var match = pattern.Match(text);
            if (match.Success)
                found.Add((key, match.Index, match.Index + match.Length));
        }

        var ordered = found.OrderBy(item => item.Start).ToList();
        var sections = new Dictionary<string, string>();

        for (var i = 0; i < ordered.Count; i++)
        {
            var current = ordered[i];
            var sliceStart = current.End;
            var sliceEnd = i + 1 < ordered.Count ? ordered[i + 1].Start : text.Length;

            if (sliceEnd <= sliceStart)
                continue;

            sections[current.Key] = CleanSection(text[sliceStart..sliceEnd]);
        }

        return sections;
    }

    private static List<StpDocxParsedStep> SplitSteps(string procedure)
    {
        var steps = new List<StpDocxParsedStep>();
        if (string.IsNullOrWhiteSpace(procedure))
            return steps;

        var matches = SubStepRegex.Matches(procedure).ToList();
        if (matches.Count == 0)
        {
            // No sub-numbering: keep the whole procedure as a single step rather than
            // dropping content on the floor.
            steps.Add(new StpDocxParsedStep
            {
                Title = null,
                Instruction = CleanSection(procedure),
                ReferencedCode = ReferToRegex.Match(procedure) is { Success: true } m
                    ? m.Groups["code"].Value
                    : null
            });
            return steps;
        }

        for (var i = 0; i < matches.Count; i++)
        {
            var start = matches[i].Index + matches[i].Length;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : procedure.Length;
            if (end <= start)
                continue;

            var body = CleanSection(procedure[start..end]);
            if (string.IsNullOrWhiteSpace(body))
                continue;

            var referTo = ReferToRegex.Match(body);

            steps.Add(new StpDocxParsedStep
            {
                Title = matches[i].Groups["number"].Value,
                Instruction = body,
                ReferencedCode = referTo.Success ? referTo.Groups["code"].Value : null
            });
        }

        return steps;
    }

    private static string DeriveName(string headerText, string code)
    {
        if (string.IsNullOrWhiteSpace(headerText) || string.IsNullOrWhiteSpace(code))
            return null;

        var index = headerText.IndexOf(code, StringComparison.Ordinal);
        if (index < 0)
            return null;

        // Correction 2 again: with no separator after the code, the product name runs
        // straight on from it.
        var tail = headerText[(index + code.Length)..].Trim();
        if (tail.Length == 0)
            return null;

        var cutoff = tail.IndexOfAny(['\n', '\r']);
        if (cutoff > 0)
            tail = tail[..cutoff];

        tail = Regex.Replace(tail, @"(Revision|Supersedes|Effective|Review|Issue|Page)\b.*", "",
            RegexOptions.IgnoreCase).Trim();

        return string.IsNullOrWhiteSpace(tail) || tail.Length > 500
            ? null
            : tail;
    }

    private static string Normalize(string text) =>
        string.IsNullOrEmpty(text)
            ? string.Empty
            : Regex.Replace(text, @"[ \t ]+", " ").Trim();

    private static string CleanSection(string text) =>
        string.IsNullOrWhiteSpace(text)
            ? null
            : Regex.Replace(text, @"\s+", " ").Trim(' ', ':', '-', '.');
}

using System.Text.RegularExpressions;
using APP.Services.QcWorksheets.WorksheetDocxImport;
using DOMAIN.Entities.StpRevisions;

namespace APP.Services.QcWorksheets;

/// <summary>
/// Reads a client STP (.docx, already parsed by <see cref="DocxDocumentReader"/>) into an import proposal: the running
/// header, sections 1.0 to 4.0, the reagents block and the 5.N tests with their 5.N.M steps, conditions blocks and notes.
/// <para>
/// Keeps the parser spike's three rules: the metadata is in a running-header part (any of them), there is no space
/// after a section number ("1.0Purpose :"), and headings are matched on joined text, not on fixed positions. The body
/// of every client STP is one two-column table (number | text), split at page breaks; rows are read in order and the
/// number column decides what a row is. Paragraphs outside the table are read the same way, with a leading number.
/// </para>
/// </summary>
public static partial class StpDocxContentReader
{
    private sealed record Entry(string Number, List<string> Lines, bool HadTable);

    [GeneratedRegex(@"^(?<number>\d+(?:\.\d+)*)\.?$")]
    private static partial Regex NumberCell();

    [GeneratedRegex(@"^(?<number>\d\.\d+(?:\.\d+)*)\s*(?<rest>\S.*)$")]
    private static partial Regex LeadingNumber();

    [GeneratedRegex(@"^(?<n>[1-5])\.0\s*(?<word>Purpose|Scope|Responsibilit\w*|Accountabilit\w*|Procedure)\s*:?\s*(?<rest>.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex SectionHeading();

    [GeneratedRegex(@"^(?<word>Purpose|Scope|Responsibilit\w*|Accountabilit\w*|Procedure)\s*:?\s*(?<rest>.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex SectionWord();

    [GeneratedRegex(@"^Reagents?\s*:?\s*(?<rest>.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex ReagentsHeading();

    [GeneratedRegex(@"^5\.(?<index>\d{1,2})$")]
    private static partial Regex TestNumber();

    [GeneratedRegex(@"^5\.\d{1,2}(?:\.\d{1,2})+$")]
    private static partial Regex StepNumber();

    [GeneratedRegex(@"(?<code>[A-Z]{2,4}/(?:STP|SOP)/(?:[A-Z]{2,3}/)?\d{2,4})")]
    private static partial Regex ReferenceCode();

    [GeneratedRegex(@"\b(refer|see)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ReferWord();

    [GeneratedRegex(@"(conditions|mobile\s*phase|apparatus|medium|column|instrument|gradient)", RegexOptions.IgnoreCase)]
    private static partial Regex ConditionsHeading();

    [GeneratedRegex(@"(flow\s*-?\s*rate|column|temp|apparatus|speed|medium|wavelength|ʎ|λ|inj|det\.|duration)", RegexOptions.IgnoreCase)]
    private static partial Regex ConditionWord();

    [GeneratedRegex(@"\s+[-–]\s*|\s*[-–]\s+")]
    private static partial Regex ReagentSeparator();

    private static readonly string[] HeaderLabels =
        ["STP No.", "Revision No.", "Supersedes", "Effective Date", "Review Date", "Issue Date", "Area", "Reference"];

    public static StpImportProposalDto Read(DocxDocument document, string fileName, StpRevisionKind kind)
    {
        var proposal = new StpImportProposalDto { FileName = fileName, Source = fileName, Kind = kind };
        ReadHeader(document.HeaderText, proposal);

        var entries = Entries(document);
        var sections = new Dictionary<StpSectionKind, List<string>>();
        var order = new List<StpSectionKind>();
        var procedure = new List<Entry>();
        string current = null;
        foreach (var entry in entries)
        {
            var heading = Heading(entry);
            if (heading is not null)
            {
                current = heading.Value.Word;
                if (current != "Procedure")
                {
                    var key = SectionKey(current);
                    if (!sections.ContainsKey(key)) { sections[key] = []; order.Add(key); }
                    if (heading.Value.Remainder.Count > 0) sections[key].AddRange(heading.Value.Remainder);
                }
                else if (heading.Value.Remainder.Count > 0) procedure.Add(entry with { Number = null, Lines = heading.Value.Remainder });
                continue;
            }
            if (current is null) continue;
            if (current == "Procedure") procedure.Add(entry);
            else sections[SectionKey(current)].AddRange(entry.Lines);
        }

        foreach (var key in new[] { StpSectionKind.Purpose, StpSectionKind.Scope, StpSectionKind.Responsibility, StpSectionKind.Accountability })
        {
            var text = sections.TryGetValue(key, out var lines) ? string.Join("\n", lines).Trim() : null;
            proposal.Sections.Add(string.IsNullOrWhiteSpace(text)
                ? new StpImportSection { Kind = key, Text = null, Confidence = StpImportConfidence.Low, Flag = $"{Label(key)} was not found. Enter it." }
                : new StpImportSection { Kind = key, Text = text, Confidence = StpImportConfidence.High });
        }

        ReadProcedure(procedure, proposal);
        CheckNumbering(proposal);

        if (proposal.Tests.Count == 0 && sections.Count == 0)
        {
            proposal.Succeeded = false;
            proposal.FailureReason = document.HeaderText.Contains("STANDARD TEST PROCEDURE", StringComparison.OrdinalIgnoreCase)
                ? "No numbered sections (1.0 Purpose to 5.0 Procedure) were found in this STP."
                : "This file does not have the STP layout (1.0 Purpose to 5.0 Procedure). It may be an analytical worksheet.";
            return proposal;
        }
        if (proposal.Tests.Count == 0)
            proposal.Warnings.Add("No 5.N tests were found under 5.0 Procedure. Add the tests by hand.");

        AssignKeys(proposal.Tests);
        proposal.FlaggedCount = Count(proposal);
        proposal.Succeeded = true;
        return proposal;
    }

    // ---- header ---------------------------------------------------------------------------------------------

    private static void ReadHeader(string header, StpImportProposalDto proposal)
    {
        header ??= string.Empty;
        proposal.StpNumber = HeaderValue(header, "STP No.", required: true);
        if (proposal.StpNumber.Value is { } stp && ReferenceCode().Match(stp.Replace(" ", "")) is { Success: true } code)
            proposal.StpNumber.Value = code.Groups["code"].Value;
        proposal.RevisionNumber = HeaderValue(header, "Revision No.", required: true);
        proposal.SupersedesRevision = HeaderValue(header, "Supersedes", required: false);
        proposal.Area = HeaderValue(header, "Area", required: false);
        proposal.Reference = HeaderValue(header, "Reference", required: false);
        proposal.EffectiveDate = HeaderValue(header, "Effective Date", required: false);
        proposal.ReviewDate = HeaderValue(header, "Review Date", required: false);
        proposal.IssueDate = HeaderValue(header, "Issue Date", required: false);
        foreach (var number in new[] { proposal.RevisionNumber, proposal.SupersedesRevision })
        {
            if (number.Value is null) continue;
            var digits = Regex.Match(number.Value, @"\d+");
            if (digits.Success && digits.Value.Length < number.Value.Length)
            {
                number.Value = digits.Value;
                number.Confidence = StpImportConfidence.Medium;
                number.Flag = "Read the number from surrounding text; check it.";
            }
            else if (!digits.Success)
            {
                number.Confidence = StpImportConfidence.Low;
                number.Flag = "Not a number; enter it.";
            }
        }
        proposal.Title = Title(header, proposal.StpNumber.Value);
    }

    /// <summary>The value after "Label:" up to the next header label, read from the first occurrence of the label.</summary>
    private static StpImportField HeaderValue(string header, string label, bool required)
    {
        var start = Regex.Match(header, Regex.Escape(label).Replace(@"\ ", @"\s*") + @"\s*:?\s*", RegexOptions.IgnoreCase);
        if (!start.Success)
            return new StpImportField
            {
                Confidence = required ? StpImportConfidence.Low : StpImportConfidence.Medium,
                Flag = $"\"{label}\" was not found in the running header.",
            };
        var rest = header[(start.Index + start.Length)..];
        var end = rest.Length;
        foreach (var other in HeaderLabels.Append("STANDARD TEST PROCEDURE"))
        {
            var match = Regex.Match(rest, Regex.Escape(other).Replace(@"\ ", @"\s*"), RegexOptions.IgnoreCase);
            if (match.Success && match.Index < end) end = match.Index;
        }
        var value = rest[..end].Trim(' ', ':', '.');
        if (label == "STP No.")
        {
            // The title runs straight on from the code (spike rule 2): keep only the code.
            var code = ReferenceCode().Match(value.Replace(" ", ""));
            value = code.Success ? code.Groups["code"].Value : value;
        }
        if (value.Length == 0)
            return new StpImportField
            {
                Confidence = required ? StpImportConfidence.Low : StpImportConfidence.High,
                Flag = required ? $"\"{label}\" is blank in the running header." : null,
            };
        return new StpImportField { Value = value, Confidence = StpImportConfidence.High };
    }

    /// <summary>The title is printed after the STP number, before "Revision No.".</summary>
    private static StpImportField Title(string header, string code)
    {
        if (!string.IsNullOrEmpty(code))
        {
            var at = header.Replace(" ", "").IndexOf(code, StringComparison.Ordinal);
            var start = Regex.Match(header, string.Join(@"\s*", code.Select(ch => Regex.Escape(ch.ToString()))));
            if (at >= 0 && start.Success)
            {
                var rest = header[(start.Index + start.Length)..];
                var end = rest.Length;
                foreach (var other in HeaderLabels)
                {
                    var match = Regex.Match(rest, Regex.Escape(other).Replace(@"\ ", @"\s*"), RegexOptions.IgnoreCase);
                    if (match.Success && match.Index < end) end = match.Index;
                }
                var title = rest[..end].Trim(' ', ':', '-');
                if (title.Length is > 0 and <= 500) return new StpImportField { Value = title, Confidence = StpImportConfidence.High };
            }
        }
        return new StpImportField { Confidence = StpImportConfidence.Low, Flag = "The title was not found after the STP number." };
    }

    // ---- body entries ---------------------------------------------------------------------------------------

    /// <summary>Every table row and loose paragraph, in order, as (number, lines).</summary>
    private static List<Entry> Entries(DocxDocument document)
    {
        var entries = new List<Entry>();
        foreach (var block in document.Blocks)
        {
            if (block.Table is null)
            {
                entries.Add(FromText(block.Text));
                continue;
            }
            CollectTable(block.Table, entries);
        }
        return entries.Where(entry => entry.Lines.Count > 0 || entry.Number is not null).ToList();
    }

    private static void CollectTable(DocxTable table, List<Entry> entries)
    {
        for (var row = 0; row < table.Rows.Count; row++)
        {
            var cells = table.OriginCells(row).Where(cell => cell.Row == row).ToList();
            if (cells.Count == 0) continue;
            string number = null;
            var lines = new List<string>();
            var hadTable = false;
            for (var index = 0; index < cells.Count; index++)
            {
                var cell = cells[index];
                if (index == 0 && NumberCell().Match(cell.Text.Trim()) is { Success: true } match && cells.Count > 1)
                {
                    number = match.Groups["number"].Value;
                    continue;
                }
                lines.AddRange(cell.Lines.Select(Tidy));
                foreach (var nested in cell.NestedTables)
                {
                    hadTable = true;
                    for (var nestedRow = 0; nestedRow < nested.Rows.Count; nestedRow++)
                    {
                        var text = string.Join(" | ", nested.OriginCells(nestedRow).Select(item => item.Text).Where(item => item.Length > 0));
                        if (text.Length > 0) lines.Add(text);
                    }
                }
            }
            if (number is null && lines.Count > 0 && LeadingNumber().Match(lines[0]) is { Success: true } leading
                && (TestNumber().IsMatch(leading.Groups["number"].Value) || StepNumber().IsMatch(leading.Groups["number"].Value)))
            {
                number = leading.Groups["number"].Value;
                lines[0] = leading.Groups["rest"].Value;
            }
            if (number is null && lines.Count == 0) continue;
            entries.Add(new Entry(number, lines, hadTable));
        }
    }

    private static Entry FromText(string text)
    {
        var line = text?.Trim() ?? string.Empty;
        if (SectionHeading().IsMatch(line)) return new Entry(null, [line], false);
        var leading = LeadingNumber().Match(line);
        if (leading.Success && (TestNumber().IsMatch(leading.Groups["number"].Value) || StepNumber().IsMatch(leading.Groups["number"].Value)))
            return new Entry(leading.Groups["number"].Value, [leading.Groups["rest"].Value], false);
        return new Entry(null, line.Length == 0 ? [] : [line], false);
    }

    /// <summary>A 1.0 to 5.0 heading: number cell "1.0" with "Purpose :", or the text "1.0Purpose :".</summary>
    private static (string Word, List<string> Remainder)? Heading(Entry entry)
    {
        if (entry.Lines.Count == 0) return null;
        var first = entry.Lines[0];
        if (entry.Number is { } number && Regex.IsMatch(number, @"^[1-5]\.0$") && SectionWord().Match(first) is { Success: true } word)
            return (Canonical(word.Groups["word"].Value), Rest(word.Groups["rest"].Value, entry.Lines));
        if (entry.Number is null && SectionHeading().Match(first) is { Success: true } inline)
            return (Canonical(inline.Groups["word"].Value), Rest(inline.Groups["rest"].Value, entry.Lines));
        return null;
    }

    private static List<string> Rest(string remainder, List<string> lines)
    {
        var rest = new List<string>();
        if (!string.IsNullOrWhiteSpace(remainder)) rest.Add(remainder.Trim());
        rest.AddRange(lines.Skip(1));
        return rest;
    }

    private static string Canonical(string word) => word.ToLowerInvariant() switch
    {
        var w when w.StartsWith("purpose") => "Purpose",
        var w when w.StartsWith("scope") => "Scope",
        var w when w.StartsWith("responsibilit") => "Responsibility",
        var w when w.StartsWith("accountabilit") => "Accountability",
        _ => "Procedure",
    };

    private static StpSectionKind SectionKey(string word) => word switch
    {
        "Purpose" => StpSectionKind.Purpose,
        "Scope" => StpSectionKind.Scope,
        "Responsibility" => StpSectionKind.Responsibility,
        _ => StpSectionKind.Accountability,
    };

    // ---- procedure ------------------------------------------------------------------------------------------

    private static void ReadProcedure(List<Entry> entries, StpImportProposalDto proposal)
    {
        var reagents = new List<string>();
        var reagentsSplit = false;
        var notes = new List<string>();
        var inReagents = false;
        StpImportTest test = null;

        foreach (var entry in entries)
        {
            var number = entry.Number;
            if (number is not null && TestNumber().IsMatch(number) && !LooksLikeFusedStep(number, test))
            {
                inReagents = false;
                var title = entry.Lines.FirstOrDefault()?.Trim().TrimEnd(':', ' ').Trim();
                test = new StpImportTest
                {
                    Number = number, Title = title,
                    Confidence = string.IsNullOrEmpty(title) ? StpImportConfidence.Low : StpImportConfidence.High,
                    Flag = string.IsNullOrEmpty(title) ? "The test has no title." : null,
                };
                proposal.Tests.Add(test);
                AddBlocks(test, entry.Lines.Skip(1).ToList(), entry.HadTable);
                continue;
            }
            if (number is not null && (StepNumber().IsMatch(number) || LooksLikeFusedStep(number, test)))
            {
                inReagents = false;
                if (test is null)
                {
                    proposal.Warnings.Add($"Step {number} comes before any 5.N test; it was kept under procedure notes.");
                    notes.Add($"{number} {string.Join(" ", entry.Lines)}".Trim());
                    continue;
                }
                AddStep(test, number, entry.Lines);
                continue;
            }
            if (number is not null && entry.Lines.Count == 0) continue;

            if (test is null)
            {
                // Before the first test: the reagents block, then notes (buffer preparation, "refer to SOP" lines).
                foreach (var line in entry.Lines)
                {
                    var heading = ReagentsHeading().Match(line);
                    if (heading.Success && line.Length < 40)
                    {
                        inReagents = true;
                        if (heading.Groups["rest"].Value.Trim() is { Length: > 0 } inline) AddReagents(inline, reagents, ref reagentsSplit);
                        continue;
                    }
                    if (inReagents && !EndsReagents(line)) { AddReagents(line, reagents, ref reagentsSplit); continue; }
                    inReagents = false;
                    notes.Add(line);
                }
                continue;
            }
            AddBlocks(test, entry.Lines, entry.HadTable);
        }

        if (reagents.Count > 0)
            proposal.Sections.Add(new StpImportSection
            {
                Kind = StpSectionKind.Reagents, Text = string.Join("\n", reagents),
                Confidence = reagentsSplit ? StpImportConfidence.Medium : StpImportConfidence.High,
                Flag = reagentsSplit ? "Reagents printed several to a line were split at the dashes; check each name." : null,
            });
        else
            proposal.Sections.Add(new StpImportSection
            {
                Kind = StpSectionKind.Reagents, Confidence = StpImportConfidence.Medium,
                Flag = "No reagents block was found. Enter the reagents if the STP has them.",
            });
        if (notes.Count > 0)
            proposal.Sections.Add(new StpImportSection
            {
                Kind = StpSectionKind.ProcedureNotes, Text = string.Join("\n", notes), Confidence = StpImportConfidence.Medium,
                Flag = "Unnumbered text before 5.1. Move anything that is a test or a step into the procedure.",
            });
    }

    /// <summary>Skipped test numbers, repeated step numbers and steps numbered under another test are flagged.</summary>
    private static void CheckNumbering(StpImportProposalDto proposal)
    {
        var previous = 0;
        foreach (var test in proposal.Tests)
        {
            if (TestNumber().Match(test.Number ?? "") is { Success: true } match)
            {
                var index = int.Parse(match.Groups["index"].Value);
                if (previous > 0 && index != previous + 1)
                    proposal.Warnings.Add(index <= previous
                        ? $"Test {test.Number} repeats or goes back from 5.{previous}."
                        : $"Test numbers skip from 5.{previous} to {test.Number}.");
                previous = index;
            }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var step in test.Steps.Where(item => item.Kind == StpStepKind.Step && item.Number is not null))
            {
                string flag = null;
                if (!seen.Add(step.Number)) flag = $"Step {step.Number} repeats an earlier step number.";
                else if (test.Number is not null && !step.Number.StartsWith(test.Number + ".", StringComparison.Ordinal))
                    flag = $"Step {step.Number} is numbered outside test {test.Number}.";
                if (flag is null || step.Confidence == StpImportConfidence.Low) continue;
                step.Confidence = StpImportConfidence.Medium;
                step.Flag = step.Flag is null ? flag : $"{step.Flag} {flag}";
            }
        }
    }

    /// <summary>A step number printed without its second point, such as "5.73" for 5.7.3 under test 5.7.</summary>
    private static bool LooksLikeFusedStep(string number, StpImportTest test)
    {
        if (test?.Number is null) return false;
        var match = Regex.Match(number, @"^5\.(?<test>\d)(?<step>\d)$");
        if (!match.Success || test.Number != $"5.{match.Groups["test"].Value}") return false;
        var step = int.Parse(match.Groups["step"].Value);
        return test.Steps.Any(item => item.Number == $"{test.Number}.{step - 1}");
    }

    private static void AddStep(StpImportTest test, string number, List<string> lines)
    {
        var fused = TestNumber().IsMatch(number);
        var printed = number;
        if (fused) number = $"{number[..3]}.{number[3..]}";
        var text = string.Join("\n", lines).Trim();
        var reference = Reference(text);
        test.Steps.Add(new StpImportStep
        {
            Kind = StpStepKind.Step, Number = number, Text = text, ReferenceCode = reference,
            Confidence = fused || text.Length == 0 ? StpImportConfidence.Medium : StpImportConfidence.High,
            Flag = fused ? $"Printed as {printed}; read as {number}." : text.Length == 0 ? "The step has no text." : null,
        });
    }

    /// <summary>
    /// Unnumbered lines inside a test, split at label lines ("Chromatographic Conditions:", "Mobile Phase A:"): conditions
    /// blocks for the chromatographic and dissolution set-up, notes for everything else.
    /// </summary>
    private static void AddBlocks(StpImportTest test, List<string> lines, bool hadTable)
    {
        string heading = null;
        var body = new List<string>();
        void Flush()
        {
            if (heading is null && body.Count == 0) return;
            var text = string.Join("\n", body).Trim();
            var conditions = heading is not null ? ConditionsHeading().IsMatch(heading) : body.Count > 0 && body.All(IsConditionLine);
            test.Steps.Add(new StpImportStep
            {
                Kind = conditions ? StpStepKind.Conditions : StpStepKind.Note,
                Heading = heading ?? (conditions ? "Conditions" : null),
                Text = text.Length == 0 ? null : text,
                ReferenceCode = Reference(text),
                Confidence = hadTable ? StpImportConfidence.Medium : heading is null && conditions ? StpImportConfidence.Medium : StpImportConfidence.High,
                Flag = hadTable ? "A table inside the cell was read as text lines; check the layout."
                    : heading is null && conditions ? "Read as conditions from its wording; check it." : null,
            });
            heading = null;
            body = [];
        }
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (IsLabel(trimmed))
            {
                Flush();
                heading = trimmed.TrimEnd(':', ' ').Trim();
                continue;
            }
            body.Add(trimmed);
        }
        Flush();
    }

    private static bool IsLabel(string line) =>
        line.Length <= 60 && (line.EndsWith(':') || Regex.IsMatch(line, @"^Mobile\s*Phase\s*Composition$", RegexOptions.IgnoreCase));

    private static bool IsConditionLine(string line) => line.Contains(':') && ConditionWord().IsMatch(line);

    /// <summary>A label, a "refer to" note, a long line, or a "Label: sentence" line (buffer preparation) ends the reagents.</summary>
    private static bool EndsReagents(string line) =>
        line.EndsWith(':') || ReferWord().IsMatch(line) || line.Length > 150 || (line.Contains(':') && line.Length > 60);

    private static void AddReagents(string line, List<string> reagents, ref bool split)
    {
        var parts = ReagentSeparator().Split(line).Select(part => part.Trim()).Where(part => part.Length > 0).ToList();
        if (parts.Count > 1) split = true;
        reagents.AddRange(parts);
    }

    [GeneratedRegex(@"\^(?=[wv]/[wv]\b)")]
    private static partial Regex RaisedConcentration();

    /// <summary>"%w/v" is often typed with a raised w/v; the reader marks superscript with "^", which is noise here.</summary>
    private static string Tidy(string line) => RaisedConcentration().Replace(line, string.Empty);

    private static string Reference(string text) =>
        string.IsNullOrEmpty(text) ? null : ReferenceCode().Match(text) is { Success: true } match ? match.Groups["code"].Value : null;

    // ---- keys and counts ------------------------------------------------------------------------------------

    /// <summary>A key from the title (assay, related-substances), made unique within the proposal.</summary>
    public static void AssignKeys(List<StpImportTest> tests)
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var test in tests)
        {
            var key = test.TestKey;
            if (string.IsNullOrEmpty(key) || !used.Add(key))
            {
                var stem = Slug(test.Title) is { Length: > 0 } slug ? slug : $"test-{test.Number?.Replace('.', '-') ?? "x"}";
                key = stem;
                for (var suffix = 2; !used.Add(key); suffix++) key = $"{stem}-{suffix}";
                test.TestKey = key;
            }
        }
    }

    public static string Slug(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return string.Empty;
        var slug = Regex.Replace(title.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        return slug.Length > 80 ? slug[..80].TrimEnd('-') : slug;
    }

    private static int Count(StpImportProposalDto proposal)
    {
        var fields = new[] { proposal.StpNumber, proposal.Title, proposal.RevisionNumber, proposal.SupersedesRevision, proposal.Area,
            proposal.Reference, proposal.EffectiveDate, proposal.ReviewDate, proposal.IssueDate };
        return fields.Count(field => field is { Confidence: not StpImportConfidence.High })
               + proposal.Sections.Count(section => section.Confidence != StpImportConfidence.High)
               + proposal.Tests.Count(test => test.Confidence != StpImportConfidence.High)
               + proposal.Tests.Sum(test => test.Steps.Count(step => step.Confidence != StpImportConfidence.High));
    }

    private static string Label(StpSectionKind kind) => kind switch
    {
        StpSectionKind.Purpose => "1.0 Purpose",
        StpSectionKind.Scope => "2.0 Scope",
        StpSectionKind.Responsibility => "3.0 Responsibility",
        StpSectionKind.Accountability => "4.0 Accountability",
        _ => kind.ToString(),
    };
}

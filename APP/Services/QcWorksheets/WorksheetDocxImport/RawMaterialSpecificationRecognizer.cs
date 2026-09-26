using System.Text.RegularExpressions;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>
/// A raw-material Specification document ("SPECIFICATION", "SPC No.: QCD/SPC/RM/NNN", brief 09).
/// It proposes <b>no template</b>: only Specification characteristics and the header metadata
/// (SPC number, material, revision), bound to the RM-NNN worksheet by <see cref="RawMaterialPairing"/>.
/// <para>
/// Layouts: Test | Specification | Reference, optionally with a leading "No." column (the capsule
/// shell), a full-width caption row ("Microbial Test") over the rows it groups, sub-tests stacked
/// in the Test cell ("Identification Tests: / IR / Chlorides") with one criterion per line, and
/// impurity tables nested in the Specification cell (Levofloxacin has two, cited as "Table 1:" /
/// "Table 2:"). A row it cannot split is kept whole and flagged.
/// </para>
/// </summary>
public sealed partial class RawMaterialSpecificationRecognizer : IArdFamilyRecognizer
{
    internal const string MicrobialGroup = "MICROBIAL";
    internal const string ChemicalGroup = "CHEMICAL";

    public ArdFamily Family => ArdFamily.RawMaterialSpecification;

    // A superscript footnote marker after a name: "N-Desmethyl levofloxacin^a".
    [GeneratedRegex(@"\s*\^[a-z]\s*$")]
    private static partial Regex FootnoteRegex();

    [GeneratedRegex(@"^Table\s*(\d+)\s*:?$", RegexOptions.IgnoreCase)]
    private static partial Regex TableCitationRegex();

    private static readonly string[] LimitHeaders = ["limit", "limits", "specification", "acceptancecriteria"];

    public void Recognize(DocxDocument document, ImportProposalBuilder builder)
    {
        var info = RawMaterialHeader.ReadSpecification(builder.Proposal.FileName, document, builder);
        builder.Proposal.RawMaterial = info;

        foreach (var block in document.Blocks.Where(block => block.Table is not null))
            new Reader(block, builder, info).Run();

        if (builder.Proposal.SpecificationProposals.Count == 0)
            builder.Flag(WorksheetImportFlagCodes.UnrecognizedContent, "No Test | Specification table was found.");

        // A Specification document has no template of its own.
        builder.Proposal.Template = null;
    }

    private sealed class Reader(DocxBlock block, ImportProposalBuilder builder, RawMaterialDocumentInfo info)
    {
        private readonly DocxTable _table = block.Table;
        private int _test = -1, _specification = -1, _reference = -1;
        private string _caption;

        public void Run()
        {
            var header = Enumerable.Range(0, _table.Rows.Count).FirstOrDefault(FindColumns, -1);
            if (header < 0)
                return;

            for (var row = header + 1; row < _table.Rows.Count; row++)
            {
                var origins = _table.OriginCells(row).Where(cell => !ImportText.IsBlank(cell.Text)).ToList();
                if (origins.Count == 0)
                    continue;

                // A full-width row captions the rows under it ("Microbial Test").
                if (_table.OriginCells(row).Count() == 1)
                {
                    _caption = Clean(origins[0].Text);
                    continue;
                }

                ReadRow(row);
            }
        }

        private bool FindColumns(int row)
        {
            var texts = _table.RowTexts(row).Select(ImportText.Canonical).ToList();
            _test = texts.FindIndex(text => text is "test" or "tests" or "testparameter");
            _specification = texts.FindIndex(text => text.StartsWith("specification") || text.StartsWith("limit"));
            _reference = texts.FindIndex(text => text.StartsWith("reference"));
            return _test >= 0 && _specification >= 0;
        }

        private void ReadRow(int row)
        {
            var testCell = OriginAt(row, _test);
            var specCell = OriginAt(row, _specification);
            var location = ImportProposalBuilder.At(block, row, _test);
            var reference = _reference < 0 ? null : Clean(_table.Resolved(row, _reference));
            var testLines = testCell.Lines.Select(Clean).Where(line => line.Length > 0).ToList();
            var specLines = specCell.Lines.Select(Clean).Where(line => line.Length > 0).ToList();
            if (testLines.Count == 0)
                return;

            var parent = testLines[0];
            var subs = testLines.Skip(1).ToList();
            var nested = specCell.NestedTables;

            if (_caption is not null)
            {
                Add(_caption, string.Join(" ", testLines), Join(specLines), reference, location, ImportConfidence.High);
                return;
            }

            if (nested.Count > 0 && subs.Count == 0)
            {
                foreach (var table in nested)
                    AddImpurities(parent, null, table, reference, location);
                return;
            }

            if (subs.Count > 0 && specLines.Count == subs.Count)
            {
                for (var index = 0; index < subs.Count; index++)
                {
                    if (TableCitationRegex().Match(specLines[index]) is { Success: true } citation
                        && int.Parse(citation.Groups[1].Value) is var number && number >= 1 && number <= nested.Count)
                        AddImpurities(parent, subs[index], nested[number - 1], reference, location);
                    else
                        Add(parent, subs[index], specLines[index], reference, location, ImportConfidence.Medium);
                }

                return;
            }

            if (subs.Count > 0)
            {
                builder.Flag(WorksheetImportFlagCodes.UnrecognizedContent,
                    $"'{parent}' lists {subs.Count} sub-tests ({string.Join(", ", subs)}) but {specLines.Count} criteria line(s); "
                    + "kept as one characteristic — split it by hand.", location);
                Add(parent, null, Join(specLines), reference, location, ImportConfidence.Low);
                return;
            }

            if (specLines.Count == 0)
            {
                builder.Flag(WorksheetImportFlagCodes.UnrecognizedContent, $"'{parent}' has no printed specification.", location);
                return;
            }

            Add(parent, null, Join(specLines), reference, location, ImportConfidence.High);
        }

        /// <summary>A nested impurity table: one characteristic per named impurity.</summary>
        private void AddImpurities(string parent, string procedure, DocxTable table, string reference, ImportSourceLocation location)
        {
            for (var row = 0; row < table.Rows.Count; row++)
            {
                var name = FootnoteRegex().Replace(Clean(string.Join(" ", table.Cell(row, 0)?.Lines ?? [])), string.Empty);
                var limit = Clean(string.Join(" ", table.Rows[row].Skip(1).Where(cell => !cell.IsContinuation).SelectMany(cell => cell.Lines)));
                if (name.Length == 0 || limit.Length == 0 || LimitHeaders.Contains(ImportText.Canonical(limit)))
                    continue;

                Add(parent, procedure is null ? name : $"{name} ({procedure})", limit, reference, location, ImportConfidence.Medium);
            }
        }

        private void Add(string testName, string analyte, string criteria, string reference, ImportSourceLocation location, ImportConfidence confidence)
        {
            var microbial = ImportText.Canonical(_caption ?? testName).Contains("microb");
            builder.Proposal.SpecificationProposals.Add(new SpecificationCharacteristicProposal
            {
                TestName = Truncate(testName, 200),
                Analyte = analyte is null ? null : Truncate(analyte, 200),
                AcceptanceCriteria = Truncate(criteria, 2000),
                PrintedCriteria = criteria,
                ActionLimit = Truncate(criteria, 500),
                GroupName = microbial ? MicrobialGroup : ChemicalGroup,
                Reference = reference,
                ProductName = info.MaterialName,
                SpecificationCode = info.SpecificationCode,
                Confidence = confidence,
                Location = location
            });
        }

        private DocxCell OriginAt(int row, int column)
        {
            var cell = _table.Cell(row, column);
            return _table.Cell(cell.OriginRow, cell.OriginColumn);
        }
    }

    /// <summary>Trailing colons and stray backticks ("10ppm`") go; the words stay as printed.</summary>
    private static string Clean(string text) => ImportText.Normalize(text).Trim('`', ' ').TrimEnd(':', ' ');

    private static string Join(IEnumerable<string> lines) => string.Join("; ", lines);

    private static string Truncate(string text, int length) => text.Length <= length ? text : text[..length];
}

using APP.Services.QcWorksheets.WorksheetDocxImport.TestDefinitions;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>
/// The body of one test of a raw-material worksheet. Its cells are buffered until the next test
/// starts; then the test is identified (brief 12) and its <see cref="RawMaterialTestDefinition"/>
/// applied, or — for a test with no definition — the layout is read as in brief 09: "Label: ____"
/// lines, printed W1/W2/W3 formulas, replicate determinations, nested grids, and calculations left
/// for review. Run data is never captured.
/// </summary>
internal sealed partial class RawMaterialSectionBody(ImportProposalBuilder builder, string sectionName, string prefix, ImportSourceLocation titleLocation)
{
    /// <summary>One body cell: its lines and the grids nested in it. A nested layout table is flattened into these.</summary>
    private sealed record BodyCell(IReadOnlyList<string> Lines, IReadOnlyList<DocxTable> Tables, ImportSourceLocation Location);

    private readonly List<BodyCell> _cells = [];
    private readonly bool _solubility = ImportText.Canonical(sectionName).StartsWith("solubility");
    private readonly Dictionary<string, string> _variables = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _conditions = [];
    private ImportSourceLocation _conditionsLocation;
    private bool _calculationAdded;
    private int _shellWeights;
    private ImportSourceLocation _shellWeightsLocation;
    private string _shellWeightsTable;

    public ProposedWorksheetSection Section { get; } = builder.CurrentSection;

    /// <summary>The definition applied to this test; null when the layout was read instead.</summary>
    public RawMaterialTestDefinition Definition { get; private set; }

    public bool HasBodyRows => _cells.Count > 0;

    public bool IsSolubility => _solubility;

    public void ReadCell(DocxBlock block, int row, DocxCell cell) =>
        Buffer(cell, ImportProposalBuilder.At(block, row, cell.Column));

    private void Buffer(DocxCell cell, ImportSourceLocation location)
    {
        var grids = new List<DocxTable>();
        _cells.Add(new BodyCell(cell.Lines, grids, location));

        foreach (var table in cell.NestedTables)
        {
            if (!IsLayoutTable(table))
            {
                grids.Add(table);
                continue;
            }

            // A table used only to lay the test out: its cells are body cells.
            foreach (var inner in table.Rows.SelectMany(row => row).Where(inner => !inner.IsContinuation))
                Buffer(inner, location);
        }
    }

    private static bool IsLayoutTable(DocxTable table) =>
        !TitrationGrid.IsTitration(table) && ReadingsGrid.Read(table) is null
        && table.Rows.SelectMany(row => row).Any(cell => cell.NestedTables.Count > 0 || cell.Lines.Count > 2);

    /// <summary>Reads the buffered cells, by definition or by layout. A section with nothing to bind a Specification to gets a Result.</summary>
    public void Complete(ImportSourceLocation location)
    {
        Definition = RawMaterialTestDefinitions.Resolve(sectionName,
            _cells.SelectMany(cell => cell.Lines).ToList(), _cells.SelectMany(cell => cell.Tables).ToList());

        if (Definition is null)
        {
            foreach (var cell in _cells)
                ReadByLayout(cell);
        }
        else
            ApplyDefinition();

        FlushConditions();
        var section = builder.CurrentSection;
        if (section is null || RawMaterialResultField.Choose(section) is not null)
            return;

        var empty = section.Fields.All(field => field.Type == WorksheetFieldType.Instrument);
        builder.AddField(new ProposedWorksheetField
        {
            FieldKey = $"{prefix}_result", Label = "Result", Type = WorksheetFieldType.LongText, Mode = WorksheetFieldMode.Entry
        }, location ?? titleLocation, empty ? ImportConfidence.High : ImportConfidence.Medium,
            empty ? "Empty observation cell: the analyst writes the result here"
                : "No result line is printed; added so the Specification can bind to this test");
    }

    private void ReadByLayout(BodyCell cell)
    {
        var location = cell.Location;
        var lines = cell.Lines;
        var tablesRead = false;

        for (var index = 0; index < lines.Count; index++)
        {
            var line = ReadInstruments(lines[index], location);
            if (ImportText.IsBlank(line))
                continue;

            if (!ReadShellWeights(line, location))
                FlushShellWeights();

            if (_shellWeights > 0)
                continue;

            if (RawMaterialLines.IsCalculationLabel(line.TrimEnd(':', '=', ' ')))
            {
                // The printed skeleton after "Calculations:" is an explanation, not a formula.
                ReadTables(cell);
                tablesRead = true;
                AddUnprintedCalculation(location, line);
                break;
            }

            if (TryFormulaLine(lines, ref index, location) || TryVariableLine(line, location) || TryDerivedLine(line, location))
                continue;

            if (RawMaterialLines.IsSkeleton(line))
                continue;

            ReadLine(line, lines, index, location);
        }

        FlushShellWeights();
        if (!tablesRead)
            ReadTables(cell);
    }

    private string ReadInstruments(string line, ImportSourceLocation location)
    {
        foreach (var match in RawMaterialLines.InstrumentRegex().Matches(line).Cast<System.Text.RegularExpressions.Match>())
            AddInstrument(match.Groups["label"].Value, match.Groups["code"].Success ? match.Groups["code"].Value : null, location);
        return ImportText.Normalize(RawMaterialLines.InstrumentRegex().Replace(line, " "));
    }

    public void AddInstrument(string printedLabel, string code, ImportSourceLocation location)
    {
        var label = ImportText.Normalize(printedLabel);
        var match = builder.Catalog.FindEquipment(code);
        var field = builder.AddField(new ProposedWorksheetField
        {
            FieldKey = $"{prefix}_{ImportText.SnakeKey(label, 20)}",
            Label = code is null ? label : $"{label} ({code})",
            Type = WorksheetFieldType.Instrument,
            Mode = WorksheetFieldMode.Entry
        }, location, code is null || match is not null ? ImportConfidence.High : ImportConfidence.Medium,
            code is null ? $"'{label}' blank: the instrument is picked at run time"
                : match is null ? "Printed equipment code; not in the register" : $"Printed equipment code; matched {match.Code}");

        builder.Proposal.EquipmentMatches.Add(new ImportEquipmentMatch
        {
            FieldKey = field.FieldKey, PrintedName = label, PrintedCode = code, EquipmentId = match?.Id, MatchedName = match?.Name
        });

        if (code is not null && match is null)
            builder.Flag(WorksheetImportFlagCodes.UnmatchedEquipment, $"'{label}' ({code}) is not in the QC equipment register.", location);
    }

    /// <summary>One ordinary line: attachment, replicates, a method condition, a constant, or blanks.</summary>
    private void ReadLine(string line, IReadOnlyList<string> lines, int index, ImportSourceLocation location)
    {
        var canonical = ImportText.Canonical(line);
        if (canonical.StartsWith("attachprint"))
        {
            AddEntry("attach_print_out", "Attach Print Out", WorksheetFieldType.FileUpload, null, location, "\"Attach Print Out\": an inline attachment");
            return;
        }

        if (RawMaterialLines.TryReplicates(line, out var leading, out var baseLabel, out var markers, out var average))
        {
            foreach (var label in leading)
                AddBlank(new LineBlank(label, null), location);
            AddReplicates(baseLabel, markers, average, location);
            return;
        }

        var firstLabel = line.Split(':', '=')[0];
        if (line.Contains(':') && RawMaterialLines.IsCondition(firstLabel))
        {
            AddCondition(line, location);
            return;
        }

        if (!RawMaterialLines.TryBlanks(line, out var blanks, out var filledLabel, out var filledValue))
        {
            if (filledLabel is not null)
                AddFilled(filledLabel, filledValue, line, location);
            else if (_conditions.Count > 0)
                AddCondition(line, location);
            else if (!RawMaterialLines.IsCaption(line))
                AddBareLine(line, location);
            return;
        }

        foreach (var blank in blanks)
        {
            // "Observed Absorbance:" only captions the wavelength blanks on the next line.
            if (RawMaterialLines.IsCaption(blank.Label))
                continue;
            AddBlank(blank, location);
        }
    }

    private void AddFilled(string label, string value, string line, ImportSourceLocation location)
    {
        if (RawMaterialLines.IsCondition(label))
        {
            AddCondition(line, location);
            return;
        }

        var decision = ParameterTable.Decide(label, value);
        if (decision.Kind == ParameterKind.Constant && ImportText.Canonical(label) != "equivalence" && _conditions.Count > 0)
        {
            AddCondition(line, location);
            return;
        }

        builder.AddDecision(decision with { Key = ImportText.SnakeKey(label, 30) }, location, prefix);
    }

    private void AddCondition(string line, ImportSourceLocation location)
    {
        _conditionsLocation ??= location;
        _conditions.Add(line);
    }

    /// <summary>The printed method conditions become one Constant, as printed; nothing in them is run data.</summary>
    private void FlushConditions()
    {
        if (_conditions.Count == 0)
            return;

        builder.AddField(new ProposedWorksheetField
        {
            FieldKey = $"{prefix}_method_conditions", Label = "Method conditions", Type = WorksheetFieldType.LongText,
            Mode = WorksheetFieldMode.Constant, ConstantValue = string.Join("\n", _conditions)
        }, _conditionsLocation, ImportConfidence.Medium, "Printed chromatographic/method conditions, kept as printed");
        _conditions.Clear();
    }

    private ProposedWorksheetField AddEntry(
        string key, string label, WorksheetFieldType type, string unit, ImportSourceLocation location, string reason,
        ImportConfidence confidence = ImportConfidence.High) =>
        builder.AddField(new ProposedWorksheetField
        {
            FieldKey = $"{prefix}_{key}", Label = label, Type = type, Mode = WorksheetFieldMode.Entry, Unit = unit
        }, location, confidence, reason);
}

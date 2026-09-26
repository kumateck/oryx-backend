using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>
/// Accumulates one file's proposal while a recognizer walks the document: sections, fields
/// with template-wide unique keys (formulas reference keys worksheet-scoped), provenance and
/// flags.
/// </summary>
public sealed class ImportProposalBuilder
{
    private readonly HashSet<string> _keys = new(StringComparer.OrdinalIgnoreCase);

    public ImportProposalBuilder(WorksheetImportProposal proposal, IWorksheetImportCatalog catalog)
    {
        Proposal = proposal;
        Catalog = catalog ?? InMemoryWorksheetImportCatalog.Empty;
        Proposal.Template ??= new ProposedWorksheetTemplate();
    }

    public WorksheetImportProposal Proposal { get; }
    public IWorksheetImportCatalog Catalog { get; }
    public ProposedWorksheetTemplate Template => Proposal.Template;

    public ProposedWorksheetSection CurrentSection { get; private set; }

    /// <summary>Opens a section, or reuses the current one when it has the same name.</summary>
    public ProposedWorksheetSection Section(string name)
    {
        name = string.IsNullOrWhiteSpace(name) ? "General" : ImportText.Normalize(name);
        if (name.Length > 200)
            name = name[..200];

        if (CurrentSection is not null && string.Equals(CurrentSection.Name, name, StringComparison.OrdinalIgnoreCase))
            return CurrentSection;

        CurrentSection = Template.Sections.FirstOrDefault(section =>
                             string.Equals(section.Name, name, StringComparison.OrdinalIgnoreCase))
                         ?? AddSection(name);
        return CurrentSection;
    }

    private ProposedWorksheetSection AddSection(string name)
    {
        var section = new ProposedWorksheetSection { Name = name, Order = Template.Sections.Count + 1 };
        Template.Sections.Add(section);
        return section;
    }

    /// <summary>A key unique across the whole template: "sterility", "sterility_2", …</summary>
    public string AllocateKey(string preferred)
    {
        var baseKey = string.IsNullOrWhiteSpace(preferred) ? "field" : preferred;
        if (baseKey.Length > 90)
            baseKey = baseKey[..90].TrimEnd('_');

        var key = baseKey;
        for (var suffix = 2; !_keys.Add(key); suffix++)
            key = $"{baseKey}_{suffix}";
        return key;
    }

    public bool IsKeyTaken(string key) => _keys.Contains(key);

    /// <summary>Adds a field to the current section, allocating its key from FieldKey (or the label).</summary>
    public ProposedWorksheetField AddField(
        ProposedWorksheetField field, ImportSourceLocation location, ImportConfidence confidence, string reason)
    {
        var section = CurrentSection ?? Section(null);
        field.FieldKey = AllocateKey(string.IsNullOrWhiteSpace(field.FieldKey) ? ImportText.SnakeKey(field.Label) : field.FieldKey);
        if (field.Label?.Length > 500)
            field.Label = field.Label[..500];
        field.Order = section.Fields.Count + 1;
        section.Fields.Add(field);

        Proposal.FieldProvenance.Add(new ImportFieldProvenance
        {
            FieldKey = field.FieldKey, Location = location, Confidence = confidence, Reason = reason
        });
        return field;
    }

    /// <summary>Adds a field from a label/value decision; HeaderData adds nothing.</summary>
    public ProposedWorksheetField AddDecision(ParameterDecision decision, ImportSourceLocation location, string keyPrefix = null)
    {
        if (decision.Kind == ParameterKind.HeaderData)
            return null;

        if (decision.FlagCode is not null)
            Flag(decision.FlagCode, $"{decision.Label}: {decision.Reason}", location);

        return AddField(new ProposedWorksheetField
        {
            FieldKey = keyPrefix is null ? decision.Key : $"{keyPrefix}_{decision.Key}",
            Label = decision.Label,
            Type = decision.Type,
            Mode = decision.Kind == ParameterKind.Constant ? WorksheetFieldMode.Constant : WorksheetFieldMode.Entry,
            ConstantValue = decision.Kind == ParameterKind.Constant ? decision.ConstantValue : null,
            Unit = decision.Unit
        }, location, decision.Confidence, decision.Reason);
    }

    /// <summary>Adds a Table field whose columns come from a <see cref="DataGrid"/>.</summary>
    public ProposedWorksheetField AddTable(
        string key, string label, IReadOnlyList<GridColumn> columns, ImportSourceLocation location, string reason)
    {
        var field = AddField(new ProposedWorksheetField
        {
            FieldKey = key,
            Label = label,
            Type = WorksheetFieldType.Table,
            Mode = WorksheetFieldMode.Entry,
            ColumnDefinitions = ColumnDefinitionsJson.Write(columns)
        }, location, columns.Any(column => column.Confidence == ImportConfidence.Low) ? ImportConfidence.Medium : ImportConfidence.High, reason);

        foreach (var column in columns)
        {
            Proposal.FieldProvenance.Add(new ImportFieldProvenance
            {
                FieldKey = field.FieldKey,
                ColumnKey = column.Key,
                Location = new ImportSourceLocation { Block = location.Block, Table = location.Table, Column = column.SourceColumn },
                Confidence = column.Confidence,
                Reason = column.Reason
            });

            if (column.FlagCode is not null)
                Flag(column.FlagCode, $"{label} / {column.Label}: {column.Reason}", location);
        }

        return field;
    }

    public void Flag(string code, string message, ImportSourceLocation location = null) =>
        Proposal.Flags.Add(new WorksheetImportFlag { Code = code, Message = message, Location = location });

    /// <summary>Drops empty sections and renumbers what is left.</summary>
    public void Complete()
    {
        Template.Sections.RemoveAll(section => section.Fields.Count == 0);
        for (var index = 0; index < Template.Sections.Count; index++)
            Template.Sections[index].Order = index + 1;
    }

    public static ImportSourceLocation At(DocxBlock block, int? row = null, int? column = null) =>
        new() { Block = block.Index, Table = block.Table?.Ordinal, Row = row, Column = column };
}

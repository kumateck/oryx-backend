using System.Globalization;
using System.Text.Json;
using APP.Services.QcWorksheets;
using APP.Services.QcWorksheets.WorksheetDocxImport;
using DOMAIN.Entities.QcWorksheets;
using Xunit;

namespace APP.Tests.QcWorksheets.WorksheetImport;

/// <summary>
/// Runs only when <c>QC_RM_STUDY_DIR</c> points at the study corpus of raw-material worksheets
/// (brief 12). The files carry staff and supplier names and batch data and are never committed, so
/// CI skips these; <see cref="RawMaterialTestDefinitionTests"/> covers every definition and variant
/// with synthetic documents.
/// </summary>
public sealed class StudyCorpusFactAttribute : FactAttribute
{
    public const string Variable = "QC_RM_STUDY_DIR";

    public StudyCorpusFactAttribute()
    {
        var directory = Environment.GetEnvironmentVariable(Variable);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            Skip = $"{Variable} is not set to the raw-material study corpus directory.";
    }
}

internal sealed record StudyFile(string FileName, WorksheetImportProposal Proposal);

internal static class StudyCorpus
{
    private static readonly Lazy<List<StudyFile>> Files = new(() =>
    {
        var root = Environment.GetEnvironmentVariable(StudyCorpusFactAttribute.Variable)!;
        return Directory.EnumerateFiles(root, "*.docx")
            .Where(path => !Path.GetFileName(path).StartsWith("~$", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path =>
            {
                using var stream = File.OpenRead(path);
                return new StudyFile(Path.GetFileName(path), WorksheetDocxImportService.Propose(Path.GetFileName(path), stream, InMemoryWorksheetImportCatalog.Empty));
            })
            .ToList();
    });

    /// <summary>Every .docx of the corpus, each proposed on its own (no Specification is paired).</summary>
    public static List<StudyFile> Load() => Files.Value;

    public static IEnumerable<StudyFile> Worksheets() =>
        Load().Where(file => file.Proposal.Family == ArdFamily.RawMaterialChemical && file.Proposal.Template is not null);
}

/// <summary>
/// Runs a proposed template through the real <see cref="QcWorksheetCalculator"/> with sample
/// numbers, so a formula that parses but cannot be evaluated (a missing field, a row with no
/// value) fails a test instead of a submission.
/// </summary>
internal static class ProposalCalculator
{
    /// <param name="scalars">Field key → value; any other numeric Entry field gets a distinct sample number.</param>
    /// <param name="cells">"table.column[row]" → value; any other numeric cell of a fixed row gets a sample number.</param>
    public static (Dictionary<string, double> Values, string Failure) Evaluate(
        ProposedWorksheetTemplate template, IReadOnlyDictionary<string, double>? scalars = null, IReadOnlyDictionary<string, double>? cells = null)
    {
        var proposed = template.Sections.SelectMany(section => section.Fields).ToList();
        var fields = proposed.Select((field, index) => new WorksheetField
        {
            Id = Guid.NewGuid(), FieldKey = field.FieldKey, Label = field.Label, Type = field.Type, Mode = field.Mode, Order = index + 1,
            FormulaExpression = field.FormulaExpression, ColumnDefinitions = field.ColumnDefinitions, ConstantValue = field.ConstantValue
        }).ToList();

        var values = new List<WorksheetFieldValue>();
        var sample = 0;
        string Next() => (2.5 + 1.37 * sample++).ToString(CultureInfo.InvariantCulture);

        foreach (var field in proposed.Where(field => field.Mode == WorksheetFieldMode.Entry))
        {
            if (field.Type == WorksheetFieldType.Table)
            {
                var columns = JsonDocument.Parse(field.ColumnDefinitions).RootElement.EnumerateArray().ToList();
                var rows = columns.Where(column => column.TryGetProperty("fixedValues", out _))
                    .Select(column => column.GetProperty("fixedValues").GetArrayLength()).DefaultIfEmpty(0).Max();
                foreach (var column in columns.Where(column => !column.TryGetProperty("fixedValues", out _) && !column.TryGetProperty("mode", out _)
                                                               && column.GetProperty("type").GetString() == nameof(WorksheetFieldType.Number)))
                {
                    var key = column.GetProperty("key").GetString()!;
                    for (var row = 0; row < rows; row++)
                        values.Add(new WorksheetFieldValue
                        {
                            FieldKey = field.FieldKey, ColumnKey = key, RowIndex = row,
                            Value = cells is not null && cells.TryGetValue($"{field.FieldKey}.{key}[{row}]", out var cell)
                                ? cell.ToString(CultureInfo.InvariantCulture) : Next()
                        });
                }

                continue;
            }

            if (field.Type is WorksheetFieldType.Number or WorksheetFieldType.Measurement)
                values.Add(new WorksheetFieldValue
                {
                    FieldKey = field.FieldKey,
                    Value = scalars is not null && scalars.TryGetValue(field.FieldKey, out var value) ? value.ToString(CultureInfo.InvariantCulture) : Next()
                });
        }

        if (!QcWorksheetCalculator.TryEvaluateAll(fields, values, out var computed, out var failure))
            return ([], $"{failure.Field.FieldKey}{(failure.ColumnKey is null ? string.Empty : $".{failure.ColumnKey}[{failure.RowIndex}]")}: {failure.Reason}");

        return (computed.ToDictionary(
            item => item.ColumnKey is null ? item.Field.FieldKey : $"{item.Field.FieldKey}.{item.ColumnKey}[{item.RowIndex}]", item => item.Number), null!);
    }
}

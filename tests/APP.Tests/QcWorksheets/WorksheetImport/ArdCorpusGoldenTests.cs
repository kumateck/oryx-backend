using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using APP.Services.QcWorksheets;
using APP.Services.QcWorksheets.WorksheetDocxImport;
using DOMAIN.Entities.QcWorksheets;
using Xunit;
using Xunit.Abstractions;

namespace APP.Tests.QcWorksheets.WorksheetImport;

public class ArdCorpusGoldenTests(ITestOutputHelper output)
{
    private static readonly WorksheetFieldType[] ChoiceTypes =
        [WorksheetFieldType.Select, WorksheetFieldType.MultiSelect, WorksheetFieldType.GrowthObservation];

    [CorpusFact]
    public void Every_file_classifies_into_its_family()
    {
        var corpus = ArdCorpus.Load();

        Assert.Equal(49, corpus.Count);
        foreach (var file in corpus)
            Assert.True(file.Expected == file.Proposal.Family, $"{file.RelativePath}: expected {file.Expected}, got {file.Proposal.Family}");

        Assert.Equal(25, corpus.Count(file => file.Expected == ArdFamily.CultureMedia));
        Assert.Equal(8, corpus.Count(file => file.Expected == ArdFamily.CompletedCertificate));
        Assert.Equal(8, corpus.Count(file => file.Expected == ArdFamily.EnvironmentalMonitoring));
        Assert.Equal(2, corpus.Count(file => file.Expected == ArdFamily.PurifiedWater));
        Assert.Equal(6, corpus.Count(file => file.Expected == ArdFamily.ProductMicro));
    }

    [CorpusFact]
    public void Certificates_are_rejected_and_every_family_has_a_recognizer()
    {
        foreach (var file in ArdCorpus.Load())
        {
            Assert.DoesNotContain(file.Proposal.Flags, flag => flag.Code == WorksheetImportFlagCodes.RecognizerPending);
            if (file.Expected == ArdFamily.CompletedCertificate)
            {
                Assert.Null(file.Proposal.Template);
                Assert.Contains(file.Proposal.Flags, flag => flag.Code == WorksheetImportFlagCodes.CompletedOutputNotTemplate);
            }
            else if (!file.Proposal.Flags.Any(flag => flag.Code == WorksheetImportFlagCodes.SharedTemplateInBatch))
            {
                Assert.NotNull(file.Proposal.Template);
            }
        }
    }

    [CorpusFact]
    public void Page_split_EM_result_tables_are_stitched_into_one()
    {
        foreach (var file in ArdCorpus.Load().Where(file => file.Expected == ArdFamily.EnvironmentalMonitoring))
        {
            var roomTables = file.Document.Tables.Count(table => ImportText.Canonical(table.Resolved(0, 0)).StartsWith("roomno"));
            Assert.True(roomTables == 1, $"{file.RelativePath}: {roomTables} room tables after stitching");
        }
    }

    [CorpusFact]
    public void Old_media_forms_are_blocked_only_when_a_newer_twin_exists()
    {
        var media = ArdCorpus.Load().Where(file => file.Expected == ArdFamily.CultureMedia).ToList();
        var superseded = media.Where(file => file.Proposal.FormatVersion == nameof(CultureMediaFormat.Superseded)).ToList();
        bool Has(CorpusFile file, string code) => file.Proposal.Flags.Any(flag => flag.Code == code);

        output.WriteLine($"Old-format media files: {superseded.Count} of {media.Count}");
        foreach (var file in superseded)
            output.WriteLine($"  {file.RelativePath}: {(Has(file, WorksheetImportFlagCodes.SupersededFormatBlocked) ? "SupersededFormatBlocked" : "SupersededFormat (warning)")}");

        foreach (var file in media)
        {
            var isOld = file.Proposal.FormatVersion == nameof(CultureMediaFormat.Superseded);
            var flags = new[] { WorksheetImportFlagCodes.SupersededFormat, WorksheetImportFlagCodes.SupersededFormatBlocked }.Count(code => Has(file, code));
            Assert.True(flags == (isOld ? 1 : 0), $"{file.RelativePath}: {flags} supersession flags");
            Assert.NotEqual(nameof(CultureMediaFormat.Unknown), file.Proposal.FormatVersion);
            Assert.NotNull(file.Proposal.Medium);

            // Media limits stay on the sheet: no media sheet proposes a Specification.
            Assert.Empty(file.Proposal.SpecificationProposals);
        }

        Assert.Equal(11, superseded.Count);
        Assert.Equal(10, superseded.Count(file => Has(file, WorksheetImportFlagCodes.SupersededFormatBlocked)));
        var warned = Assert.Single(superseded, file => Has(file, WorksheetImportFlagCodes.SupersededFormat));
        Assert.EndsWith("EEBM.docx", warned.RelativePath);
    }

    [CorpusFact]
    public void Standard_zones_stay_on_the_sheet_as_fixed_columns()
    {
        var sheet = Assert.Single(ArdCorpus.Load(), file => file.RelativePath.EndsWith("Mueller Hinton Agar.docx"));
        var tables = sheet.Proposal.Template.Sections.SelectMany(section => section.Fields)
            .Where(field => field.FieldKey.StartsWith("antibiotic_sensitivity_")).ToList();

        Assert.Equal(3, tables.Count);
        foreach (var table in tables)
        {
            var standard = Columns(table.ColumnDefinitions).Single(column => column.GetProperty("key").GetString() == "standardZone");
            Assert.True(standard.GetProperty("fixedValues").GetArrayLength() > 0);
        }
    }

    [CorpusFact]
    public void New_media_forms_hold_their_invariants()
    {
        foreach (var file in ArdCorpus.Load().Where(file =>
                     file.Expected == ArdFamily.CultureMedia && file.Proposal.FormatVersion == nameof(CultureMediaFormat.New)))
        {
            var template = file.Proposal.Template;
            Assert.NotNull(template);
            var fields = template.Sections.SelectMany(section => section.Fields).ToList();

            // 1. No run-data value in any constant, fixed cell, label, name or code.
            var runData = RunDataValues(file.Document);
            var constantTexts = fields.Where(field => field.Mode == WorksheetFieldMode.Constant).Select(field => field.ConstantValue!)
                .Concat(fields.SelectMany(field => FixedValues(field.ColumnDefinitions)))
                .Concat(fields.Select(field => field.Label))
                .Concat([template.Name, template.Code])
                .ToList();
            var leaks = runData
                .SelectMany(value => constantTexts.Where(text => Leaks(text, value))
                    .Select(text => $"'{value}' in '{text}'"))
                .ToList();
            Assert.True(leaks.Count == 0, $"{file.RelativePath}: run data leaked: {string.Join("; ", leaks)}");
            Assert.DoesNotContain(fields, field => field.Mode == WorksheetFieldMode.Constant && RunDataLabels.LooksLikeRunData(field.ConstantValue));
            // A fill-in leader is a blank; it can never have become constant text.
            Assert.DoesNotContain(fields, field => field.Mode == WorksheetFieldMode.Constant && ImportText.HasLeader(field.ConstantValue));

            // 2. Every choice field has at least two distinct options.
            foreach (var field in fields.Where(field => ChoiceTypes.Contains(field.Type)))
                Assert.True(field.Options?.Distinct().Count() >= 2, $"{file.RelativePath}: {field.FieldKey} has too few options");

            // 3. Strain rows are fixed-column values of the cultural response table.
            var response = fields.SingleOrDefault(field => field.FieldKey == "cultural_response");
            Assert.True(response is not null, $"{file.RelativePath}: no cultural_response table");
            var organisms = Columns(response!.ColumnDefinitions).First(column => column.GetProperty("type").GetString() == nameof(WorksheetFieldType.Organism));
            Assert.True(organisms.TryGetProperty("fixedValues", out var strains) && strains.GetArrayLength() > 0, $"{file.RelativePath}: strains not fixed");

            // 4. Structurally savable: keys unique, constants filled, formulas parse.
            Assert.Equal(fields.Count, fields.Select(field => field.FieldKey).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.DoesNotContain(fields, field => field.Mode == WorksheetFieldMode.Constant && string.IsNullOrWhiteSpace(field.ConstantValue));
            foreach (var formula in fields.SelectMany(field => Columns(field.ColumnDefinitions))
                         .Where(column => column.TryGetProperty("formula", out _)).Select(column => column.GetProperty("formula").GetString()))
                Assert.True(QcFormulaEvaluator.Analyze(formula).IsValid, formula);

            Assert.Contains(fields, field => field.FieldKey == CultureMediaKeys.Medium && field.Type == WorksheetFieldType.Reagent);
            Assert.Contains(fields, field => field.FieldKey == CultureMediaKeys.Remark && field.Options is { Count: 2 });
        }
    }

    [CorpusFact]
    public void Coverage_report()
    {
        var report = new StringBuilder().AppendLine("file | family | format | fields | High | Medium | Low | %High | specs | flags");
        foreach (var file in ArdCorpus.Load())
        {
            var provenance = file.Proposal.FieldProvenance;
            var high = provenance.Count(item => item.Confidence == ImportConfidence.High);
            var medium = provenance.Count(item => item.Confidence == ImportConfidence.Medium);
            var low = provenance.Count(item => item.Confidence == ImportConfidence.Low);
            var fields = file.Proposal.Template?.Sections.Sum(section => section.Fields.Count) ?? 0;
            var percent = provenance.Count == 0 ? "-" : $"{100.0 * high / provenance.Count:F0}%";
            var flags = string.Join(",", file.Proposal.Flags.GroupBy(flag => flag.Code).Select(group => $"{group.Key}x{group.Count()}"));
            report.AppendLine($"{file.RelativePath} | {file.Proposal.Family} | {file.Proposal.FormatVersion ?? "-"} | {fields} | {high} | {medium} | {low} | {percent} | {file.Proposal.SpecificationProposals.Count} | {flags}");
        }

        output.WriteLine(report.ToString());

        var dump = Environment.GetEnvironmentVariable("QC_ARD_PROPOSAL_DUMP");
        if (!string.IsNullOrWhiteSpace(dump))
        {
            Directory.CreateDirectory(dump);
            foreach (var file in ArdCorpus.Load())
                File.WriteAllText(Path.Combine(dump, file.RelativePath.Replace(Path.DirectorySeparatorChar, '_') + ".json"),
                    JsonSerializer.Serialize(WithoutSource(file.Proposal), new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private static WorksheetImportProposal WithoutSource(WorksheetImportProposal proposal)
    {
        proposal.Source = null;
        return proposal;
    }

    /// <summary>
    /// Long values leak as substrings; short ones ("02" under Revision No.) only as a whole
    /// token, or "ATCC 10231" would count as leaking revision "02".
    /// </summary>
    private static bool Leaks(string text, string value) =>
        value.Length >= 5
            ? text.Contains(value, StringComparison.OrdinalIgnoreCase)
            : Regex.IsMatch(text, $@"(?<![\w/]){Regex.Escape(value)}(?![\w/])", RegexOptions.IgnoreCase);

    /// <summary>Values printed under run-data labels in the metadata table (issue no., batch no., issuer, dates).</summary>
    private static List<string> RunDataValues(DocxDocument document)
    {
        var metadata = document.Tables.First(table => ImportText.Canonical(string.Join(" ", table.RowTexts(0))).Contains("culturemediumname"));
        return metadata.Rows.SelectMany(row => row).Where(cell => !cell.IsContinuation && cell.Text.Length > 0)
            .SelectMany(cell => ParameterTable.Segments(cell.Text))
            .Where(segment => RunDataLabels.TryMatch(segment.Label, out _) && !ImportText.IsBlank(segment.Value))
            .Select(segment => segment.Value)
            .ToList();
    }

    private static IEnumerable<JsonElement> Columns(string? definitions) =>
        string.IsNullOrWhiteSpace(definitions) ? [] : JsonDocument.Parse(definitions).RootElement.EnumerateArray().ToList();

    private static IEnumerable<string> FixedValues(string? definitions) =>
        Columns(definitions).Where(column => column.TryGetProperty("fixedValues", out _))
            .SelectMany(column => column.GetProperty("fixedValues").EnumerateArray().Select(value => value.GetString() ?? string.Empty));
}

using System.Text.RegularExpressions;
using APP.Services.QcWorksheets.WorksheetDocxImport;
using DOMAIN.Entities.QcWorksheets;
using Xunit;
using Xunit.Abstractions;

namespace APP.Tests.QcWorksheets.WorksheetImport;

/// <summary>The eight EM worksheets (and their COAs, as cross-checks). Runs only with <c>QC_ARD_CORPUS_DIR</c> set.</summary>
public class ArdCorpusEmTests(ITestOutputHelper output)
{
    private static List<CorpusFile> Worksheets() =>
        ArdCorpus.Load().Where(file => file.Expected == ArdFamily.EnvironmentalMonitoring).ToList();

    private static List<ProposedWorksheetField> Fields(CorpusFile file) =>
        file.Proposal.Template.Sections.SelectMany(section => section.Fields).ToList();

    /// <summary>The printed room rows of the stitched room table, header rows excluded.</summary>
    private static List<(string Code, string Name)> PrintedRooms(CorpusFile file)
    {
        var table = file.Document.Tables.Single(table => ImportText.Canonical(table.Resolved(0, 0)).StartsWith("roomno"));
        return Enumerable.Range(0, table.Rows.Count)
            .Select(row => (Code: table.Resolved(row, 0), Name: table.Resolved(row, 1)))
            .Where(room => !ImportText.IsBlank(room.Code) && !ImportText.Canonical(room.Code).StartsWith("roomno"))
            .ToList();
    }

    [CorpusFact]
    public void Every_room_is_a_sampling_point_and_none_is_a_template_row()
    {
        var worksheets = Worksheets();
        Assert.Equal(8, worksheets.Count);

        foreach (var file in worksheets)
        {
            var printed = PrintedRooms(file);
            var points = file.Proposal.SamplingPointProposals;
            output.WriteLine($"{file.RelativePath}: {printed.Count} printed room rows → {points.Count} sampling points "
                             + $"({printed.Select(room => room.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count()} distinct codes)");

            var codes = points.Select(point => ImportText.Canonical(point.Code)).ToHashSet();
            Assert.All(printed, room => Assert.Contains(ImportText.Canonical(room.Code), codes));
            Assert.All(points, point => Assert.Equal(SamplingPointType.Environmental, point.Type));
            Assert.All(points, point => Assert.False(string.IsNullOrWhiteSpace(point.Area)));

            var fields = Fields(file);
            Assert.DoesNotContain(fields, field => field.Type == WorksheetFieldType.Table);
            // Instrument labels are excluded: the Microbiology Lab's LAF benches are both sampling
            // points and equipment used in the test ("Laminar Air Flow Unit (QCD/EQT/LAF/001)").
            var texts = fields.Where(field => field.Type != WorksheetFieldType.Instrument)
                .SelectMany(field => new[] { field.Label, field.ConstantValue ?? string.Empty }).ToList();
            foreach (var point in points.Where(point => point.Code.Length > 3))
                Assert.DoesNotContain(texts, text => Regex.IsMatch(text, $@"(?<![\w/]){Regex.Escape(point.Code)}(?![\w])"));

            var result = Assert.Single(fields, field => field.FieldKey == EnvironmentalMonitoringRecognizer.ResultKey);
            Assert.Equal((WorksheetFieldType.ColonyCount, WorksheetFieldMode.Entry, "CFU/4Hrs"), (result.Type, result.Mode, result.Unit));
        }
    }

    [CorpusFact]
    public void The_area_templates_differ_only_in_name_equipment_and_constants()
    {
        var comparable = Worksheets().ToDictionary(file => file.RelativePath, file => Fields(file)
            .Where(field => field.Type is not (WorksheetFieldType.Instrument or WorksheetFieldType.Reagent))
            .Select(field => string.Join(" | ", field.FieldKey, field.Type, field.Mode, field.Unit,
                field.Mode == WorksheetFieldMode.Constant ? null : field.Label, string.Join("/", field.Options ?? [])))
            .ToList());

        var first = comparable.First();
        foreach (var other in comparable.Skip(1))
            Assert.True(first.Value.SequenceEqual(other.Value), $"{other.Key} differs from {first.Key} in structure");

        // The per-area Constants: report which ones actually differ between the eight sheets.
        var constants = Worksheets().SelectMany(file => Fields(file).Where(field => field.Mode == WorksheetFieldMode.Constant
                && field.Type != WorksheetFieldType.Instructions)
            .Select(field => (field.FieldKey, field.ConstantValue)))
            .GroupBy(item => item.FieldKey)
            .Select(group => $"{group.Key}: {string.Join(" | ", group.Select(item => item.ConstantValue).Distinct())}");
        foreach (var line in constants)
            output.WriteLine(line);
    }

    [CorpusFact]
    public void Specification_tiers_group_the_points_and_take_alert_limits_from_the_coas()
    {
        foreach (var file in Worksheets())
        {
            var proposal = file.Proposal;
            Assert.NotEmpty(proposal.SamplingPointGroupProposals);
            Assert.Equal(proposal.SamplingPointGroupProposals.Select(group => group.Name), proposal.SpecificationProposals.Select(spec => spec.GroupName));
            Assert.All(proposal.SpecificationProposals, spec =>
            {
                Assert.Equal(EnvironmentalMonitoringRecognizer.ResultKey, spec.SourceFieldKey);
                Assert.Equal(spec.AcceptanceCriteria, spec.ActionLimit);
            });

            var rooms = proposal.SpecificationProposals.Single(spec => spec.GroupName == "Rooms");
            Assert.Equal("NMT 100 cfu/4Hrs", rooms.ActionLimit);
            Assert.Equal("NMT 80 cfu/4Hrs", rooms.AlertLimit);
            Assert.StartsWith("from COA", rooms.AlertLimitSource);

            // Every point is in a tier, or flagged.
            var ungrouped = proposal.SamplingPointProposals.Count(point => point.GroupName is null);
            Assert.Equal(ungrouped, proposal.Flags.Count(flag => flag.Code == WorksheetImportFlagCodes.SamplingPointWithoutLimit));

            output.WriteLine($"{file.RelativePath}: " + string.Join("; ", proposal.SpecificationProposals.Select(spec =>
                $"{spec.GroupName} Action {spec.ActionLimit} / Alert {spec.AlertLimit ?? "-"} ({proposal.SamplingPointGroupProposals.Single(group => group.Name == spec.GroupName).PointCodes.Count} points)")));
        }
    }
}

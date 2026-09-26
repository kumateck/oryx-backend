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
    public void Every_room_is_a_sampling_point_and_the_one_template_has_no_room_rows()
    {
        var worksheets = Worksheets();
        Assert.Equal(8, worksheets.Count);

        // Uploaded together, the eight area sheets propose the shared template exactly once.
        var carrier = Assert.Single(worksheets, file => file.Proposal.Template is not null);
        Assert.Equal(EnvironmentalMonitoringRecognizer.TemplateCode, carrier.Proposal.Template.Code);
        Assert.All(worksheets.Where(file => file != carrier), file =>
        {
            Assert.Equal(WorksheetImportFlagCodes.SharedTemplateInBatch, file.Proposal.Flags[0].Code);
            Assert.Equal(carrier.Proposal.FileName, file.Proposal.SharedTemplate.CarriedBy);
        });

        var fields = Fields(carrier);
        Assert.DoesNotContain(fields, field => field.Type == WorksheetFieldType.Table);
        var result = Assert.Single(fields, field => field.FieldKey == EnvironmentalMonitoringRecognizer.ResultKey);
        Assert.Equal((WorksheetFieldType.ColonyCount, WorksheetFieldMode.Entry, "CFU/4Hrs"), (result.Type, result.Mode, result.Unit));

        // Instrument labels are excluded: the Microbiology Lab's LAF benches are both sampling
        // points and equipment used in the test ("Laminar Air Flow Unit (QCD/EQT/LAF/001)").
        var texts = fields.Where(field => field.Type != WorksheetFieldType.Instrument)
            .SelectMany(field => new[] { field.Label, field.ConstantValue ?? string.Empty }).ToList();

        var total = 0;
        foreach (var file in worksheets)
        {
            var printed = PrintedRooms(file);
            var points = file.Proposal.SamplingPointProposals;
            total += points.Count;
            output.WriteLine($"{file.RelativePath}: {printed.Count} printed room rows → {points.Count} sampling points "
                             + $"(area '{points[0].Area}')");

            var codes = points.Select(point => ImportText.Canonical(point.Code)).ToHashSet();
            Assert.All(printed, room => Assert.Contains(ImportText.Canonical(room.Code), codes));
            Assert.All(points, point => Assert.Equal(SamplingPointType.Environmental, point.Type));
            Assert.All(points, point => Assert.False(string.IsNullOrWhiteSpace(point.Area)));
            foreach (var point in points.Where(point => point.Code.Length > 3))
                Assert.DoesNotContain(texts, text => Regex.IsMatch(text, $@"(?<![\w/]){Regex.Escape(point.Code)}(?![\w])"));
        }

        Assert.Equal(222, total);
        var tablet = worksheets.Single(file => file.RelativePath.Contains("(Tablet)"));
        Assert.Contains(tablet.Proposal.Flags, flag => flag.Message.Contains("'SF-56' is printed for two different rooms"));
        Assert.Equal(7, tablet.Proposal.Flags.Count(flag => flag.Message.Contains("listed twice") || flag.Message.Contains("two different rooms")));
    }

    [CorpusFact]
    public void Each_area_sheet_uploaded_alone_proposes_the_same_template()
    {
        var root = Environment.GetEnvironmentVariable(CorpusFactAttribute.Variable)!;
        var alone = Worksheets().Select(file =>
        {
            using var stream = File.OpenRead(Path.Combine(root, file.RelativePath));
            var proposal = WorksheetDocxImportService.Propose(file.RelativePath, stream, InMemoryWorksheetImportCatalog.Empty);
            WorksheetDocxImportService.ApplyBatchRules([proposal], InMemoryWorksheetImportCatalog.Empty);
            return (file.RelativePath, Json: System.Text.Json.JsonSerializer.Serialize(WorksheetImportTemplateMapper.ToCreateRequest(proposal.Template)));
        }).ToList();

        foreach (var (path, json) in alone.Skip(1))
            Assert.True(json == alone[0].Json, $"{path} proposes a different template from {alone[0].RelativePath}");
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

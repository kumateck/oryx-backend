using APP.Services.QcWorksheets;
using APP.Services.QcWorksheets.WorksheetDocxImport;
using DocumentFormat.OpenXml;
using DOMAIN.Entities.QcWorksheets;
using Xunit;
using static APP.Tests.QcWorksheets.WorksheetImport.TestDocx;

namespace APP.Tests.QcWorksheets.WorksheetImport;

public class PurifiedWaterRecognizerTests
{
    private const string Header = "QUALITY CONTROL DEPARTMENT\tMICROBIOLOGY ANALYTICAL WORKSHEET\tWATER\tPage 1 of 8";

    private static IEnumerable<OpenXmlElement> Sheet(params string[][] points) =>
    [
        Grid(["Issue no: 26/0001", "WATER", "Sampled by: Date Sampled:"], ["Issue date: 01/02/2026", "Issued by: Test Issuer", "Analysis Start Date: Analysis End Date:"]),
        P("A. MICROBIAL ENUMERATION TESTS"),
        P("Growth Promotion and Sterility of the Culture Medium"),
        P("Test Agar"),
        P("Medium Batch no.: QCD/26/007/0000000001"),
        P("Results: Refer to Culture Media Batch Data sheet serial numbered: QCD/MIC/BDTA/26/006"),
        P("Remark: Complies / Does not comply"),
        P("Testing of Samples"),
        P("Microbial Count"),
        Grid(["Method", "Membrane filtration"], ["Volume of water sample filtered", "100mL"], ["Incubation Period", "5 days"]),
        P("Results"),
        T(new[]
            {
                R(C("Sampling Point Code", merge: VMerge.Restart), C("Sample Detail", merge: VMerge.Restart), C("Result", span: 2), C("AR. Number", merge: VMerge.Restart)),
                R(C(merge: VMerge.Continue), C(merge: VMerge.Continue), C("CFU/ 100mL"), C("CFU/ mL"), C(merge: VMerge.Continue))
            }
            .Concat(points.Select(point => R(C(point[0]), C(point[1]), C(), C(), C("QCD/WT/26/0" + point[0].Replace(" ", "")))))
            .ToArray()),
        P("Specifications: SP1 – NMT 500cfu/mL"),
        P("SP2-SP3, NSP1 – NMT 80 cfu/mL"),
        P("Remark: Comply / Do not comply"),
        P("B. TESTS FOR SPECIFIED MICROORGANISMS"),
        P("TESTING OF PRODUCTS"),
        P("Absence of Escherichia coli"),
        P("ii. Selection"),
        Grid(["Medium:", "Test Broth"], ["Incubation Period", "24hours"]),
        P("Absence of Salmonella spp."),
        P("iii. Subculture"),
        Grid(["Medium", "Test Agar"], ["Incubation Period", "24hours"]),
        T(new[]
            {
                R(C("Sampling Point Code", merge: VMerge.Restart), C("Sample Detail", merge: VMerge.Restart), C("Results", span: 2)),
                R(C(merge: VMerge.Continue), C(merge: VMerge.Continue), C("Escherichia coli"), C("Salmonella spp."))
            }
            .Concat(points.Select(point => R(C(point[0]), C(point[1]), C("Absent / Detected"), C("Absent / Detected"))))
            .ToArray()),
        P("Specifications: Absence of specified indicator pathogens."),
        P("Remark: Comply/ Do not comply")
    ];

    private static WorksheetImportProposal Propose(params string[][] points)
    {
        using var stream = Create(Sheet(points), Header);
        return WorksheetDocxImportService.Propose("water.docx", stream, InMemoryWorksheetImportCatalog.Empty);
    }

    private static readonly string[][] ThreePoints = [["SP 1", "Raw water tank"], ["SP 2", "Sand filter"], ["NSP 4", "Filling line"]];

    private static Dictionary<string, ProposedWorksheetField> Fields(WorksheetImportProposal proposal) =>
        proposal.Template.Sections.SelectMany(section => section.Fields).ToDictionary(field => field.FieldKey);

    [Fact]
    public void The_template_is_one_sampling_points_results()
    {
        var proposal = Propose(ThreePoints);
        var fields = Fields(proposal);

        Assert.Equal((ArdFamily.PurifiedWater, WorksheetCategory.Microbial), (proposal.Family, proposal.Template.Category));
        Assert.Equal((WorksheetFieldType.ColonyCount, WorksheetFieldMode.Entry), (fields["cfu_per_100ml"].Type, fields["cfu_per_100ml"].Mode));
        Assert.Equal((WorksheetFieldType.Result, WorksheetFieldMode.Calculated, "{cfu_per_100ml} / 100", "CFU/mL"),
            (fields["cfu_per_ml"].Type, fields["cfu_per_ml"].Mode, fields["cfu_per_ml"].FormulaExpression, fields["cfu_per_ml"].Unit));
        Assert.Equal(["Absent", "Detected"], fields["escherichia_coli_result"].Options);
        Assert.Equal(WorksheetFieldType.Select, fields["salmonella_spp_result"].Type);
        Assert.Equal("Membrane filtration", fields["count_method"].ConstantValue);
        Assert.Equal("Test Broth", fields["escherichia_coli_selection_medium"].ConstantValue);
        Assert.Equal(["Complies", "Does not comply"], fields["count_remark"].Options);
        Assert.Equal(["Complies", "Does not comply"], fields["pathogens_remark"].Options);

        // No point is a template row, and nothing per point (AR numbers) reaches the template.
        Assert.DoesNotContain(fields.Values, field => field.Type == WorksheetFieldType.Table);
        var texts = fields.Values.SelectMany(field => new[] { field.Label, field.ConstantValue ?? string.Empty }).ToList();
        foreach (var runData in new[] { "SP 1", "Raw water tank", "QCD/WT/26", "Test Issuer", "QCD/MIC/BDTA", "26/0001" })
            Assert.DoesNotContain(texts, text => text.Contains(runData));
    }

    [Fact]
    public void Media_cited_by_serial_become_a_batch_field_and_a_reference()
    {
        var fields = Fields(Propose(ThreePoints));

        Assert.Equal(WorksheetFieldType.Reagent, fields["medium_batch_test_agar"].Type);
        Assert.Equal("medium_batch_test_agar", fields["media_qualification_test_agar"].ReferencedResultResolutionFieldKey);
        Assert.DoesNotContain(fields.Keys, key => key.Contains("results") && key.Contains("refer"));
    }

    [Fact]
    public void Points_become_proposals_grouped_by_their_printed_limit()
    {
        var proposal = Propose(ThreePoints);

        Assert.Equal(["SP 1", "SP 2", "NSP 4"], proposal.SamplingPointProposals.Select(point => point.Code));
        Assert.All(proposal.SamplingPointProposals, point => Assert.Equal((PurifiedWaterRecognizer.Area, SamplingPointType.Water), (point.Area, point.Type)));
        Assert.Equal([["SP 1"], ["SP 2"]], proposal.SamplingPointGroupProposals.Select(group => group.PointCodes));
        Assert.Equal("Purified water – NMT 80 cfu/mL", proposal.SamplingPointProposals[1].GroupName);
        Assert.Null(proposal.SamplingPointProposals[2].GroupName);
        Assert.Contains(proposal.Flags, flag => flag.Code == WorksheetImportFlagCodes.SamplingPointWithoutLimit && flag.Message.Contains("NSP 4"));

        var tiers = proposal.SpecificationProposals.Where(item => item.SourceFieldKey == "cfu_per_ml").ToList();
        Assert.Equal(["NMT 500cfu/mL", "NMT 80 cfu/mL"], tiers.Select(item => item.AcceptanceCriteria));
        var pathogens = proposal.SpecificationProposals.Where(item => item.SourceFieldKey.EndsWith("_result")).ToList();
        Assert.Equal(2, pathogens.Count);
        Assert.All(pathogens, item => Assert.Equal(("Absent", "Absence of specified indicator pathogens."), (item.AcceptanceCriteria, item.PrintedCriteria)));
    }

    [Fact]
    public void A_different_point_list_proposes_the_same_template()
    {
        var few = WorksheetImportTemplateMapper.ToCreateRequest(Propose(ThreePoints).Template);
        var many = WorksheetImportTemplateMapper.ToCreateRequest(Propose([.. ThreePoints, ["SP 3", "Softener"], ["NSP 1", "Syrup"]]).Template);

        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(few), System.Text.Json.JsonSerializer.Serialize(many));
    }

    [Fact]
    public async Task The_count_is_computed_and_the_template_saves()
    {
        var request = WorksheetImportTemplateMapper.ToCreateRequest(Propose(ThreePoints).Template);
        var entities = request.Sections.SelectMany(section => section.Fields)
            .Select(field => new WorksheetField { FieldKey = field.FieldKey, Type = field.Type, Mode = field.Mode, FormulaExpression = field.FormulaExpression })
            .ToList();
        Assert.True(QcWorksheetCalculator.TryEvaluateAll(entities, [new WorksheetFieldValue { FieldKey = "cfu_per_100ml", Value = "250" }],
            out var computed, out var failure), failure?.Reason);
        Assert.Equal("2.5", computed.Single(item => item.Field.FieldKey == "cfu_per_ml").Value);

        using var harness = new QcWorksheetTestContext();
        var result = await harness.Templates.CreateTemplate(request, (await harness.SeedUser()).Id);
        Assert.True(result.IsSuccess, result.Error?.Description);
    }

    [Theory]
    [InlineData("SP1- SP3 – NMT 500cfu/mL", "SP1- SP3", new[] { "sp1", "sp2", "sp3" }, "NMT 500cfu/mL")]
    [InlineData("SP4-SP9 - NMT 200 cfu/mL", "SP4-SP9", new[] { "sp4", "sp5", "sp6", "sp7", "sp8", "sp9" }, "NMT 200 cfu/mL")]
    [InlineData("SP14, NSP1-NSP2– NMT 80 cfu/mL", "SP14, NSP1-NSP2", new[] { "sp14", "nsp1", "nsp2" }, "NMT 80 cfu/mL")]
    public void Limit_tiers_expand_their_point_ranges(string text, string printed, string[] keys, string criteria)
    {
        Assert.True(WaterSpecificationTiers.TryParse(text, out var tier));
        Assert.Equal((printed, criteria), (tier.PrintedPoints, tier.Criteria));
        Assert.Equal(keys, tier.PointKeys);
    }

    [Theory]
    [InlineData("Specification: NMT 200cfu/mL")]
    [InlineData("Absence of specified indicator pathogens.")]
    [InlineData("SP1 to the tank – NMT 5")]
    public void Other_text_is_not_a_tier(string text) => Assert.False(WaterSpecificationTiers.TryParse(text, out _));

    [Fact]
    public void A_point_listed_twice_is_reported()
    {
        Assert.True(WaterSpecificationTiers.TryParse("NSP6, NSP6, NSP7 – NMT 80 cfu/mL", out var tier));
        Assert.Equal(["nsp6", "nsp7"], tier.PointKeys);
        Assert.Equal(["NSP6"], tier.Duplicates);
    }

    [Theory]
    [InlineData("i.  Sample Preparation and Pre-Incubation", "pre_incubation")]
    [InlineData("ii. Selection and Subculture", "selection_subculture")]
    [InlineData("Subculture", "subculture")]
    public void Step_captions_get_short_keys(string caption, string key) =>
        Assert.Equal(key, ImportText.StepKey(ImportText.StripEnumerator(caption)));

    [Theory]
    [InlineData("Iii .Rappaport Vassiliadis Broth", "Rappaport Vassiliadis Broth")]
    [InlineData("ii.  MacConkey Agar", "MacConkey Agar")]
    [InlineData("Cetrimide Agar", "Cetrimide Agar")]
    public void Enumerators_are_stripped(string text, string expected) => Assert.Equal(expected, ImportText.StripEnumerator(text));
}

using APP.Services.QcWorksheets.WorksheetDocxImport;
using DOMAIN.Entities.QcWorksheets;
using Xunit;
using static APP.Tests.QcWorksheets.WorksheetImport.TestDocx;

namespace APP.Tests.QcWorksheets.WorksheetImport;

public class ImportTablePrimitiveTests
{
    private static readonly Guid BalanceId = Guid.NewGuid();
    private static readonly Guid BufferId = Guid.NewGuid();
    private static readonly Guid AgarTemplateId = Guid.NewGuid();

    private static readonly InMemoryWorksheetImportCatalog Catalog = new(
        [new CatalogEquipment(BalanceId, "QCD/EQT/BAL/001", "Balance one")],
        [new CatalogReagent(BufferId, "Phosphate Buffer")],
        [new CatalogTemplate(AgarTemplateId, "MQ-TSA", "Culture Media Qualification – Test Soya Agar")]);

    private static (ImportProposalBuilder Builder, DocxBlock Block) Build(params string[][] rows)
    {
        var document = Read([Grid(rows)]);
        var builder = new ImportProposalBuilder(new WorksheetImportProposal(), Catalog);
        return (builder, document.Blocks.Single());
    }

    private static List<ProposedWorksheetField> Fields(ImportProposalBuilder builder) =>
        builder.Template.Sections.SelectMany(section => section.Fields).ToList();

    [Fact]
    public void Equipment_rows_become_instrument_entries_matched_by_code()
    {
        var (builder, block) = Build(
            ["EQUIPMENT USED", "EQUIPMENT CODE"],
            ["Weighing Balance", "QCD/EQT/BAL/001"],
            ["BOD Incubator", "QCD/EQT/BOD/009"],
            ["", ""]);

        Assert.True(EquipmentTable.Is(block.Table));
        EquipmentTable.Apply(block, builder);

        var fields = Fields(builder);
        Assert.Equal(2, fields.Count);
        Assert.All(fields, field => Assert.Equal((WorksheetFieldType.Instrument, WorksheetFieldMode.Entry), (field.Type, field.Mode)));
        Assert.Equal(BalanceId, builder.Proposal.EquipmentMatches[0].EquipmentId);
        Assert.Null(builder.Proposal.EquipmentMatches[1].EquipmentId);
        var flag = Assert.Single(builder.Proposal.Flags);
        Assert.Equal(WorksheetImportFlagCodes.UnmatchedEquipment, flag.Code);
    }

    [Fact]
    public void Reagent_rows_are_matched_by_normalized_name()
    {
        var (builder, block) = Build(["Reagent", "Batch"], ["phosphate  buffer", ""], ["Unknown Salt", ""]);

        Assert.True(ReagentTable.Is(block.Table));
        ReagentTable.Apply(block, builder);

        Assert.Equal(BufferId, builder.Proposal.ReagentMatches[0].ReagentId);
        Assert.Null(builder.Proposal.ReagentMatches[1].ReagentId);
        Assert.Equal(WorksheetImportFlagCodes.UnmatchedReagent, Assert.Single(builder.Proposal.Flags).Code);
    }

    [Fact]
    public void A_cited_medium_becomes_a_resolution_reagent_and_a_referenced_result()
    {
        var (builder, block) = Build(
            ["", "Test Soya Agar", "Other Broth"],
            ["Medium Batch no:", "QCD/26/001/0000000001", "QCD/26/002/0000000002"],
            ["Remark", "Complies / Does not comply", "Complies / Does not comply"]);

        Assert.True(MediaReference.IsMediaReferenceTable(block.Table));
        MediaReference.Apply(block, builder);

        var fields = Fields(builder);
        var referenced = fields.Where(field => field.Type == WorksheetFieldType.ReferencedResult).ToList();
        Assert.Equal(2, referenced.Count);
        Assert.Equal(AgarTemplateId, referenced[0].ReferencedResultSourceTemplateId);
        Assert.Equal(CultureMediaKeys.Remark, referenced[0].ReferencedResultSourceFieldKey);
        var resolution = fields.Single(field => field.FieldKey == referenced[0].ReferencedResultResolutionFieldKey);
        Assert.Equal(WorksheetFieldType.Reagent, resolution.Type);
        Assert.Null(referenced[1].ReferencedResultSourceTemplateId);
        Assert.Contains(builder.Proposal.Flags, flag => flag.Code == WorksheetImportFlagCodes.MediaTemplateMissing);

        // The printed batch numbers are run data: they appear nowhere in the proposal.
        Assert.DoesNotContain(fields, field => (field.Label + field.ConstantValue).Contains("0000000001"));
        Assert.True(MediaReference.IsReferToSheet("Results: Refer to Culture Media Batch Data sheet serial numbered: X"));
    }

    [Theory]
    [InlineData("Analysed By:\t\tChecked By: Date:\t\tDate:", true)]
    [InlineData("DONE BY CHECKED BY SIGNATURE", true)]
    [InlineData("Checked equipment", false)]
    public void Sign_off_blocks_are_recognised(string text, bool expected) =>
        Assert.Equal(expected, SignOffTable.IsSignOff(text));

    [Fact]
    public void A_sign_off_grid_is_recognised_as_a_table()
    {
        Assert.True(SignOffTable.Is(Table(["", "DONE BY", "CHECKED BY"], ["SIGNATURE", "", ""])));
    }

    [Theory]
    [InlineData("7.2 ± 0.2", "7.2 ± 0.2 (7.0 – 7.4)")]
    [InlineData("7.20 - 7.60", "7.20 – 7.60")]
    [InlineData("5.00 – 5.40", "5.00 – 5.40")]
    [InlineData("pH Range: 5.6 ± 0.2 ", "5.6 ± 0.2 (5.4 – 5.8)")]
    public void Printed_ranges_are_read_and_plus_minus_spelled_out(string text, string expected)
    {
        Assert.True(PrintedSpecification.TryParseRange(text, out var criteria));
        Assert.Equal(expected, criteria);
    }

    [Fact]
    public void Specification_statements_and_limits_are_read()
    {
        Assert.True(PrintedSpecification.TryParseStatement("Specification: NMT 2000cfu/g", out var criteria));
        Assert.Equal("NMT 2000cfu/g", criteria);
        Assert.True(PrintedSpecification.IsLimit("Absence of E. coli in 1g of sample"));
        Assert.False(PrintedSpecification.TryParseStatement("Remark: Complies", out _));
    }

    [Fact]
    public void An_area_specification_table_becomes_one_proposal_per_area()
    {
        var (_, block) = Build(["AREA", "SPECIFICATION"], ["Rooms", "NMT 100 cfu/4Hrs"], ["Dispensing Booth", "NMT 5 cfu/4Hrs"]);

        Assert.True(PrintedSpecification.IsAreaSpecificationTable(block.Table));
        var proposals = PrintedSpecification.FromAreaSpecificationTable(block, "Airborne viables", "airborne_viables");

        Assert.Equal([("Rooms", "NMT 100 cfu/4Hrs"), ("Dispensing Booth", "NMT 5 cfu/4Hrs")],
            proposals.Select(proposal => (proposal.GroupName, proposal.AcceptanceCriteria)));
        Assert.All(proposals, proposal => Assert.Equal(ImportConfidence.High, proposal.Confidence));
    }

    [Fact]
    public void A_room_grid_becomes_sampling_point_proposals()
    {
        var document = Read([T(
            R(C("Room No. | ID No.", merge: VMerge.Restart), C("Room/Area Name", merge: VMerge.Restart), C("Airborne Viables")),
            R(C(merge: VMerge.Continue), C(merge: VMerge.Continue), C("(CFU/4Hrs)")),
            R(C("R-01"), C("Corridor"), C()),
            R(C("R-02"), C("Wash"), C()))]);

        var block = document.Blocks.Single();
        Assert.True(SamplingPointList.Is(block.Table));
        var points = SamplingPointList.Read(block, "Test Block", SamplingPointType.Environmental);

        Assert.Equal([("R-01", "Corridor"), ("R-02", "Wash")], points.Select(point => (point.Code, point.Name)));
        Assert.All(points, point => Assert.Equal("Test Block", point.Area));
    }
}

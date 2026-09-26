using APP.Services.QcWorksheets.WorksheetDocxImport;
using DocumentFormat.OpenXml;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static APP.Tests.QcWorksheets.QcWorksheetRuns;
using static APP.Tests.QcWorksheets.WorksheetImport.TestDocx;

namespace APP.Tests.QcWorksheets.WorksheetImport;

/// <summary>Every EM area sheet proposes one shared template; points, groups and specifications stay per file.</summary>
public class EmSharedTemplateTests
{
    private const string Header = "ENVIRONMENTAL MONITORING RAW DATA - MICROBIOLOGY\tA.R. No.: QCD/MIC/EN/001";

    private static IEnumerable<OpenXmlElement> Sheet(string room, params string[][] equipment) =>
    [
        Grid(["Analysis Start Date:", "Analysis End Date:"]),
        P("Airborne Viables"),
        Grid(["Method", "Plate Exposure"], ["Period of Exposure", "4 Hours"]),
        T(R(C("Room No. | ID No.", merge: VMerge.Restart), C("Room/Area Name", merge: VMerge.Restart), C("Airborne Viables")),
          R(C(merge: VMerge.Continue), C(merge: VMerge.Continue), C("(CFU/4Hrs)")),
          R(C(room), C("Corridor"), C())),
        Grid(["AREA", "SPECIFICATION"], ["Rooms", "NMT 100 cfu/4Hrs"]),
        Grid([["EQUIPMENT USED", "EQUIPMENT CODE"], .. equipment])
    ];

    private static WorksheetImportProposal Propose(string area, string room, params string[][] equipment)
    {
        using var stream = Create(Sheet(room, equipment), Header);
        return WorksheetDocxImportService.Propose($"EM WORKSHEET ({area}).docx", stream, InMemoryWorksheetImportCatalog.Empty);
    }

    private static readonly string[] Incubator = ["BOD Incubator", "QCD/EQT/BOD/001"];
    private static readonly string[] Balance = ["Weighing Balance", "QCD/EQT/BAL/006"];

    [Fact]
    public void An_upload_of_several_area_sheets_proposes_the_template_once_with_every_instrument()
    {
        var tablet = Propose("Tablet", "SF-01", Incubator);
        var syrup = Propose("Syrup", "GF-01", Incubator, Balance);

        WorksheetDocxImportService.ApplyBatchRules([tablet, syrup], InMemoryWorksheetImportCatalog.Empty);

        Assert.Equal(("EM-AIRBORNE-VIABLES", "Environmental Monitoring – Airborne Viables"), (tablet.Template.Code, tablet.Template.Name));
        var instruments = tablet.Template.Sections.SelectMany(section => section.Fields)
            .Where(field => field.Type == WorksheetFieldType.Instrument).Select(field => field.FieldKey);
        Assert.Equal(["instrument_qcd_eqt_bod_001", "instrument_qcd_eqt_bal_006"], instruments);

        Assert.Null(syrup.Template);
        Assert.Equal(WorksheetImportFlagCodes.SharedTemplateInBatch, syrup.Flags[0].Code);
        Assert.Equal(("EM-AIRBORNE-VIABLES", "EM WORKSHEET (Tablet).docx"), (syrup.SharedTemplate.Key, syrup.SharedTemplate.CarriedBy));
        Assert.Equal("EM WORKSHEET (Tablet).docx", tablet.SharedTemplate.CarriedBy);
        Assert.Empty(syrup.FieldProvenance);
        Assert.DoesNotContain(syrup.Flags, flag => flag.Code == WorksheetImportFlagCodes.UnmatchedEquipment);

        // The file's own proposals stay with it.
        Assert.Equal(("GF-01", "Syrup"), (syrup.SamplingPointProposals.Single().Code, syrup.SamplingPointProposals.Single().Area));
        Assert.Equal(EnvironmentalMonitoringRecognizer.ResultKey, syrup.SpecificationProposals.Single().SourceFieldKey);
    }

    [Fact]
    public void Each_sheet_alone_proposes_the_same_template()
    {
        var tablet = WorksheetImportTemplateMapper.ToCreateRequest(Propose("Tablet", "SF-01", Incubator).Template);
        var syrup = WorksheetImportTemplateMapper.ToCreateRequest(Propose("Syrup", "GF-77", Incubator).Template);

        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(tablet), System.Text.Json.JsonSerializer.Serialize(syrup));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_saved_shared_template_is_not_proposed_again(bool savedHasTheField)
    {
        var saved = new CatalogTemplate(Guid.NewGuid(), "EM-AIRBORNE-VIABLES", "Environmental Monitoring – Airborne Viables",
            savedHasTheField ? ["method", EnvironmentalMonitoringRecognizer.ResultKey] : ["method"]);
        var catalog = new InMemoryWorksheetImportCatalog([], [], [], [saved]);
        var tablet = Propose("Tablet", "SF-01", Incubator);

        WorksheetDocxImportService.ApplyBatchRules([tablet], catalog);

        Assert.Null(tablet.Template);
        Assert.Equal(WorksheetImportFlagCodes.SharedTemplateExists, tablet.Flags[0].Code);
        Assert.Equal((saved.Id, "EM-AIRBORNE-VIABLES", (string?)null),
            (tablet.SharedTemplate.ExistingTemplateId!.Value, tablet.SharedTemplate.ExistingTemplateCode, tablet.SharedTemplate.CarriedBy));
        var specification = tablet.SpecificationProposals.Single();
        Assert.Equal(saved.Id, specification.SourceWorksheetTemplateId);
        Assert.Equal(savedHasTheField ? EnvironmentalMonitoringRecognizer.ResultKey : null, specification.SourceFieldKey);
        Assert.Equal("SF-01", tablet.SamplingPointProposals.Single().Code);
    }

    /// <summary>
    /// The imported count is a ColonyCount entry, and detection judges whatever a characteristic
    /// binds: a count over the tier's Action limit opens a case through the real submit path.
    /// </summary>
    [Theory]
    [InlineData("120", true)]
    [InlineData("85", false)]
    public async Task An_over_limit_airborne_viables_count_opens_an_oos_case(string count, bool opens)
    {
        var proposal = Propose("Tablet", "SF-01", Incubator);
        var imported = proposal.Template.Sections.SelectMany(section => section.Fields)
            .Single(field => field.FieldKey == EnvironmentalMonitoringRecognizer.ResultKey);
        var tier = proposal.SpecificationProposals.Single();

        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedTemplateWithFields($"WS/EM/{Guid.NewGuid().ToString()[..6]}", WorksheetCategory.Microbial,
            [new WorksheetField { FieldKey = imported.FieldKey, Label = imported.Label, Type = imported.Type, Mode = imported.Mode, Unit = imported.Unit, Order = 1 }]);
        var (instanceId, analyst) = await StartedWorksheet(harness, template);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);
        var specification = await harness.Db.QcSpecifications.SingleAsync();
        await harness.SeedCharacteristic(specification, template, tier.SourceFieldKey,
            acceptanceCriteria: tier.AcceptanceCriteria, alertLimit: "NMT 80 cfu/4Hrs", actionLimit: tier.ActionLimit);

        var saved = await harness.WorksheetInstances.SaveValues(instanceId,
            Values(new WorksheetFieldValueEntry { FieldKey = imported.FieldKey, Value = count }), analyst.Id);
        Assert.True(saved.IsSuccess, saved.Error?.Description);
        var submitted = await harness.WorksheetInstances.Submit(instanceId, analyst.Id);
        Assert.True(submitted.IsSuccess, submitted.Error?.Description);

        var cases = await harness.Db.QcOosCases.AsNoTracking().Where(item => item.WorksheetInstanceId == instanceId).ToListAsync();
        Assert.Equal(opens, cases.Count == 1);
        if (opens)
            Assert.Equal((EnvironmentalMonitoringRecognizer.ResultKey, "120"), (cases[0].FieldKey, cases[0].ObservedValue));
    }
}

using APP.Services.QcWorksheets;
using APP.Services.QcWorksheets.WorksheetDocxImport;
using DocumentFormat.OpenXml;
using DOMAIN.Entities.QcWorksheets;
using Xunit;
using static APP.Tests.QcWorksheets.WorksheetImport.TestDocx;

namespace APP.Tests.QcWorksheets.WorksheetImport;

public class ProductMicroRecognizerTests
{
    private const string Header =
        "ANALYTICAL RAW DATA – MICROBIOLOGY\tBatch No.: ZZ001\tProduct Name: TEST SYRUP\tPage 1 of 3\tA.R. No.: XQC/FP/26/0001"
        + "\tMfg. Date: 01/2026\tSampled On: 02/02/2026\tSampled by: Test Sampler\tIssued by: Test Issuer\tSpec. No.: XX/FP/SPC/001"
        + "\tIssue No.: 26/0001\tAnalysis Start Date:\tAnalysis End Date:";

    private static IEnumerable<OpenXmlElement> Sheet(string dilution = "1 in 10") =>
    [
        P("A. MICROBIAL ENUMERATION TESTS"),
        T(R(C("Growth Promotion and Sterility of the Culture Media", span: 2)),
          R(C(), C("Test Soya Agar")),
          R(C("Medium Batch no:"), C("QCD/26/001/0000000001")),
          R(C("Remark"), C("Complies / Does not comply"))),
        P("Sample Preparation"),
        Grid(["Diluent used", "Sterilized buffer"], ["pH of Diluent", ""], ["Dilution", dilution]),
        P("Tests:"),
        T(R(C(), C("Total Aerobic Microbial Count")),
          R(C("Method"), C("Pour plate")),
          R(C("Incubation Period"), C("5 days"))),
        P("Results"),
        P("Total Aerobic Microbial Count (TAMC)"),
        T(R(C("Plate 1:_____cfu"), C("Plate 2:____cfu")),
          R(C("Average count: Plate 1 + Plate 2 => + = cfu / 2", span: 2)),
          R(C("Result in cfu/mL = Average Count x Dilution Factor = x = cfu/mL", span: 2))),
        P("Specification: NMT 200cfu/mL"),
        P("Remark: Complies / Does Not Comply"),
        P("B. TESTS FOR SPECIFIED MICROORGANISMS"),
        T(R(C("Pre-incubation", span: 2)), R(C("Medium:"), C("Test Broth")), R(C("Incubation period"), C("24 hours"))),
        P("Absence of Escherichia coli"),
        P("Subculture"),
        Grid(["Medium", "Test Agar"], ["Incubation Period", "24hours"]),
        P("Result and Interpretation: Presence of E.coli / Absence of E.coli in 1ml of sample."),
        P("Specification: Absence of E. coli in 1mL of sample"),
        P("Remark: Complies / Does Not Comply"),
        Grid(["EQUIPMENT USED", "EQUIPMENT CODE"], ["Weighing Balance", "QCD/EQT/BAL/001"], ["REAGENTS USED", "REAGENT CODE"],
             ["Test Soya Agar", "QCD/RGT/TSA-001"], ["Other Reagent", "QCD/RGT/OR-001"]),
        P("Conclusion: Product Complies / Does Not Comply with specifications"),
        Grid(["Analysed By: Checked By: Date: Date:"])
    ];

    private static WorksheetImportProposal Propose(IEnumerable<OpenXmlElement> body, IWorksheetImportCatalog? catalog = null)
    {
        using var stream = Create(body, Header);
        return WorksheetDocxImportService.Propose("product.docx", stream, catalog ?? InMemoryWorksheetImportCatalog.Empty);
    }

    private static Dictionary<string, ProposedWorksheetField> Fields(WorksheetImportProposal proposal) =>
        proposal.Template.Sections.SelectMany(section => section.Fields).ToDictionary(field => field.FieldKey);

    [Fact]
    public void The_enumeration_becomes_plates_a_calculated_average_and_a_judged_result()
    {
        var proposal = Propose(Sheet());
        var fields = Fields(proposal);

        Assert.Equal((ArdFamily.ProductMicro, WorksheetCategory.Microbial), (proposal.Family, proposal.Template.Category));
        Assert.Equal(("MIC-TEST-SYRUP", "Microbiology ARD – TEST SYRUP"), (proposal.Template.Code, proposal.Template.Name));
        Assert.Equal((WorksheetFieldType.ColonyCount, WorksheetFieldMode.Entry, "cfu"),
            (fields["tamc_plate_1"].Type, fields["tamc_plate_1"].Mode, fields["tamc_plate_1"].Unit));
        Assert.Equal("({tamc_plate_1} + {tamc_plate_2}) / 2", fields["tamc_average"].FormulaExpression);
        Assert.Equal((WorksheetFieldType.Result, WorksheetFieldMode.Calculated, "cfu/mL", "{tamc_average} * 10"),
            (fields["tamc_result"].Type, fields["tamc_result"].Mode, fields["tamc_result"].Unit, fields["tamc_result"].FormulaExpression));
        Assert.Equal("Pour plate", fields["tamc_method"].ConstantValue);
        Assert.Equal(["Complies", "Does not comply"], fields["tamc_remark"].Options);
    }

    [Fact]
    public void The_result_is_computed_by_the_calculator_and_judged_by_the_limit_evaluator()
    {
        var request = WorksheetImportTemplateMapper.ToCreateRequest(Propose(Sheet()).Template);
        var entities = request.Sections.SelectMany(section => section.Fields).Select(field => new WorksheetField
        {
            FieldKey = field.FieldKey, Type = field.Type, Mode = field.Mode, FormulaExpression = field.FormulaExpression, Order = field.Order
        }).ToList();
        var values = new[] { ("tamc_plate_1", "30"), ("tamc_plate_2", "60") }
            .Select(item => new WorksheetFieldValue { FieldKey = item.Item1, Value = item.Item2 }).ToList();

        Assert.True(QcWorksheetCalculator.TryEvaluateAll(entities, values, out var computed, out var failure), failure?.ToString());
        var result = computed.Single(item => item.Field.FieldKey == "tamc_result");
        Assert.Equal("450", result.Value);

        // OOS detection judges Result-typed fields against the characteristic bound to their key.
        Assert.Equal(WorksheetFieldType.Result, result.Field.Type);
        var limit = new SpecificationCharacteristic { AcceptanceCriteria = "NMT 200cfu/mL", SourceFieldKey = "tamc_result" };
        Assert.Equal(LimitOutcome.ActionOos, LimitEvaluator.Evaluate(limit, result.Value).Outcome);
        Assert.Equal(LimitOutcome.Compliant, LimitEvaluator.Evaluate(limit, "150").Outcome);
    }

    [Fact]
    public void A_blank_dilution_makes_the_factor_an_entry_the_result_formula_reads()
    {
        var fields = Fields(Propose(Sheet(dilution: "")));

        Assert.Equal(WorksheetFieldMode.Entry, fields["dilution_factor"].Mode);
        Assert.Equal("{tamc_average} * {dilution_factor}", fields["tamc_result"].FormulaExpression);
    }

    [Fact]
    public void Organisms_media_specifications_and_header_are_handled()
    {
        var proposal = Propose(Sheet());
        var fields = Fields(proposal);

        var organism = fields["escherichia_coli_result"];
        Assert.Equal((WorksheetFieldType.Select, 2), (organism.Type, organism.Options!.Count));
        Assert.Equal("Test Agar", fields["escherichia_coli_subculture_medium"].ConstantValue);
        Assert.Equal("Test Broth", fields["pre_incubation_medium"].ConstantValue);
        Assert.Equal(["Complies", "Does not comply"], fields["conclusion"].Options);

        Assert.Equal(WorksheetFieldType.ReferencedResult, fields["media_qualification_test_soya_agar"].Type);
        Assert.Contains(proposal.Flags, flag => flag.Code == WorksheetImportFlagCodes.MediaTemplateMissing && flag.Message.Contains("QCD/RGT/TSA-001"));
        Assert.DoesNotContain("reagent_test_soya_agar", fields.Keys);
        Assert.Contains("reagent_other_reagent", fields.Keys);

        // A choice result's criteria is exactly its compliant option; the printed sentence is context.
        Assert.Equal([("tamc_result", "NMT 200cfu/mL", "NMT 200cfu/mL"),
                      ("escherichia_coli_result", "Absence of E.coli", "Absence of E. coli in 1mL of sample")],
            proposal.SpecificationProposals.Select(item => (item.SourceFieldKey, item.AcceptanceCriteria, item.PrintedCriteria)));
        Assert.All(proposal.SpecificationProposals, item =>
            Assert.Equal((SpecificationStage.Finished, "TEST SYRUP", "XX/FP/SPC/001"), (item.Stage!.Value, item.ProductName, item.SpecificationCode)));

        var texts = fields.Values.SelectMany(field => new[] { field.Label, field.ConstantValue ?? string.Empty }).ToList();
        foreach (var runData in new[] { "ZZ001", "XQC/FP/26/0001", "Test Sampler", "Test Issuer", "01/2026", "QCD/26/001/0000000001" })
            Assert.DoesNotContain(texts, text => text.Contains(runData));
        Assert.Equal(["analysis_start_date", "analysis_end_date"], proposal.Template.Sections[0].Fields.Select(field => field.FieldKey));
    }

    [Fact]
    public void A_media_template_is_matched_by_the_reagent_list_code()
    {
        var saved = new CatalogTemplate(Guid.NewGuid(), "QCD/RGT/TSA-001", "Some differently named qualification");
        var fields = Fields(Propose(Sheet(), new InMemoryWorksheetImportCatalog([], [], [saved])));

        Assert.Equal(saved.Id, fields["media_qualification_test_soya_agar"].ReferencedResultSourceTemplateId);
    }

    [Theory]
    [InlineData("Absence of E. coli in 1g of sample", "Absence of E.coli")]
    [InlineData("absence of  e.coli", "Absence of E.coli")]
    [InlineData("Presence of E.coli", "Presence of E.coli")]
    [InlineData("Absence of specified indicator pathogens", null)]
    public void A_printed_qualitative_specification_maps_to_its_option(string printed, string? expected) =>
        Assert.Equal(expected, PrintedSpecification.CompliantOption(printed, ["Presence of E.coli", "Absence of E.coli"]));

    [Fact]
    public void A_printed_absence_maps_to_absent()
    {
        Assert.Equal("Absent", PrintedSpecification.CompliantOption("Absence of specified indicator pathogens.", ["Absent", "Detected"]));
        Assert.Null(PrintedSpecification.CompliantOption("NMT 100 cfu/mL", ["Absent", "Detected"]));
    }

    [Theory]
    [InlineData("1 in 10", 10)]
    [InlineData("1:100", 100)]
    [InlineData("1/10", 10)]
    [InlineData("2 in 10", 5)]
    public void Dilutions_parse_to_their_factor(string printed, double expected)
    {
        Assert.True(FormulaLibrary.TryParseDilutionFactor(printed, out var factor));
        Assert.Equal(expected, factor);
    }

    [Fact]
    public async Task The_proposal_saves_through_the_real_create_validation()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;

        var result = await harness.Templates.CreateTemplate(WorksheetImportTemplateMapper.ToCreateRequest(Propose(Sheet()).Template), userId);

        Assert.True(result.IsSuccess, result.Error?.Description);
    }
}

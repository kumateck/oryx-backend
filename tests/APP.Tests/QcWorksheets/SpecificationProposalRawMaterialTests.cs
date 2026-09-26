using APP.Repository.QcWorksheets;
using APP.Services.QcWorksheets.WorksheetDocxImport;
using APP.Tests.QcWorksheets.WorksheetImport;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace APP.Tests.QcWorksheets;

/// <summary>
/// Brief 09 through brief 08: a raw-material Specification document's proposals become a Draft
/// RawMaterial Specification with one Chemical link, through the real repositories.
/// </summary>
public class SpecificationProposalRawMaterialTests(ITestOutputHelper output)
{
    private static SpecificationProposalRepository Proposals(QcWorksheetTestContext harness) =>
        new(harness.Db, harness.Mapper, harness.Specifications);

    private static async Task<Guid> Store(
        QcWorksheetTestContext harness, Guid templateId, List<SpecificationCharacteristicProposal> rows,
        string code = "QCD/SPC/RM/901", string name = "TESTOCAINE HYDROCHLORIDE", string file = "901 spec.docx")
    {
        var result = await Proposals(harness).CreateProposalSet(new CreateSpecificationProposalSetRequest
        {
            Family = ArdFamily.RawMaterialSpecification, SourceFileName = file, WorksheetTemplateId = templateId,
            ProductName = name, SpecificationCode = code, SpecificationProposals = rows
        }, Guid.NewGuid());
        Assert.True(result.IsSuccess, result.Error?.Description);
        return result.Value.Id;
    }

    private static async Task<SpecificationDraftPlan> Draft(QcWorksheetTestContext harness, params Guid[] ids)
    {
        var result = await Proposals(harness).BuildDraftPlan(new SpecificationProposalDraftRequest { ProposalSetIds = ids.ToList() });
        Assert.True(result.IsSuccess, result.Error?.Description);
        return result.Value;
    }

    private static Task<SHARED.Result<SpecificationProposalApplyResult>> Apply(QcWorksheetTestContext harness, SpecificationDraftPlan plan, params Guid[] ids) =>
        Proposals(harness).Apply(new SpecificationProposalApplyRequest { ProposalSetIds = ids.ToList(), Plan = plan }, Guid.NewGuid());

    [Fact]
    public async Task A_raw_material_specification_drafts_a_chemical_raw_material_plan_in_document_order()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("RM-901", WorksheetCategory.Chemical,
            "description_result", "related_substances_impurity_e", "related_substances_total", "microbial_tamc");

        var id = await Store(harness, template.Id,
        [
            new() { TestName = "Description", AcceptanceCriteria = "White powder", ActionLimit = "White powder", SourceFieldKey = "description_result", GroupName = "CHEMICAL", Reference = "BP 2025" },
            new() { TestName = "Related Substances", Analyte = "Impurity E", AcceptanceCriteria = "NMT 0.30%", ActionLimit = "NMT 0.30%", SourceFieldKey = "related_substances_impurity_e", GroupName = "CHEMICAL" },
            new() { TestName = "Related Substances", Analyte = "Total Impurities", AcceptanceCriteria = "NMT 0.30%", ActionLimit = "NMT 0.30%", SourceFieldKey = "related_substances_total", GroupName = "CHEMICAL" },
            new() { TestName = "Microbial Contamination", Analyte = "TAMC", AcceptanceCriteria = "Not more than 10^3 CFU/g", ActionLimit = "Not more than 10^3 CFU/g", SourceFieldKey = "microbial_tamc", GroupName = "MICROBIAL" }
        ]);

        var plan = await Draft(harness, id);

        Assert.Equal(("QCD/SPC/RM/901", "TESTOCAINE HYDROCHLORIDE"), (plan.Code, plan.Name));
        Assert.Equal((SpecificationAppliesTo.RawMaterial, (SpecificationStage?)null, (QcRetestPolicy?)null), (plan.AppliesTo!.Value, plan.Stage, plan.RetestPolicy));
        var link = Assert.Single(plan.WorksheetLinks);
        Assert.Equal((template.Id, SpecificationAnalysisType.Chemical), (link.WorksheetTemplateId, link.AnalysisType!.Value));
        Assert.Equal(["description_result", "related_substances_impurity_e", "related_substances_total", "microbial_tamc"], plan.Characteristics.Select(row => row.SourceFieldKey));
        Assert.Equal([1, 2, 3, 4], plan.Characteristics.Select(row => row.DisplayOrder));
        Assert.Equal(["CHEMICAL", "CHEMICAL", "CHEMICAL", "MICROBIAL"], plan.Characteristics.Select(row => row.GroupName));
        Assert.Empty(plan.Warnings);

        plan.RetestPolicy = QcRetestPolicy.SameSample;
        var applied = await Apply(harness, plan, id);
        Assert.True(applied.IsSuccess, applied.Error?.Description);

        var specification = await harness.Db.QcSpecifications.Include(item => item.Characteristics).Include(item => item.WorksheetLinks)
            .SingleAsync(item => item.Id == applied.Value.SpecificationId);
        Assert.Equal((QcDocumentStatus.Draft, SpecificationAppliesTo.RawMaterial, (SpecificationStage?)null),
            (specification.Status, specification.AppliesTo, specification.Stage));
        Assert.Equal(SpecificationAnalysisType.Chemical, Assert.Single(specification.WorksheetLinks).AnalysisType);
        Assert.Equal(4, specification.Characteristics.Count);
    }

    [Fact]
    public async Task A_raw_material_plan_takes_exactly_one_set()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("RM-901", WorksheetCategory.Chemical, "ph_result");
        List<SpecificationCharacteristicProposal> Row() => [new() { TestName = "pH", AcceptanceCriteria = "4 – 6", SourceFieldKey = "ph_result", GroupName = "CHEMICAL" }];

        var first = await Store(harness, template.Id, Row());
        var second = await Store(harness, template.Id, Row(), file: "again.docx");

        var result = await Proposals(harness).BuildDraftPlan(new SpecificationProposalDraftRequest { ProposalSetIds = [first, second] });
        Assert.Equal("QcSpecificationProposal.ProductSingleSet", result.Error.Code);
    }

    /// <summary>
    /// The import screen's flow on the real corpus: save each worksheet proposal (after the reviewer
    /// fills the formulas left empty), then post its Specification document's set against the saved
    /// template, draft and apply.
    /// </summary>
    [RmCorpusFact]
    public async Task Corpus_worksheets_save_and_their_specifications_apply()
    {
        var files = RmCorpus.Load();
        foreach (var key in RmCorpus.Keys)
        {
            using var harness = new QcWorksheetTestContext();
            var userId = (await harness.SeedUser()).Id;
            var worksheet = files.Single(file => file.Key == key && !file.IsSpecification).Proposal;
            var specification = files.Single(file => file.Key == key && file.IsSpecification).Proposal;

            var request = WorksheetImportTemplateMapper.ToCreateRequest(worksheet.Template);
            foreach (var section in request.Sections)
            {
                var input = section.Fields.FirstOrDefault(field => field.Type == WorksheetFieldType.Number && field.Mode == WorksheetFieldMode.Entry);
                foreach (var field in section.Fields.Where(field => field.Mode == WorksheetFieldMode.Calculated && string.IsNullOrEmpty(field.FormulaExpression)))
                    field.FormulaExpression = input is null ? "100" : $"{{{input.FieldKey}}}";
            }

            var saved = await harness.Templates.CreateTemplate(request, userId);
            Assert.True(saved.IsSuccess, $"{key}: {saved.Error?.Code} {saved.Error?.Description}");
            var templateId = await harness.Db.QcWorksheetTemplates.Where(item => item.Code == $"RM-{key}").Select(item => item.Id).SingleAsync();

            var id = await Store(harness, templateId, specification.SpecificationProposals,
                specification.RawMaterial.SpecificationCode, specification.RawMaterial.MaterialName, $"{key} spec.docx");
            var plan = await Draft(harness, id);
            output.WriteLine($"{key}: {plan.Characteristics.Count} characteristics, warnings: {string.Join(", ", plan.Warnings.Select(warning => warning.Code))}");

            Assert.Equal(SpecificationAppliesTo.RawMaterial, plan.AppliesTo);
            Assert.Null(plan.Stage);
            Assert.Equal(SpecificationAnalysisType.Chemical, Assert.Single(plan.WorksheetLinks).AnalysisType);
            Assert.Equal(specification.SpecificationProposals.Count, plan.Characteristics.Count);
            Assert.DoesNotContain(plan.Warnings, warning => warning.Code == SpecificationDraftPlanWarningCodes.FieldNotOnTemplate);
            Assert.Equal(specification.SpecificationProposals.Select(row => row.GroupName), plan.Characteristics.Select(row => row.GroupName));

            plan.RetestPolicy = QcRetestPolicy.FreshResample;
            var applied = await Apply(harness, plan, id);
            Assert.True(applied.IsSuccess, $"{key}: {applied.Error?.Code} {applied.Error?.Description}");
        }
    }
}

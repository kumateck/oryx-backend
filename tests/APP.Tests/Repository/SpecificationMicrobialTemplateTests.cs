using APP.Repository;
using APP.Services.Forms;
using AutoMapper;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Forms.Request;
using DOMAIN.Entities.MaterialARD;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.MaterialSpecifications;
using DOMAIN.Entities.MaterialStandardTestProcedures;
using DOMAIN.Entities.QualityRoutines;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class MicroSlotCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

/// <summary>A specification's microbial template slot sits beside its chemical template (FormId).</summary>
public class SpecificationMicrobialTemplateTests
{
    private static ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            new MicroSlotCurrentUserService());

    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(_ => { }, typeof(APP.Mapper.OryxMapper));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static Form Template(string name, AnalysisType? analysisType) => new()
    {
        Id = Guid.NewGuid(), Name = name,
        Sections =
        [
            new FormSection
            {
                Id = Guid.NewGuid(), Name = name + " test", AnalysisType = analysisType,
                Fields = [new FormField { Id = Guid.NewGuid(), Question = new Question { Id = Guid.NewGuid(), Label = "Result", Type = QuestionType.ShortAnswer } }]
            }
        ]
    };

    private static UpdateMaterialSpecificationRequest Update(MaterialSpecification spec, Guid? microbialFormId) => new()
    {
        RevisionReason = "Add the microbial template to this specification",
        SpecificationNumber = spec.SpecificationNumber, RevisionNumber = "02", SupersedesNumber = "01",
        EffectiveDate = DateTime.UtcNow, ReviewDate = DateTime.UtcNow.AddYears(1), DueDate = DateTime.UtcNow.AddYears(2),
        FormId = spec.FormId, MicrobialFormId = microbialFormId, Reference = "BP"
    };

    [Fact]
    public async Task No_microbial_template_is_valid()
    {
        await using var context = CreateContext();
        Assert.True((await SpecificationTemplateSlots.Validate(context, "Spec", Guid.NewGuid(), null)).IsSuccess);
    }

    [Fact]
    public async Task The_microbial_template_must_differ_from_the_chemical_one()
    {
        await using var context = CreateContext();
        var formId = Guid.NewGuid();
        var result = await SpecificationTemplateSlots.Validate(context, "Spec", formId, formId);
        Assert.Equal("Spec.MicrobialForm", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task A_chemical_only_template_cannot_fill_the_microbial_slot()
    {
        await using var context = CreateContext();
        var chemicalOnly = Template("Assay", AnalysisType.Chemical);
        context.Forms.Add(chemicalOnly);
        await context.SaveChangesAsync();

        var result = await SpecificationTemplateSlots.Validate(context, "Spec", Guid.NewGuid(), chemicalOnly.Id);

        Assert.Contains("no sections that apply to microbial", Assert.Single(result.Errors).Description);
    }

    [Theory]
    [InlineData(AnalysisType.Microbial)]
    [InlineData(null)]
    public async Task Microbial_or_shared_sections_fill_the_microbial_slot(AnalysisType? analysisType)
    {
        await using var context = CreateContext();
        var micro = Template("TAMC", analysisType);
        context.Forms.Add(micro);
        await context.SaveChangesAsync();

        Assert.True((await SpecificationTemplateSlots.Validate(context, "Spec", Guid.NewGuid(), micro.Id)).IsSuccess);
    }

    [Fact]
    public async Task Each_slot_keeps_its_own_test_references()
    {
        await using var context = CreateContext();
        var chemical = Template("Assay", AnalysisType.Chemical);
        var micro = Template("TAMC", AnalysisType.Microbial);
        var spec = new MaterialSpecification { Id = Guid.NewGuid(), SpecificationNumber = "QCD/SPC/RM/900", FormId = chemical.Id, MicrobialFormId = micro.Id, MaterialId = Guid.NewGuid() };
        context.AddRange(chemical, micro, spec);
        await context.SaveChangesAsync();
        var forms = new FormRepository(context, null!, null!, null!, null!);

        await forms.SubmitFormSectionValue([new SubmitFormSectionValue { FormSectionId = chemical.Sections[0].Id, Value = "98.0 - 102.0 %" }], spec.Id, null);
        await forms.SubmitFormSectionValue([new SubmitFormSectionValue { FormSectionId = micro.Sections[0].Id, Value = "NMT 1000 cfu/g" }], spec.Id, null);

        var values = await context.FormSections.AsNoTracking().ToDictionaryAsync(section => section.Id, section => section.Value);
        Assert.Equal("98.0 - 102.0 %", values[chemical.Sections[0].Id]);
        Assert.Equal("NMT 1000 cfu/g", values[micro.Sections[0].Id]);
    }

    [Fact]
    public async Task Changing_the_microbial_template_is_refused_while_a_microbial_ard_uses_it()
    {
        await using var context = CreateContext();
        var chemical = Template("Assay", AnalysisType.Chemical);
        var micro = Template("TAMC", AnalysisType.Microbial);
        var material = new Material { Id = Guid.NewGuid(), Name = "Maize starch", Code = "RM-900" };
        var stp = new MaterialStandardTestProcedure { Id = Guid.NewGuid(), StpNumber = "QCD/STP/RM/900", MaterialId = material.Id };
        var spec = new MaterialSpecification { Id = Guid.NewGuid(), SpecificationNumber = "QCD/SPC/RM/900", RevisionNumber = "01", SupersedesNumber = "00", FormId = chemical.Id, MicrobialFormId = micro.Id, MaterialId = material.Id };
        var ard = new MaterialAnalyticalRawData { Id = Guid.NewGuid(), StpId = stp.Id, FormId = micro.Id, AnalysisType = AnalysisType.Microbial, SpecNumber = spec.SpecificationNumber };
        context.AddRange(chemical, micro, material, stp, spec, ard);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new MaterialSpecificationRepository(context, CreateMapper());

        var result = await repository.UpdateMaterialSpecification(spec.Id, Update(spec, null));

        Assert.Equal("MaterialSpecification.MicrobialFormInUse", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task A_microbial_ard_must_use_the_specifications_microbial_template()
    {
        await using var context = CreateContext();
        var chemical = Template("Assay", AnalysisType.Chemical);
        var micro = Template("TAMC", AnalysisType.Microbial);
        var other = Template("Other micro", AnalysisType.Microbial);
        var material = new Material { Id = Guid.NewGuid(), Name = "Maize starch", Code = "RM-901" };
        var stp = new MaterialStandardTestProcedure { Id = Guid.NewGuid(), StpNumber = "QCD/STP/RM/901", MaterialId = material.Id };
        var spec = new MaterialSpecification { Id = Guid.NewGuid(), SpecificationNumber = "QCD/SPC/RM/901", FormId = chemical.Id, MicrobialFormId = micro.Id, MaterialId = material.Id };
        context.AddRange(chemical, micro, other, material, stp, spec);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new MaterialAnalyticalRawDataRepository(context, CreateMapper());
        var request = new CreateMaterialAnalyticalRawDataRequest
        {
            FormId = other.Id, StpId = stp.Id, MaterialId = material.Id, AnalysisType = AnalysisType.Microbial,
            SpecNumber = spec.SpecificationNumber,
            CoaItems = [new CommercialCoaItemRequest { FormFieldId = micro.Sections[0].Fields[0].Id, DisplayLabel = "TAMC", GroupName = "MICROBIAL", SpecificationText = "NMT 1000 cfu/g" }]
        };

        var refused = await repository.CreateAnalyticalRawData(request);
        Assert.Equal("MaterialArd.MicrobialForm", Assert.Single(refused.Errors).Code);

        request.FormId = micro.Id;
        var created = await repository.CreateAnalyticalRawData(request);
        Assert.True(created.IsSuccess, string.Join("; ", created.Errors?.Select(error => error.Code + ": " + error.Description) ?? []));
    }
}

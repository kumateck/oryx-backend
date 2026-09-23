using System.Text.Json;
using APP.Repository;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.MaterialARD;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.MaterialSampling;
using DOMAIN.Entities.MaterialStandardTestProcedures;
using DOMAIN.Entities.QualityRoutines;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class CertificateCurrentUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class CommercialCertificateRepositoryTests
{
    private static ApplicationDbContext Context() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new CertificateCurrentUser());

    [Fact]
    public async Task Material_certificate_waits_for_both_tracks_and_uses_reportable_items()
    {
        await using var context = Context();
        var material = new Material { Id = Guid.NewGuid(), Name = "Alginate" };
        var batch = new MaterialBatch
        {
            Id = Guid.NewGuid(), MaterialId = material.Id, Material = material
        };
        var stp = new MaterialStandardTestProcedure
        {
            Id = Guid.NewGuid(), MaterialId = material.Id, Material = material
        };
        var chemicalFormId = Guid.NewGuid();
        var microbialFormId = Guid.NewGuid();
        var chemicalFieldId = Guid.NewGuid();
        var microbialFieldId = Guid.NewGuid();
        var chemicalForm = FormWithField(
            chemicalFormId, chemicalFieldId, "Chemical", "Assay");
        var microbialForm = FormWithField(
            microbialFormId, microbialFieldId, "Microbial", "TAMC");
        var chemical = Ard(stp, chemicalFormId, chemicalFieldId,
            AnalysisType.Chemical, "Assay");
        var microbial = Ard(stp, microbialFormId, microbialFieldId,
            AnalysisType.Microbial, "TAMC");
        var sample = new MaterialSampling
        {
            Id = Guid.NewGuid(), MaterialBatchId = batch.Id,
            MaterialBatch = batch, SampleDate = DateTime.UtcNow,
            ChemicalArdId = chemical.Id, MicrobialArdId = microbial.Id,
            MicrobialRequired = true
        };
        var chemicalResponse = Response(sample, chemicalFormId, chemicalFieldId, "99.5%");
        context.AddRange(material, batch, stp, chemicalForm, microbialForm,
            chemical, microbial, sample, chemicalResponse);
        await context.SaveChangesAsync();
        var repository = new CommercialCertificateRepository(context);

        var pending = await repository.GenerateForMaterial(sample.Id, Guid.NewGuid());
        Assert.True(pending.IsFailure);
        Assert.Equal(sample.Id, await context.MaterialSamplings
            .Select(item => item.Id).SingleAsync());

        context.Responses.Add(Response(
            sample, microbialFormId, microbialFieldId, "<10 cfu/g"));
        await context.SaveChangesAsync();
        var scopedResponses = await context.Responses.Where(item =>
            item.MaterialSamplingId == sample.Id).ToListAsync();
        Assert.Equal(2, scopedResponses.Count);
        Assert.All(scopedResponses, item => Assert.True(item.Approved));
        Assert.Contains(scopedResponses, item => item.FormId == chemicalFormId);
        Assert.Contains(scopedResponses, item => item.FormId == microbialFormId);
        var issued = await repository.GenerateForMaterial(sample.Id, Guid.NewGuid());

        Assert.True(issued.IsSuccess,
            string.Join("; ", issued.Errors.Select(error =>
                error.Code + ": " + error.Description)));
        var certificate = await context.CommercialCertificates.SingleAsync();
        Assert.True(certificate.Combined);
        using var rows = JsonDocument.Parse(certificate.RowsJson);
        Assert.Equal(2, rows.RootElement.GetArrayLength());
        Assert.Contains(rows.RootElement.EnumerateArray(),
            row => row.GetProperty("DisplayLabel").GetString() == "TAMC");
        Assert.True((await repository.GenerateForMaterial(
            sample.Id, Guid.NewGuid())).IsFailure);
    }

    private static Form FormWithField(
        Guid formId, Guid fieldId, string formName, string label)
    {
        var form = new Form { Id = formId, Name = formName };
        var section = new FormSection
        {
            Id = Guid.NewGuid(), FormId = formId, Form = form,
            Name = formName, Fields = []
        };
        var question = new Question
        {
            Id = Guid.NewGuid(), Label = label,
            Type = QuestionType.ShortAnswer
        };
        section.Fields.Add(new FormField
        {
            Id = fieldId, FormSectionId = section.Id, FormSection = section,
            QuestionId = question.Id, Question = question,
            Description = label
        });
        form.Sections.Add(section);
        return form;
    }

    private static MaterialAnalyticalRawData Ard(
        MaterialStandardTestProcedure stp, Guid formId, Guid fieldId,
        AnalysisType type, string label)
    {
        var ard = new MaterialAnalyticalRawData
        {
            Id = Guid.NewGuid(), StpId = stp.Id,
            MaterialStandardTestProcedure = stp,
            FormId = formId, AnalysisType = type, IsVerified = true
        };
        ard.CoaItems.Add(new CommercialCoaItem
        {
            Id = Guid.NewGuid(), MaterialArdId = ard.Id, MaterialArd = ard,
            FormFieldId = fieldId, DisplayLabel = label,
            SpecificationText = "Controlled limit", DisplayOrder = 0
        });
        return ard;
    }

    private static Response Response(
        MaterialSampling sample, Guid formId, Guid fieldId, string value) => new()
    {
        Id = Guid.NewGuid(), FormId = formId,
        MaterialBatchId = sample.MaterialBatchId,
        MaterialSamplingId = sample.Id,
        Approved = true,
        FormResponses =
        [
            new FormResponse
            {
                Id = Guid.NewGuid(), FormFieldId = fieldId,
                Value = value, Complies = true
            }
        ]
    };
}

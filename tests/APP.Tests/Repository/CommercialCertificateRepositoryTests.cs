using System.Text.Json;
using APP.Repository;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Designations;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.Users;
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
        // The batch facts and signatories are fixed when the certificate is issued.
        using (var header = JsonDocument.Parse(certificate.HeaderJson))
            Assert.Equal("Alginate", header.RootElement.GetProperty("name").GetString());
        using var rows = JsonDocument.Parse(certificate.RowsJson);
        Assert.Equal(2, rows.RootElement.GetArrayLength());
        Assert.Contains(rows.RootElement.EnumerateArray(),
            row => row.GetProperty("DisplayLabel").GetString() == "TAMC");
        Assert.True((await repository.GenerateForMaterial(
            sample.Id, Guid.NewGuid())).IsFailure);
    }

    [Fact]
    public async Task The_issued_header_fixes_the_batch_facts_and_who_analysed_checked_and_approved()
    {
        await using var context = Context();
        var analyst = new User { Id = Guid.NewGuid(), FirstName = "Richmond", LastName = "Ocran Danso" };
        var checker = new User { Id = Guid.NewGuid(), FirstName = "Michael", LastName = "Arthur" };
        var approver = new User { Id = Guid.NewGuid(), FirstName = "Nandkumar", LastName = "Chavan" };
        var designation = new Designation { Id = Guid.NewGuid(), Name = "GM-QA" };
        var material = new Material { Id = Guid.NewGuid(), Name = "Sucrose", Code = "RM-12" };
        var batch = new MaterialBatch { Id = Guid.NewGuid(), MaterialId = material.Id, Material = material, BatchNumber = "L001K076",
            ManufacturingDate = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc), ExpiryDate = new DateTime(2029, 10, 1, 0, 0, 0, DateTimeKind.Utc) };
        var sample = new MaterialSampling { Id = Guid.NewGuid(), MaterialBatchId = batch.Id, MaterialBatch = batch, ArNumber = "LQC/RM/26/0503",
            SampleDate = new DateTime(2026, 11, 6, 0, 0, 0, DateTimeKind.Utc) };
        var formId = Guid.NewGuid(); var fieldId = Guid.NewGuid();
        var response = Response(sample, formId, fieldId, "Complies");
        response.CheckedById = checker.Id;
        response.CheckedAt = new DateTime(2026, 11, 7, 0, 0, 0, DateTimeKind.Utc);
        context.AddRange(analyst, checker, approver, designation, material, batch, sample, FormWithField(formId, fieldId, "Chemical", "Description"), response,
            new Employee { Id = Guid.NewGuid(), UserId = approver.Id, DesignationId = designation.Id, Designation = designation, FirstName = "Nandkumar", LastName = "Chavan" },
            new ResponseApproval { Id = Guid.NewGuid(), ResponseId = response.Id, ApprovalId = Guid.NewGuid(), Required = true, Order = 1,
                Status = ApprovalStatus.Approved, ApprovedById = approver.Id, ApprovalTime = DateTime.UtcNow });
        await context.SaveChangesAsync();
        // Saving stamps the signed-in user as author; in the app that is the analyst entering the reading.
        response.FormResponses[0].CreatedById = analyst.Id;
        await context.SaveChangesAsync();

        using var header = JsonDocument.Parse(await CommercialCertificateHeader.BuildAsync(context, sample.Id, null, [response]));
        var root = header.RootElement;
        Assert.Equal("Material", root.GetProperty("subject").GetString());
        Assert.Equal(("Sucrose", "RM-12", "L001K076", "LQC/RM/26/0503"),
            (root.GetProperty("name").GetString(), root.GetProperty("code").GetString(), root.GetProperty("batchNumber").GetString(), root.GetProperty("arNumber").GetString()));
        Assert.Equal(new DateTime(2029, 10, 1, 0, 0, 0, DateTimeKind.Utc), root.GetProperty("expiryDate").GetDateTime());
        Assert.Equal(new DateTime(2026, 11, 7, 0, 0, 0, DateTimeKind.Utc), root.GetProperty("analysedAt").GetDateTime());
        Assert.Equal("Richmond Ocran Danso", root.GetProperty("analysedBy")[0].GetProperty("name").GetString());
        Assert.Equal("Michael Arthur", root.GetProperty("checkedBy")[0].GetProperty("name").GetString());
        var signed = root.GetProperty("approvedBy")[0];
        Assert.Equal(("Nandkumar Chavan", "GM-QA"), (signed.GetProperty("name").GetString(), signed.GetProperty("designation").GetString()));
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

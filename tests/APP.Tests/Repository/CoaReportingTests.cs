using System.Reflection;
using System.Text.Json;
using API.Controllers;
using APP.Repository;
using APP.Services.Forms.CoaReporting;
using APP.Utils;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.MaterialARD;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.MaterialSampling;
using DOMAIN.Entities.MaterialStandardTestProcedures;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.ProductSpecifications;
using DOMAIN.Entities.QualityRoutines;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file sealed class CoaReportingActor(Guid id) : ICurrentUserService
{
    public Guid? UserId => id;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

/// <summary>"Report on COA" on template fields, its product specification override, and certificates leaving unreported tests out.</summary>
public sealed class CoaReportingTests
{
    [Theory]
    [InlineData(nameof(SpecificationCoaReportingController.Get), PermissionKeys.CanViewProductCoaReportedTests)]
    [InlineData(nameof(SpecificationCoaReportingController.Save), PermissionKeys.CanEditProductCoaReportedTests)]
    public void Each_endpoint_has_its_own_registered_permission(string action, string key)
    {
        var method = typeof(SpecificationCoaReportingController).GetMethod(action)!;
        Assert.Equal(key, Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());
        Assert.Empty(typeof(SpecificationCoaReportingController).GetCustomAttributes<AllowAnonymousAttribute>());
        Assert.Single(PermissionUtils.GeneratePermissions(), permission => permission.Key == key);
    }

    [Fact]
    public async Task Saving_overrides_without_the_edit_key_is_refused()
    {
        var f = new Fixture(PermissionKeys.CanViewProductCoaReportedTests, PermissionKeys.CanEditProductSpecification);
        var refused = await new SpecificationCoaReportingService(f.Db).Save(f.Spec.Id, AnalysisType.Microbial, f.HideTamc(), f.User);
        Assert.Equal(ErrorType.Forbidden, Assert.Single(refused.Errors).Type);
        Assert.Empty(f.Db.SpecificationCoaReportings);
        Assert.True((await f.Db.ProductSpecifications.SingleAsync(spec => spec.Id == f.Spec.Id)).IsVerified);
    }

    [Fact]
    public async Task Saved_overrides_cover_every_row_of_the_specification_number_and_need_approval_again()
    {
        var f = new Fixture(PermissionKeys.CanEditProductCoaReportedTests);
        var request = f.HideTamc();
        // Same as the template: accepted but not stored, so later template changes still reach it.
        request.Overrides.Add(new CoaReportingOverrideRequest { FormFieldId = f.EColiField, ReportOnCoa = true });
        var saved = await new SpecificationCoaReportingService(f.Db).Save(f.Spec.Id, AnalysisType.Microbial, request, f.User);

        Assert.True(saved.IsSuccess, string.Join("; ", saved.Errors.Select(error => error.Description)));
        var rows = await f.Db.SpecificationCoaReportings.ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, row => Assert.Equal((f.TamcField, false), (row.FormFieldId, row.ReportOnCoa)));
        Assert.All(await f.Db.ProductSpecifications.ToListAsync(), spec => Assert.False(spec.IsVerified));
        var tamc = saved.Value.Fields.Single(field => field.FormFieldId == f.TamcField);
        Assert.Equal((true, (bool?)false, false), (tamc.TemplateReportOnCoa, tamc.Override, tamc.ReportOnCoa));
        Assert.False(saved.Value.Fields.Single(field => field.FormFieldId == f.ConclusionField).ReportOnCoa);
    }

    [Fact]
    public async Task An_override_on_a_field_outside_the_template_is_refused()
    {
        var f = new Fixture(PermissionKeys.CanEditProductCoaReportedTests);
        var request = new SaveCoaReportingRequest { Reason = "Report the tests the customer requires", Overrides = [new() { FormFieldId = Guid.NewGuid(), ReportOnCoa = true }] };
        var refused = await new SpecificationCoaReportingService(f.Db).Save(f.Spec.Id, AnalysisType.Microbial, request, f.User);
        Assert.Equal("CoaReporting.Field", Assert.Single(refused.Errors).Code);
    }

    [Fact]
    public async Task Hidden_fields_follow_the_template_unless_the_specification_overrides_them()
    {
        var f = new Fixture(PermissionKeys.CanEditProductCoaReportedTests);
        Assert.Equal([f.ConclusionField], await CoaReportingRuntime.HiddenFields(f.Db, f.Response));

        var request = f.HideTamc();
        request.Overrides.Add(new CoaReportingOverrideRequest { FormFieldId = f.ConclusionField, ReportOnCoa = true });
        Assert.True((await new SpecificationCoaReportingService(f.Db).Save(f.Spec.Id, AnalysisType.Microbial, request, f.User)).IsSuccess);

        Assert.Equal([f.TamcField], await CoaReportingRuntime.HiddenFields(f.Db, f.Response));
    }

    [Fact]
    public async Task The_combined_certificate_leaves_out_tests_not_reported_on_the_coa()
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new CoaReportingActor(Guid.NewGuid()));
        var material = new Material { Id = Guid.NewGuid(), Name = "Alginate" };
        var batch = new MaterialBatch { Id = Guid.NewGuid(), MaterialId = material.Id, Material = material };
        var stp = new MaterialStandardTestProcedure { Id = Guid.NewGuid(), MaterialId = material.Id, Material = material };
        var form = new Form { Id = Guid.NewGuid(), Name = "Microbial" };
        var tamc = Field(form, "TAMC", reportOnCoa: true);
        var media = Field(form, "Culture media verification", reportOnCoa: false);
        var ard = new MaterialAnalyticalRawData
        {
            Id = Guid.NewGuid(), StpId = stp.Id, MaterialStandardTestProcedure = stp, FormId = form.Id,
            AnalysisType = AnalysisType.Chemical, IsVerified = true,
        };
        foreach (var (field, index) in new[] { (tamc, 0), (media, 1) })
            ard.CoaItems.Add(new CommercialCoaItem
            {
                Id = Guid.NewGuid(), MaterialArdId = ard.Id, FormFieldId = field.Id, DisplayLabel = field.Question.Label,
                SpecificationText = "Controlled limit", DisplayOrder = index,
            });
        var sample = new MaterialSampling
        {
            Id = Guid.NewGuid(), MaterialBatchId = batch.Id, MaterialBatch = batch, SampleDate = DateTime.UtcNow,
            ChemicalArdId = ard.Id, MicrobialRequired = false,
        };
        var response = new Response
        {
            Id = Guid.NewGuid(), FormId = form.Id, MaterialBatchId = batch.Id, MaterialSamplingId = sample.Id, Approved = true,
            FormResponses =
            [
                new FormResponse { Id = Guid.NewGuid(), FormFieldId = tamc.Id, Value = "<10 cfu/g", Complies = true },
                // Not reported, so its missing result does not block the certificate.
                new FormResponse { Id = Guid.NewGuid(), FormFieldId = media.Id, Value = "-", Complies = null },
            ],
        };
        context.AddRange(material, batch, stp, form, ard, sample, response);
        await context.SaveChangesAsync();

        var issued = await new CommercialCertificateRepository(context).GenerateForMaterial(sample.Id, Guid.NewGuid());

        Assert.True(issued.IsSuccess, string.Join("; ", issued.Errors.Select(error => error.Code + ": " + error.Description)));
        using var rows = JsonDocument.Parse((await context.CommercialCertificates.SingleAsync()).RowsJson);
        Assert.Equal("TAMC", Assert.Single(rows.RootElement.EnumerateArray()).GetProperty("DisplayLabel").GetString());
    }

    private static FormField Field(Form form, string label, bool reportOnCoa)
    {
        var section = new FormSection { Id = Guid.NewGuid(), FormId = form.Id, Form = form, Name = label, Fields = [] };
        var question = new Question { Id = Guid.NewGuid(), Label = label, Type = QuestionType.ShortAnswer };
        var field = new FormField { Id = Guid.NewGuid(), FormSectionId = section.Id, FormSection = section, QuestionId = question.Id, Question = question, ReportOnCoa = reportOnCoa };
        section.Fields.Add(field);
        form.Sections.Add(section);
        return field;
    }

    private sealed class Fixture
    {
        public readonly Guid User = Guid.NewGuid();
        public readonly ApplicationDbContext Db;
        public readonly Guid MicroForm = Guid.NewGuid(), TamcField = Guid.NewGuid(), EColiField = Guid.NewGuid(), ConclusionField = Guid.NewGuid();
        public ProductSpecification Spec = null!;
        public Response Response = null!;

        public Fixture(params string[] grants)
        {
            Db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new CoaReportingActor(User));
            var role = new Role { Id = Guid.NewGuid(), Name = "qc", NormalizedName = "QC", DisplayName = "qc" };
            Db.Roles.Add(role);
            Db.Users.Add(new User { Id = User, UserName = "qc", Email = "qc@example.com", FirstName = "Q", LastName = "C" });
            Db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = User, RoleId = role.Id });
            for (var index = 0; index < grants.Length; index++)
            {
                Db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = index + 1, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = grants[index] });
                Db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), Key = grants[index], RoleClaimId = index + 1, Type = "Access" });
            }
            Db.Forms.Add(new Form { Id = MicroForm, Name = "Paracetamol syrup microbial" });
            var order = 0;
            void Add(Guid id, string name, bool reportOnCoa)
            {
                var section = Guid.NewGuid();
                Db.FormSections.Add(new FormSection { Id = section, FormId = MicroForm, Name = name, Order = order++, AnalysisType = AnalysisType.Microbial });
                Db.FormFields.Add(new FormField { Id = id, FormSectionId = section, ReportOnCoa = reportOnCoa,
                    Question = new Question { Id = Guid.NewGuid(), Label = name, Type = QuestionType.ShortAnswer } });
            }
            Add(TamcField, "TAMC", true);
            Add(EColiField, "Escherichia coli", true);
            Add(ConclusionField, "Conclusion", false);
            var product = Guid.NewGuid(); var other = Guid.NewGuid(); var batch = Guid.NewGuid(); var bmr = Guid.NewGuid(); var step = Guid.NewGuid();
            Db.Products.AddRange(new Product { Id = product, Name = "Paracetamol 120mg Syrup" }, new Product { Id = other, Name = "Paracetamol 250mg Syrup" });
            Spec = new ProductSpecification { Id = Guid.NewGuid(), ProductId = product, SpecificationNumber = "QCD/SPC/FP/120", FormId = Guid.NewGuid(), MicrobialFormId = MicroForm, IsVerified = true, TestStage = TestStage.Finished };
            var shared = new ProductSpecification { Id = Guid.NewGuid(), ProductId = other, SpecificationNumber = "QCD/SPC/FP/120", FormId = Spec.FormId, MicrobialFormId = MicroForm, IsVerified = true, TestStage = TestStage.Finished };
            Db.ProductSpecifications.AddRange(Spec, shared);
            Db.ProductionScheduleProducts.Add(new ProductionScheduleProduct { Id = batch, ProductId = product });
            Db.ProductionActivitySteps.Add(new ProductionActivityStep { Id = step });
            Db.BatchManufacturingRecords.Add(new BatchManufacturingRecord { Id = bmr, ProductionScheduleProductId = batch, ProductionActivityStepId = step, BatchNumber = "B1" });
            Db.AnalyticalTestRequests.Add(new AnalyticalTestRequest { Id = Guid.NewGuid(), ProductionScheduleProductId = batch, BatchManufacturingRecordId = bmr, ProductionActivityStepId = step, Stage = TestStage.Finished });
            Response = new Response { Id = Guid.NewGuid(), FormId = MicroForm, BatchManufacturingRecordId = bmr, ProductionActivityStepId = step };
            Db.Responses.Add(Response);
            Db.SaveChanges();
        }

        public SaveCoaReportingRequest HideTamc() => new()
        {
            Reason = "The customer certificate reports E. coli only",
            Overrides = [new CoaReportingOverrideRequest { FormFieldId = TamcField, ReportOnCoa = false }],
        };
    }
}

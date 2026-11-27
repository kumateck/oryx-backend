using APP.Services.Forms.Microbial;
using APP.Utils;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.MaterialSpecifications;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Materials.Batch;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file sealed class MicrobialActor(Guid id) : ICurrentUserService
{
    public Guid? UserId => id;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

/// <summary>Microbial question types, CFU calculation, typed specification limits and their enforcement.</summary>
public sealed class MicrobialTestingTests
{
    private static SpecificationMicrobialLimit Nmt(decimal value, string unit = "cfu/g") => new() { Kind = MicrobialLimitKind.NotMoreThan, Value = value, Unit = unit };
    private static SpecificationMicrobialLimit Absent(string organism = "escherichia-coli") => new() { Kind = MicrobialLimitKind.Absent, Organism = organism, Quantity = 1, QuantityUnit = "g" };

    [Theory]
    [InlineData(QuestionType.Organism, "escherichia-coli", true)]
    [InlineData(QuestionType.Organism, "E. coli", false)]
    [InlineData(QuestionType.GrowthObservation, "Absent", true)]
    [InlineData(QuestionType.GrowthObservation, "No growth", false)]
    [InlineData(QuestionType.Dilution, "{\"sample\":10,\"sampleUnit\":\"g\",\"diluent\":90,\"factor\":10}", true)]
    [InlineData(QuestionType.Dilution, "{\"factor\":0}", false)]
    [InlineData(QuestionType.ColonyCount, "{\"plates\":[12,15]}", true)]
    [InlineData(QuestionType.ColonyCount, "{\"plates\":[-1]}", false)]
    [InlineData(QuestionType.ColonyCount, "{\"plates\":[],\"tooNumerousToCount\":true}", true)]
    [InlineData(QuestionType.Incubation, "{\"temperature\":35,\"hours\":120,\"startedAt\":\"2026-11-20T08:00:00Z\"}", true)]
    [InlineData(QuestionType.Incubation, "{\"temperature\":35,\"hours\":120,\"startedAt\":\"2026-11-20T08:00:00Z\",\"endedAt\":\"2026-11-19T08:00:00Z\"}", false)]
    public void Analyst_answers_are_checked_by_type(QuestionType type, string value, bool valid) =>
        Assert.Equal(valid, MicrobialValues.Validate(type, value).IsSuccess);

    [Fact]
    public void An_incubation_is_complete_only_once_it_has_ended()
    {
        Assert.False(MicrobialValues.IsComplete(QuestionType.Incubation, "{\"temperature\":35,\"hours\":120,\"startedAt\":\"2026-11-20T08:00:00Z\"}"));
        Assert.True(MicrobialValues.IsComplete(QuestionType.Incubation, "{\"temperature\":35,\"hours\":120,\"startedAt\":\"2026-11-20T08:00:00Z\",\"endedAt\":\"2026-11-25T08:00:00Z\"}"));
    }

    [Fact]
    public void Cfu_is_the_average_plate_count_times_the_dilution_factor()
    {
        var cfu = MicrobialValues.Calculate(MicrobialValues.ReadColonyCount("{\"plates\":[12,15]}")!, MicrobialValues.ReadDilution("{\"factor\":10}")!, "cfu/mL");
        Assert.Equal((13.5m, 135m, "cfu/mL"), (cfu.Average, cfu.Result, cfu.Unit));
        Assert.Equal(cfu, MicrobialValues.ReadCfu(MicrobialValues.Serialize(cfu)));
    }

    [Fact]
    public void A_cfu_question_names_one_known_unit_and_other_microbial_types_have_no_options()
    {
        Assert.True(MicrobialValues.ValidateQuestion(QuestionType.CfuCalculation, ["cfu/g"]).IsSuccess);
        Assert.False(MicrobialValues.ValidateQuestion(QuestionType.CfuCalculation, []).IsSuccess);
        Assert.False(MicrobialValues.ValidateQuestion(QuestionType.CfuCalculation, ["cfu"]).IsSuccess);
        Assert.False(MicrobialValues.ValidateQuestion(QuestionType.GrowthObservation, ["Absent"]).IsSuccess);
    }

    [Fact]
    public void Limits_print_as_the_laboratory_writes_them()
    {
        Assert.Equal("NMT 1000 cfu/g", MicrobialLimits.Text(Nmt(1000)));
        Assert.Equal("Absence of Escherichia coli in 1 g", MicrobialLimits.Text(Absent()));
    }

    [Fact]
    public void Count_limits_compare_the_calculated_result_and_never_pass_an_unreadable_one()
    {
        string Cfu(decimal result, string unit = "cfu/g", bool tntc = false) => MicrobialValues.Serialize(new MicrobialValues.CfuValue(result / 10, 10, tntc ? null : result, tntc, unit));
        Assert.Equal(MicrobialLimits.Verdict.Complies, MicrobialLimits.Evaluate(Nmt(200), Cfu(200)));
        Assert.Equal(MicrobialLimits.Verdict.DoesNotComply, MicrobialLimits.Evaluate(Nmt(200), Cfu(210)));
        Assert.Equal(MicrobialLimits.Verdict.DoesNotComply, MicrobialLimits.Evaluate(Nmt(200), Cfu(0, tntc: true)));
        Assert.Equal(MicrobialLimits.Verdict.Unresolved, MicrobialLimits.Evaluate(Nmt(200), Cfu(100, "cfu/mL")));
        Assert.Equal(MicrobialLimits.Verdict.Unresolved, MicrobialLimits.Evaluate(Nmt(200), "135"));
        Assert.Equal(MicrobialLimits.Verdict.Complies, MicrobialLimits.Evaluate(Absent(), "Absent"));
        Assert.Equal(MicrobialLimits.Verdict.DoesNotComply, MicrobialLimits.Evaluate(Absent(), "Present"));
        Assert.Equal(MicrobialLimits.Verdict.Unresolved, MicrobialLimits.Evaluate(Absent(), null));
    }

    [Fact]
    public void A_limit_must_suit_its_field()
    {
        var count = new MicrobialLimitRequest { Kind = MicrobialLimitKind.NotMoreThan, Value = 100, Unit = "cfu/g" };
        Assert.True(MicrobialLimits.Validate(count, QuestionType.CfuCalculation, "cfu/g").IsSuccess);
        Assert.False(MicrobialLimits.Validate(count, QuestionType.CfuCalculation, "cfu/mL").IsSuccess);
        Assert.False(MicrobialLimits.Validate(count, QuestionType.GrowthObservation, null).IsSuccess);
        Assert.False(MicrobialLimits.Validate(new MicrobialLimitRequest { Kind = MicrobialLimitKind.Absent, Organism = "salmonella", Quantity = 10, QuantityUnit = "kg" },
            QuestionType.GrowthObservation, null).IsSuccess);
    }

    [Fact]
    public void A_cfu_test_needs_one_colony_count_and_one_dilution_in_its_section()
    {
        FormField Field(QuestionType type) => new() { Question = new Question { Label = type.ToString(), Type = type } };
        var section = new FormSection { Name = "TAMC", Fields = [Field(QuestionType.ColonyCount), Field(QuestionType.CfuCalculation)] };
        Assert.Equal("Form.MicrobialSection", Assert.Single(FormValidator.MicrobialSectionErrors("Micro", section)).Code);
        section.Fields.Add(Field(QuestionType.Dilution));
        Assert.Empty(FormValidator.MicrobialSectionErrors("Micro", section));
    }

    private sealed class Fixture
    {
        public readonly Guid User = Guid.NewGuid();
        public readonly ApplicationDbContext Db;
        public Guid MicroForm, CfuField, GrowthField, CountField, DilutionField, CfuSection, GrowthSection, Batch;
        public MaterialSpecification Spec = null!, Shared = null!;
        private static int _claim = 70000;

        public Fixture(params string[] grants)
        {
            Db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new MicrobialActor(User));
            var role = new Role { Id = Guid.NewGuid(), Name = "qc", NormalizedName = "QC", DisplayName = "qc" };
            Db.Roles.Add(role);
            Db.Users.Add(new User { Id = User, UserName = "qc", Email = "qc@example.com", FirstName = "Q", LastName = "C" });
            Db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = User, RoleId = role.Id });
            foreach (var key in grants)
            {
                var claim = new IdentityRoleClaim<Guid> { Id = ++_claim, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = key };
                Db.RoleClaims.Add(claim);
                Db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), Key = key, RoleClaimId = claim.Id, Type = "Access" });
            }
            MicroForm = Guid.NewGuid(); CfuSection = Guid.NewGuid(); GrowthSection = Guid.NewGuid();
            Db.Forms.Add(new Form { Id = MicroForm, Name = "Microbial limits" });
            Db.FormSections.AddRange(new FormSection { Id = CfuSection, FormId = MicroForm, Name = "TAMC", Order = 0, AnalysisType = DOMAIN.Entities.QualityRoutines.AnalysisType.Microbial },
                new FormSection { Id = GrowthSection, FormId = MicroForm, Name = "Absence of E. coli", Order = 1, AnalysisType = DOMAIN.Entities.QualityRoutines.AnalysisType.Microbial });
            Guid Field(Guid section, QuestionType type, string? unit = null)
            {
                var question = new Question { Id = Guid.NewGuid(), Label = type.ToString(), Type = type };
                if (unit is not null) question.Options.Add(new QuestionOption { Id = Guid.NewGuid(), Name = unit });
                var field = new FormField { Id = Guid.NewGuid(), FormSectionId = section, Question = question };
                Db.FormFields.Add(field);
                return field.Id;
            }
            CountField = Field(CfuSection, QuestionType.ColonyCount);
            DilutionField = Field(CfuSection, QuestionType.Dilution);
            CfuField = Field(CfuSection, QuestionType.CfuCalculation, "cfu/g");
            GrowthField = Field(GrowthSection, QuestionType.GrowthObservation);
            var material = new Material { Id = Guid.NewGuid(), Name = "Maize starch", Code = "RM-1", Kind = MaterialKind.Raw };
            var other = new Material { Id = Guid.NewGuid(), Name = "Corn starch", Code = "RM-2", Kind = MaterialKind.Raw };
            Spec = new MaterialSpecification { Id = Guid.NewGuid(), SpecificationNumber = "QCD/SPC/RM/050", MaterialId = material.Id, FormId = Guid.NewGuid(), MicrobialFormId = MicroForm, IsVerified = true };
            Shared = new MaterialSpecification { Id = Guid.NewGuid(), SpecificationNumber = "QCD/SPC/RM/050", MaterialId = other.Id, FormId = Spec.FormId, MicrobialFormId = MicroForm, IsVerified = true };
            Batch = Guid.NewGuid();
            Db.Materials.AddRange(material, other);
            Db.MaterialSpecifications.AddRange(Spec, Shared);
            Db.MaterialBatches.Add(new MaterialBatch { Id = Batch, MaterialId = material.Id });
            Db.SaveChanges();
        }

        public SaveMicrobialLimitsRequest Limits() => new()
        {
            Reason = "Set limits from the BP monograph",
            Limits =
            [
                new MicrobialLimitRequest { FormFieldId = CfuField, Kind = MicrobialLimitKind.NotMoreThan, Value = 1000, Unit = "cfu/g" },
                new MicrobialLimitRequest { FormFieldId = GrowthField, Kind = MicrobialLimitKind.Absent, Organism = "escherichia-coli", Quantity = 1, QuantityUnit = "g" },
            ],
        };
    }

    [Fact]
    public async Task Saving_limits_needs_the_edit_key_for_the_material_kind()
    {
        var denied = new Fixture(PermissionKeys.CanEditPackagingMaterialMicrobialLimits);
        var refused = await new SpecificationMicrobialLimitService(denied.Db).Save(SpecificationMicrobialLimitService.Subject.Material, denied.Spec.Id, denied.Limits(), denied.User);
        Assert.Equal("MicrobialLimit.Permission", Assert.Single(refused.Errors).Code);
        Assert.Empty(denied.Db.SpecificationMicrobialLimits);
    }

    [Fact]
    public async Task Saved_limits_cover_every_row_of_the_specification_number_and_need_approval_again()
    {
        var f = new Fixture(PermissionKeys.CanEditRawMaterialMicrobialLimits);
        var saved = await new SpecificationMicrobialLimitService(f.Db).Save(SpecificationMicrobialLimitService.Subject.Material, f.Spec.Id, f.Limits(), f.User);
        Assert.True(saved.IsSuccess, string.Join("; ", saved.Errors?.Select(error => error.Description) ?? []));
        Assert.Equal(["NMT 1000 cfu/g", "Absence of Escherichia coli in 1 g"], saved.Value.Limits.Select(limit => limit.Text));
        Assert.Equal([f.CfuField, f.GrowthField], saved.Value.Fields.Select(field => field.FormFieldId));
        Assert.Equal(2, await f.Db.SpecificationMicrobialLimits.CountAsync(limit => limit.MaterialSpecificationId == f.Shared.Id));
        Assert.False(await f.Db.MaterialSpecifications.AnyAsync(spec => spec.IsVerified));
    }

    [Fact]
    public async Task A_limit_on_a_field_outside_the_microbial_template_is_refused()
    {
        var f = new Fixture(PermissionKeys.CanEditRawMaterialMicrobialLimits);
        var request = f.Limits();
        request.Limits[0].FormFieldId = f.CountField;
        var refused = await new SpecificationMicrobialLimitService(f.Db).Save(SpecificationMicrobialLimitService.Subject.Material, f.Spec.Id, request, f.User);
        Assert.Equal("MicrobialLimit.Field", Assert.Single(refused.Errors).Code);
    }

    [Fact]
    public async Task The_server_calculates_cfu_and_sets_compliance_from_the_limits()
    {
        var f = new Fixture(PermissionKeys.CanEditRawMaterialMicrobialLimits);
        await new SpecificationMicrobialLimitService(f.Db).Save(SpecificationMicrobialLimitService.Subject.Material, f.Spec.Id, f.Limits(), f.User);
        var response = new Response
        {
            Id = Guid.NewGuid(), FormId = f.MicroForm, MaterialBatchId = f.Batch,
            FormResponses =
            [
                new FormResponse { FormFieldId = f.CountField, Value = "{\"plates\":[120,150]}", Complies = true },
                new FormResponse { FormFieldId = f.DilutionField, Value = "{\"factor\":10}", Complies = true },
                new FormResponse { FormFieldId = f.GrowthField, Value = "Absent", Complies = false },
            ],
        };
        f.Db.Responses.Add(response);
        await MicrobialCalculation.Apply(f.Db, response, f.CfuSection, null);
        await f.Db.SaveChangesAsync();
        var cfu = MicrobialValues.ReadCfu(response.FormResponses.Single(answer => answer.FormFieldId == f.CfuField).Value)!;
        Assert.Equal(1350m, cfu.Result);

        var applied = await MicrobialLimitRuntime.Apply(f.Db, response);
        Assert.True(applied.IsSuccess);
        // 1350 cfu/g exceeds NMT 1000 cfu/g, whatever the analyst ticked; E. coli was absent although the box said otherwise.
        Assert.All(response.FormResponses.Where(answer => answer.FormFieldId != f.GrowthField), answer => Assert.False(answer.Complies));
        Assert.True(response.FormResponses.Single(answer => answer.FormFieldId == f.GrowthField).Complies);
        Assert.Equal("NMT 1000 cfu/g", (await MicrobialLimitRuntime.Texts(f.Db, response))[f.CfuField]);
    }

    [Fact]
    public async Task A_certificate_cannot_be_checked_while_a_limited_result_is_missing()
    {
        var f = new Fixture(PermissionKeys.CanEditRawMaterialMicrobialLimits);
        await new SpecificationMicrobialLimitService(f.Db).Save(SpecificationMicrobialLimitService.Subject.Material, f.Spec.Id, f.Limits(), f.User);
        var response = new Response { Id = Guid.NewGuid(), FormId = f.MicroForm, MaterialBatchId = f.Batch, FormResponses = [new FormResponse { FormFieldId = f.GrowthField, Value = "Absent" }] };
        f.Db.Responses.Add(response);
        await f.Db.SaveChangesAsync();
        Assert.Equal("MicrobialLimit.Unresolved", Assert.Single((await MicrobialLimitRuntime.Apply(f.Db, response)).Errors).Code);
    }
}

using System.Reflection;
using API.Controllers;
using APP.Services.Forms;
using APP.Utils;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.MaterialSampling;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public sealed class TemplateRepairTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId { get; init; } = Guid.NewGuid();
        public Guid? DepartmentId { get; } = Guid.NewGuid();
        public string DepartmentType => "QualityControl";
    }

    /// <summary>
    /// One template with an old field (given to the analyst on every test) and a field added after the tests were assigned,
    /// used by an open product ATR (with a saved answer), an open raw material GRN test, an open packaging retest and a released ATR.
    /// </summary>
    private sealed class Fixture : IAsyncDisposable
    {
        public Actor Actor { get; } = new();
        public ApplicationDbContext Db { get; }
        public TemplateRepairService Service { get; }
        public Guid Role { get; } = Guid.NewGuid();
        public Guid Analyst { get; } = Guid.NewGuid();
        public Guid Form { get; } = Guid.NewGuid();
        public Guid OldField { get; } = Guid.NewGuid();
        public Guid NewField { get; } = Guid.NewGuid();
        public Guid AtrTest { get; } = Guid.NewGuid();
        public Guid GrnTest { get; } = Guid.NewGuid();
        public Guid RetestTest { get; } = Guid.NewGuid();
        public Guid ReleasedTest { get; } = Guid.NewGuid();
        public Guid AtrResponse { get; } = Guid.NewGuid();
        private int claim;

        public Fixture(string? connection = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>();
            if (connection is null) options.UseInMemoryDatabase(Guid.NewGuid().ToString(), o => o.EnableNullChecks(false));
            else options.UseNpgsql(connection);
            Db = new(options.Options, Actor);
            Service = new(Db, Actor);
        }

        public async Task Grant(params string[] keys)
        {
            foreach (var key in keys)
            {
                claim++;
                Db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = claim, RoleId = Role, ClaimType = AppConstants.Permission, ClaimValue = key });
                Db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = claim, Key = key, Type = "Action" });
            }
            await Db.SaveChangesAsync();
        }

        public async Task Revoke(string key)
        {
            Db.RoleClaims.RemoveRange(Db.RoleClaims.Where(c => c.ClaimValue == key));
            await Db.SaveChangesAsync();
        }

        public async Task Seed()
        {
            Db.Users.Add(new User { Id = Actor.UserId!.Value, UserName = "qc-supervisor", FirstName = "Quality", LastName = "Lead" });
            Db.Users.Add(new User { Id = Analyst, UserName = "analyst", FirstName = "Ama", LastName = "Analyst" });
            Db.Roles.Add(new Role { Id = Role, Name = "QC supervisor" });
            Db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = Actor.UserId.Value, RoleId = Role });
            await Db.SaveChangesAsync();
            await Grant(PermissionKeys.CanViewTemplateRepair, PermissionKeys.CanApplyTemplateRepair, PermissionKeys.CanReassignProductTest,
                PermissionKeys.CanReassignRawMaterialTest, PermissionKeys.CanReassignPackagingMaterialTest);

            var section = Guid.NewGuid(); var oldQuestion = Guid.NewGuid(); var newQuestion = Guid.NewGuid();
            Db.Forms.Add(new Form { Id = Form, Name = "Paracetamol ARD" });
            Db.FormSections.Add(new FormSection { Id = section, FormId = Form, Name = "<p>Assay</p>", Order = 1 });
            Db.Questions.Add(new Question { Id = oldQuestion, Label = "Weight", Type = QuestionType.ShortAnswer });
            Db.Questions.Add(new Question { Id = newQuestion, Label = "Appearance", Type = QuestionType.ShortAnswer });
            Db.FormFields.Add(new FormField { Id = OldField, FormSectionId = section, QuestionId = oldQuestion, Rank = 1 });
            Db.FormFields.Add(new FormField { Id = NewField, FormSectionId = section, QuestionId = newQuestion, Rank = 2, Required = true });

            // An open product ATR, with a saved answer, and a released one.
            var product = Guid.NewGuid();
            Db.Products.Add(new Product { Id = product, Name = "Paracetamol" });
            foreach (var (id, status, released) in new[] { (AtrTest, AnalyticalTestStatus.Testing, false), (ReleasedTest, AnalyticalTestStatus.Released, true) })
            {
                var schedule = Guid.NewGuid(); var bmr = Guid.NewGuid(); var step = Guid.NewGuid();
                Db.ProductionScheduleProducts.Add(new ProductionScheduleProduct { Id = schedule, ProductId = product });
                Db.ProductionActivitySteps.Add(new ProductionActivityStep { Id = step });
                Db.BatchManufacturingRecords.Add(new BatchManufacturingRecord { Id = bmr, ProductionScheduleProductId = schedule, ProductionActivityStepId = step, BatchNumber = $"B-{status}" });
                Db.AnalyticalTestRequests.Add(new AnalyticalTestRequest { Id = Guid.NewGuid(), ProductionScheduleProductId = schedule, BatchManufacturingRecordId = bmr,
                    ProductionActivityStepId = step, ArNumber = $"AR-{status}", Status = status, ReleasedAt = released ? DateTime.UtcNow : null });
                Db.FormAssignees.Add(Assignment(id, a => { a.BatchManufacturingRecordId = bmr; a.ProductionActivityStepId = step; }));
                if (id == AtrTest)
                {
                    Db.Responses.Add(new Response { Id = AtrResponse, FormId = Form, BatchManufacturingRecordId = bmr, ProductionActivityStepId = step });
                    Db.FormResponses.Add(new FormResponse { Id = Guid.NewGuid(), ResponseId = AtrResponse, FormFieldId = OldField, Value = "500 mg" });
                }
            }

            // An open raw material GRN test.
            var raw = Guid.NewGuid(); var rawBatch = Guid.NewGuid();
            Db.Materials.Add(new Material { Id = raw, Name = "Starch", Kind = MaterialKind.Raw });
            Db.MaterialBatches.Add(new MaterialBatch { Id = rawBatch, MaterialId = raw, BatchNumber = "RM-1", Status = BatchStatus.Testing });
            Db.FormAssignees.Add(Assignment(GrnTest, a => a.MaterialBatchId = rawBatch));

            // An open packaging retest: its answers bind to the batch's newest sample, taken for the retest.
            var pack = Guid.NewGuid(); var packBatch = Guid.NewGuid(); var retest = Guid.NewGuid(); var sample = Guid.NewGuid();
            Db.Materials.Add(new Material { Id = pack, Name = "Foil", Kind = MaterialKind.Package });
            Db.MaterialBatches.Add(new MaterialBatch { Id = packBatch, MaterialId = pack, BatchNumber = "PM-1", Status = BatchStatus.Frozen });
            Db.MaterialRetestRequests.Add(new MaterialRetestRequest { Id = retest, MaterialBatchId = packBatch, Status = MaterialRetestStatus.Assigned, ArNumber = "RT-1" });
            Db.MaterialSamplings.Add(new MaterialSampling { Id = sample, MaterialBatchId = packBatch, MaterialRetestRequestId = retest, ChemicalArdId = Guid.NewGuid(), SampleDate = DateTime.UtcNow });
            Db.FormAssignees.Add(Assignment(RetestTest, a => { a.MaterialBatchId = packBatch; a.MaterialSamplingId = sample; }));
            await Db.SaveChangesAsync();
        }

        private FormAssignee Assignment(Guid id, Action<FormAssignee> context)
        {
            var assignee = new FormAssignee { Id = id, FormId = Form,
                FieldAssignees = [new FormFieldAssignee { Id = Guid.NewGuid(), FormFieldId = OldField, AssigneeId = Analyst }] };
            context(assignee);
            return assignee;
        }

        public TemplateRepairCandidate Candidate(params Guid[] tests) =>
            new((tests.Length == 0 ? [AtrTest, GrnTest, RetestTest] : tests).Select(t => new TemplateRepairAssignmentInput(t, NewField, Analyst)).ToList());

        public async Task<ApplyTemplateRepairRequest> Request(TemplateRepairCandidate? candidate = null)
        {
            candidate ??= Candidate();
            var preview = await Service.Preview(Form, candidate);
            Assert.True(preview.IsSuccess, preview.IsSuccess ? null : preview.Errors[0].Description);
            Assert.Empty(preview.Value.Blockers);
            return new(candidate, preview.Value.PreviewToken, Guid.NewGuid(), "Appearance was added to the template after assignment.");
        }

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    [Fact]
    public async Task Context_lists_open_tests_missing_the_new_field_with_the_section_analyst_suggested()
    {
        await using var f = new Fixture(); await f.Seed();
        var context = (await f.Service.Context(f.Form)).Value;
        Assert.True(context.CanApply);
        Assert.Equal(["Weight", "Appearance"], context.Fields.Select(x => x.Label));
        Assert.Equal("Assay", context.Fields[0].Section);
        Assert.Equal(new[] { f.AtrTest, f.GrnTest, f.RetestTest }.Order(), context.Tests.Select(t => t.FormAssigneeId).Order());
        Assert.All(context.Tests, t => { Assert.True(t.Eligible, t.Blocker); Assert.Equal(f.NewField, Assert.Single(t.Missing).FormFieldId); Assert.Equal(f.Analyst, t.Missing[0].SuggestedAssigneeId); });
        var retest = context.Tests.Single(t => t.FormAssigneeId == f.RetestTest);
        Assert.True(retest.Retest); Assert.Equal(TemplateRepairTestKind.PackagingMaterial, retest.Kind); Assert.StartsWith("Retest RT-1", retest.Label);
        Assert.Equal(1, context.Tests.Single(t => t.FormAssigneeId == f.AtrTest).AnsweredFields);
        Assert.Equal(TemplateRepairTestKind.RawMaterial, context.Tests.Single(t => t.FormAssigneeId == f.GrnTest).Kind);
    }

    [Fact]
    public async Task Apply_adds_the_new_field_to_each_open_test_keeps_answers_and_records_evidence()
    {
        await using var f = new Fixture(); await f.Seed();
        var request = await f.Request();
        var result = await f.Service.Apply(f.Form, request);
        Assert.True(result.IsSuccess, result.IsSuccess ? null : result.Errors[0].Description);
        Assert.Equal(3, result.Value.FieldsAdded);
        Assert.False(result.Value.AlreadyApplied);
        foreach (var test in new[] { f.AtrTest, f.GrnTest, f.RetestTest })
            Assert.Single(f.Db.FormFieldAssignees.Where(a => a.FormAssigneeId == test && a.FormFieldId == f.NewField && a.AssigneeId == f.Analyst));
        Assert.Empty(f.Db.FormFieldAssignees.Where(a => a.FormAssigneeId == f.ReleasedTest && a.FormFieldId == f.NewField));
        Assert.Equal("500 mg", (await f.Db.FormResponses.SingleAsync()).Value);
        var repair = await f.Db.TemplateRepairs.SingleAsync();
        Assert.Equal(f.Actor.UserId, repair.ActorId);
        Assert.Contains("preImage", repair.EvidenceJson);
        Assert.Equal(3, await f.Db.TemplateRepairAssignments.CountAsync());

        var replay = await f.Service.Apply(f.Form, request);
        Assert.True(replay.Value.AlreadyApplied);
        Assert.Single(f.Db.TemplateRepairs);
        Assert.Empty((await f.Service.Context(f.Form)).Value.Tests);
        var history = Assert.Single((await f.Service.History(f.Form)).Value);
        Assert.Equal((3, 3, "Quality Lead"), (history.Tests, history.Fields, history.ActorName));
    }

    [Fact]
    public async Task Without_the_view_key_the_workspace_and_preview_are_refused()
    {
        await using var f = new Fixture(); await f.Seed();
        await f.Revoke(PermissionKeys.CanViewTemplateRepair);
        Assert.True((await f.Service.Context(f.Form)).IsFailure);
        Assert.True((await f.Service.Preview(f.Form, f.Candidate())).IsFailure);
        Assert.True((await f.Service.History(f.Form)).IsFailure);
    }

    [Fact]
    public async Task Without_the_apply_key_nothing_is_written()
    {
        await using var f = new Fixture(); await f.Seed();
        var request = await f.Request();
        await f.Revoke(PermissionKeys.CanApplyTemplateRepair);
        Assert.False((await f.Service.Context(f.Form)).Value.CanApply);
        Assert.True((await f.Service.Apply(f.Form, request)).IsFailure);
        Assert.Empty(f.Db.TemplateRepairs);
        Assert.Equal(4, await f.Db.FormFieldAssignees.CountAsync());
    }

    [Fact]
    public async Task A_test_the_caller_cannot_reassign_is_hidden_and_refused()
    {
        await using var f = new Fixture(); await f.Seed();
        await f.Revoke(PermissionKeys.CanReassignRawMaterialTest);
        var context = (await f.Service.Context(f.Form)).Value;
        Assert.DoesNotContain(context.Tests, t => t.FormAssigneeId == f.GrnTest);
        Assert.Equal(1, context.HiddenTests);
        var opened = (await f.Service.Context(f.Form, f.GrnTest)).Value.Tests.Single(t => t.FormAssigneeId == f.GrnTest);
        Assert.False(opened.Eligible); Assert.Contains(PermissionKeys.CanReassignRawMaterialTest, opened.Blocker);
        Assert.True((await f.Service.Preview(f.Form, f.Candidate(f.GrnTest))).IsFailure);
        Assert.True((await f.Service.Preview(f.Form, f.Candidate(f.AtrTest))).IsSuccess);
    }

    [Fact]
    public async Task Checked_worksheets_closed_tests_and_analysts_not_on_the_test_are_blocked()
    {
        await using var f = new Fixture(); await f.Seed();
        Assert.NotEmpty((await f.Service.Preview(f.Form, f.Candidate(f.ReleasedTest))).Value.Blockers);
        var stranger = new TemplateRepairCandidate([new(f.AtrTest, f.NewField, f.Actor.UserId!.Value)]);
        Assert.NotEmpty((await f.Service.Preview(f.Form, stranger)).Value.Blockers);
        (await f.Db.Responses.SingleAsync()).CheckedAt = DateTime.UtcNow; await f.Db.SaveChangesAsync();
        var preview = (await f.Service.Preview(f.Form, f.Candidate(f.AtrTest))).Value;
        Assert.Contains(preview.Blockers, b => b.Contains("checked"));
        var request = new ApplyTemplateRepairRequest(f.Candidate(f.AtrTest), preview.PreviewToken, Guid.NewGuid(), "Appearance was added after assignment.");
        Assert.True((await f.Service.Apply(f.Form, request)).IsFailure);
        Assert.Empty(f.Db.TemplateRepairs);
    }

    [Fact]
    public async Task A_change_after_the_preview_needs_a_fresh_review_and_a_reason_is_required()
    {
        await using var f = new Fixture(); await f.Seed();
        var request = await f.Request();
        Assert.True((await f.Service.Apply(f.Form, request with { Reason = "short" })).IsFailure);
        f.Db.FormResponses.Add(new FormResponse { Id = Guid.NewGuid(), ResponseId = f.AtrResponse, FormFieldId = f.OldField, Value = "501 mg" });
        (await f.Db.Responses.SingleAsync()).UpdatedAt = DateTime.UtcNow;
        await f.Db.SaveChangesAsync();
        var stale = await f.Service.Apply(f.Form, request);
        Assert.Equal("TemplateRepair.StalePreview", Assert.Single(stale.Errors).Code);
        Assert.Empty(f.Db.TemplateRepairs);
    }

    [Fact]
    public async Task Repair_history_is_append_only()
    {
        await using var f = new Fixture(); await f.Seed();
        Assert.True((await f.Service.Apply(f.Form, await f.Request())).IsSuccess);
        (await f.Db.TemplateRepairs.SingleAsync()).Reason = "Tampered reason";
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task Editing_an_answer_without_updating_its_response_invalidates_the_preview()
    {
        await using var f = new Fixture(); await f.Seed();
        var request = await f.Request();
        (await f.Db.FormResponses.SingleAsync()).Value = "501 mg";
        await f.Db.SaveChangesAsync();
        Assert.Equal("TemplateRepair.StalePreview", (await f.Service.Apply(f.Form, request)).Error.Code);
        Assert.Empty(f.Db.TemplateRepairs);
    }

    [Fact]
    public async Task Duplicate_fields_existing_fields_and_request_id_reuse_are_refused()
    {
        await using var f = new Fixture(); await f.Seed();
        var assignment = new TemplateRepairAssignmentInput(f.AtrTest, f.NewField, f.Analyst);
        Assert.True((await f.Service.Preview(f.Form, new([assignment, assignment]))).IsFailure);
        Assert.NotEmpty((await f.Service.Preview(f.Form, new([assignment with { FormFieldId = f.OldField }]))).Value.Blockers);
        var request = await f.Request();
        Assert.True((await f.Service.Apply(f.Form, request)).IsSuccess);
        Assert.Equal("TemplateRepair.RequestReused", (await f.Service.Apply(f.Form, request with { Reason = "A different repair reason" })).Error.Code);
        Assert.Single(f.Db.TemplateRepairs);
    }

    [Fact]
    public async Task Closing_a_test_or_revoking_its_variant_grant_after_preview_prevents_apply()
    {
        await using var f = new Fixture(); await f.Seed();
        var request = await f.Request(f.Candidate(f.GrnTest));
        await f.Revoke(PermissionKeys.CanReassignRawMaterialTest);
        Assert.True((await f.Service.Apply(f.Form, request)).IsFailure);
        await f.Grant(PermissionKeys.CanReassignRawMaterialTest);
        var batchId = (await f.Db.FormAssignees.SingleAsync(a => a.Id == f.GrnTest)).MaterialBatchId;
        (await f.Db.MaterialBatches.IgnoreAutoIncludes().SingleAsync(b => b.Id == batchId)).Status = BatchStatus.Available;
        await f.Db.SaveChangesAsync();
        Assert.True((await f.Service.Apply(f.Form, request)).IsFailure);
        Assert.Empty(f.Db.TemplateRepairs);
    }

    [Fact]
    public async Task A_new_sample_supersedes_an_old_retest_assignment()
    {
        await using var f = new Fixture(); await f.Seed();
        var assignment = await f.Db.FormAssignees.SingleAsync(a => a.Id == f.RetestTest);
        f.Db.MaterialSamplings.Add(new MaterialSampling { Id = Guid.NewGuid(), MaterialBatchId = assignment.MaterialBatchId!.Value,
            ChemicalArdId = Guid.NewGuid(), SampleDate = DateTime.UtcNow.AddDays(1) });
        await f.Db.SaveChangesAsync();
        var opened = (await f.Service.Context(f.Form, f.RetestTest)).Value.Tests.Single(t => t.FormAssigneeId == f.RetestTest);
        Assert.False(opened.Eligible);
        Assert.Contains("earlier sample", opened.Blocker);
        Assert.NotEmpty((await f.Service.Preview(f.Form, f.Candidate(f.RetestTest))).Value.Blockers);
    }

    [Fact]
    public async Task A_governed_response_and_disabled_analyst_are_blocked()
    {
        await using var f = new Fixture(); await f.Seed();
        (await f.Db.Responses.SingleAsync()).FormRevisionId = Guid.NewGuid();
        (await f.Db.Users.SingleAsync(u => u.Id == f.Analyst)).IsDisabled = true;
        await f.Db.SaveChangesAsync();
        var context = (await f.Service.Context(f.Form)).Value;
        Assert.All(context.Tests, t => Assert.False(t.Eligible));
        Assert.NotEmpty((await f.Service.Preview(f.Form, f.Candidate())).Value.Blockers);
        Assert.Empty(f.Db.TemplateRepairs);
    }

    [Fact]
    public async Task Reassignment_can_split_unanswered_questions_between_different_analysts()
    {
        await using var f = new Fixture(); await f.Seed();
        await f.Grant(PermissionKeys.CanStartRawMaterialTest);
        var other = Guid.NewGuid();
        f.Db.Users.Add(new User { Id = other, UserName = "second-analyst", FirstName = "Kojo" });
        f.Db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = other, RoleId = f.Role });
        var first = await f.Db.FormFieldAssignees.SingleAsync(a => a.FormAssigneeId == f.GrnTest);
        var second = new FormFieldAssignee { Id = Guid.NewGuid(), FormAssigneeId = f.GrnTest, FormFieldId = f.NewField, AssigneeId = f.Analyst };
        f.Db.FormFieldAssignees.Add(second); await f.Db.SaveChangesAsync();
        var request = new ReassignTestFieldsRequest([new(first.Id, f.Analyst, f.Actor.UserId!.Value), new(second.Id, f.Analyst, other)], "Share the remaining unanswered test questions.");
        var result = await f.Service.ReassignFields(f.GrnTest, TemplateRepairTestKind.RawMaterial, request);
        Assert.True(result.IsSuccess, result.Error?.Description);
        Assert.Equal(2, result.Value.FieldsChanged);
        Assert.Equal(f.Actor.UserId, first.AssigneeId); Assert.Equal(other, second.AssigneeId);
        Assert.Equal("500 mg", (await f.Db.FormResponses.SingleAsync()).Value);
        Assert.True((await f.Service.ReassignFields(f.GrnTest, TemplateRepairTestKind.RawMaterial, request)).Value.AlreadyApplied);
    }

    [Fact]
    public async Task Reassignment_refuses_answered_fields_stale_owners_and_wrong_variant_permissions()
    {
        await using var f = new Fixture(); await f.Seed();
        await f.Grant(PermissionKeys.CanStartProductTest);
        var row = await f.Db.FormFieldAssignees.SingleAsync(a => a.FormAssigneeId == f.AtrTest);
        var request = new ReassignTestFieldsRequest([new(row.Id, f.Analyst, f.Actor.UserId!.Value)], "Move only the selected unanswered question.");
        Assert.True((await f.Service.ReassignFields(f.AtrTest, TemplateRepairTestKind.RawMaterial, request)).IsFailure);
        var answered = await f.Service.ReassignFields(f.AtrTest, TemplateRepairTestKind.Product, request);
        Assert.True(answered.IsFailure); Assert.Contains("saved responses", answered.Error.Description);
        await f.Revoke(PermissionKeys.CanReassignProductTest);
        Assert.True((await f.Service.ReassignFields(f.AtrTest, TemplateRepairTestKind.Product, request)).IsFailure);
        Assert.Equal(f.Analyst, row.AssigneeId);
    }

    [Theory]
    [InlineData(nameof(TestFieldReassignmentController.Product), PermissionKeys.CanReassignProductTest)]
    [InlineData(nameof(TestFieldReassignmentController.RawMaterial), PermissionKeys.CanReassignRawMaterialTest)]
    [InlineData(nameof(TestFieldReassignmentController.PackagingMaterial), PermissionKeys.CanReassignPackagingMaterialTest)]
    public void Reassignment_endpoints_require_the_specific_test_variant(string action, string key)
    {
        var method = typeof(TestFieldReassignmentController).GetMethod(action)!;
        Assert.Equal(key, Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public async Task Product_certificate_requires_its_key_and_valid_recorded_dates()
    {
        await using var f = new Fixture(); await f.Seed();
        var assignment = await f.Db.FormAssignees.SingleAsync(a => a.Id == f.AtrTest);
        var bmr = assignment.BatchManufacturingRecordId!.Value; var step = assignment.ProductionActivityStepId!.Value;
        Assert.True((await ProductCertificateGuard.Validate(f.Db, bmr, step, f.Actor.UserId!.Value)).IsFailure);
        await f.Grant(PermissionKeys.CanCheckProductTest);
        var test = await f.Db.AnalyticalTestRequests.SingleAsync(a => a.BatchManufacturingRecordId == bmr);
        test.SampledAt = DateTime.UtcNow.AddDays(-3); test.AcknowledgedAt = DateTime.UtcNow.AddDays(-2); test.TestedAt = DateTime.UtcNow.AddDays(-1);
        await f.Db.SaveChangesAsync();
        Assert.True((await ProductCertificateGuard.Validate(f.Db, bmr, step, f.Actor.UserId.Value)).IsSuccess);
        var now = new DateTime(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);
        Assert.True(ProductCertificateGuard.Dates(now.AddDays(-3), now.AddDays(-1), now.AddDays(-2), now).IsFailure);
        Assert.True(ProductCertificateGuard.Dates(now, now, now.AddDays(1), now).IsFailure);
        Assert.True(ProductCertificateGuard.Dates(now, now, null, now).IsFailure);
        Assert.True(ProductCertificateGuard.Dates(now, now, now, now).IsSuccess);
        var method = typeof(FormController).GetMethod(nameof(FormController.GenerateCertificateOfAnalysisForProduct))!;
        Assert.Equal(PermissionKeys.CanCheckProductTest, Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Theory]
    [InlineData(TemplateRepairTestKind.Product)]
    [InlineData(TemplateRepairTestKind.RawMaterial)]
    [InlineData(TemplateRepairTestKind.PackagingMaterial)]
    public async Task Each_test_variant_can_transfer_one_unanswered_field(TemplateRepairTestKind kind)
    {
        await using var f = new Fixture(); await f.Seed();
        await f.Grant(PermissionKeys.CanStartProductTest, PermissionKeys.CanStartRawMaterialTest, PermissionKeys.CanStartPackagingMaterialTest);
        Assert.True((await f.Service.Apply(f.Form, await f.Request())).IsSuccess);
        var testId = kind == TemplateRepairTestKind.Product ? f.AtrTest : kind == TemplateRepairTestKind.RawMaterial ? f.GrnTest : f.RetestTest;
        var row = await f.Db.FormFieldAssignees.SingleAsync(a => a.FormAssigneeId == testId && a.FormFieldId == f.NewField);
        var result = await f.Service.ReassignFields(testId, kind, new([new(row.Id, f.Analyst, f.Actor.UserId!.Value)], "Transfer the additional unanswered question."));
        Assert.True(result.IsSuccess, result.Error?.Description);
        Assert.Equal(f.Actor.UserId, row.AssigneeId);
        Assert.Equal("500 mg", (await f.Db.FormResponses.SingleAsync()).Value);
    }

    private sealed class TemplatePostgresFactAttribute : FactAttribute
    {
        public TemplatePostgresFactAttribute()
        {
            if (Environment.GetEnvironmentVariable("ORYX_TEMPLATE_REPAIR_TEST_CONNECTION") is null)
                Skip = "Set ORYX_TEMPLATE_REPAIR_TEST_CONNECTION to a disposable local PostgreSQL server (CREATE DATABASE required).";
        }
    }

    [TemplatePostgresFact]
    public async Task Postgres_migration_enforces_history_and_serialises_assignment_writes()
    {
        var admin = new Npgsql.NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ORYX_TEMPLATE_REPAIR_TEST_CONNECTION"));
        var database = "oryx_template_repair_" + Guid.NewGuid().ToString("N");
        await using var connection = new Npgsql.NpgsqlConnection(admin.ConnectionString);
        await connection.OpenAsync();
        await using (var create = new Npgsql.NpgsqlCommand($"CREATE DATABASE \"{database}\"", connection))
            await create.ExecuteNonQueryAsync();
        var target = new Npgsql.NpgsqlConnectionStringBuilder(admin.ConnectionString) { Database = database }.ConnectionString;
        try
        {
            await using var f = new Fixture(target);
            await f.Db.Database.MigrateAsync();
            // The seed omits unrelated business parents; all operations under test enforce normal constraints.
            await using (var seed = await f.Db.Database.BeginTransactionAsync())
            {
                await f.Db.Database.ExecuteSqlRawAsync("SET LOCAL session_replication_role = replica");
                await f.Seed(); await seed.CommitAsync();
            }
            f.Db.ChangeTracker.Clear();
            var request = await f.Request();
            await using (var writer = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(target).Options, f.Actor))
            {
                await using var write = await writer.Database.BeginTransactionAsync();
                await writer.Database.ExecuteSqlRawAsync("LOCK TABLE \"FormFieldAssignees\" IN ROW EXCLUSIVE MODE");
                var error = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => f.Service.Apply(f.Form, request));
                Assert.Equal("55P03", error.SqlState);
            }
            Assert.Empty(await f.Db.TemplateRepairs.ToListAsync());
            Assert.True((await f.Service.Apply(f.Form, request)).IsSuccess);
            Assert.True((await f.Service.Apply(f.Form, request)).Value.AlreadyApplied);
            Assert.Equal(3, await f.Db.TemplateRepairAssignments.CountAsync());
            Assert.Equal("500 mg", await f.Db.FormResponses.Select(r => r.Value).SingleAsync());
            foreach (var sql in new[] { "UPDATE \"TemplateRepairs\" SET \"Reason\" = 'Tampered reason'",
                "DELETE FROM \"TemplateRepairAssignments\"", "TRUNCATE \"TemplateRepairAssignments\"", "TRUNCATE \"TemplateRepairs\" CASCADE" })
            {
                var error = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => f.Db.Database.ExecuteSqlRawAsync(sql));
                Assert.Contains("append-only", error.MessageText);
            }
        }
        finally
        {
            Npgsql.NpgsqlConnection.ClearAllPools();
            await using var drop = new Npgsql.NpgsqlCommand($"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)", connection);
            await drop.ExecuteNonQueryAsync();
        }
    }

    [Theory]
    [InlineData(nameof(TemplateRepairController.Context), PermissionKeys.CanViewTemplateRepair)]
    [InlineData(nameof(TemplateRepairController.Preview), PermissionKeys.CanViewTemplateRepair)]
    [InlineData(nameof(TemplateRepairController.History), PermissionKeys.CanViewTemplateRepair)]
    [InlineData(nameof(TemplateRepairController.Apply), PermissionKeys.CanApplyTemplateRepair)]
    public void Each_endpoint_names_its_own_key(string action, string key)
    {
        var method = typeof(TemplateRepairController).GetMethod(action)!;
        Assert.Equal(key, Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Null(typeof(TemplateRepairController).GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void Template_repair_keys_are_registered_once()
    {
        var keys = PermissionUtils.GeneratePermissions().Select(p => p.Key).ToList();
        Assert.Single(keys, k => k == PermissionKeys.CanViewTemplateRepair);
        Assert.Single(keys, k => k == PermissionKeys.CanApplyTemplateRepair);
    }
}

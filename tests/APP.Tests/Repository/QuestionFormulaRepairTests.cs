using System.Text.Json;
using APP.Services.Formulas;
using APP.Utils;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public sealed class QuestionFormulaRepairTests
{
    private sealed class EngineFactAttribute : FactAttribute
    {
        public EngineFactAttribute()
        { if (Environment.GetEnvironmentVariable("ORYX_FORMULA_REPAIR_SERVICE_URL") is null) Skip = "Set ORYX_FORMULA_REPAIR_SERVICE_URL to an isolated local formula engine."; }
    }
    private sealed class PostgresFactAttribute : FactAttribute
    {
        public PostgresFactAttribute()
        { if (Environment.GetEnvironmentVariable("ORYX_FORMULA_REPAIR_TEST_CONNECTION") is null) Skip = "Set ORYX_FORMULA_REPAIR_TEST_CONNECTION to an isolated PostgreSQL server (requires CREATE DATABASE)."; }
    }
    private const string Before = "{\"type\":0,\"expression\":\":a/:b\",\"decimalPlaces\":2}";
    private const string After = "{\"type\":0,\"expression\":\":a/:b*100\",\"decimalPlaces\":2}";
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? DepartmentId { get; } = Guid.NewGuid();
        public string DepartmentType => "Production";
    }
    private sealed class Client : IFormulaCalculationClient
    {
        public bool Available { get; set; } = true;
        public Task<Result<bool>> IsReadyAsync(CancellationToken token = default) => Task.FromResult(Result.Success(Available));
        public Task<Result<FormulaServiceResponse>> ValidateAsync(JsonElement request, CancellationToken token = default) =>
            Task.FromResult(Available ? Result.Success(new FormulaServiceResponse("oryx-formula-service-response-v1", 0, "test", 5, "COMPLETED",
                "test", new string('a', 64), request.GetProperty("definitionHash").GetString(), request.GetProperty("definitionHash").GetString(),
                null, null, null, null, null, [], [], [], [], null, [])) : Result.Failure<FormulaServiceResponse>(Error.Conflict("Test.Unavailable", "Offline")));
        public JsonElement? Evaluated { get; private set; }
        /// <summary>Answers every result of the requested definition with 1.00, echoing the hashes the server checks.</summary>
        public Task<Result<FormulaServiceResponse>> EvaluateAsync(JsonElement request, CancellationToken token = default)
        {
            Evaluated = request.Clone();
            var results = request.GetProperty("definition").GetProperty("results").EnumerateArray()
                .Select(result => new { key = result.GetProperty("key").GetString(), roundedResult = "1.00", displayedResult = "1.00", unit = (string?)null }).ToArray();
            var inputs = JsonSerializer.Deserialize<Dictionary<string, string?>>(request.GetProperty("resolvedInputs").GetRawText())!;
            return Task.FromResult(Result.Success(new FormulaServiceResponse("oryx-formula-service-response-v1", 1, "test", 5, "COMPLETED", "test", new string('a', 64),
                request.GetProperty("definitionHash").GetString(), request.GetProperty("definitionHash").GetString(),
                request.GetProperty("configurationHash").GetString(), request.GetProperty("configurationHash").GetString(),
                FormulaCanonicalJson.HashJson("oryx:formula-resolved-inputs:v1", JsonSerializer.Serialize(inputs), 262144),
                request.GetProperty("configuration").GetProperty("placementKey").GetString(), null, [], [], [], [],
                JsonSerializer.SerializeToElement(new { status = 0, results }), [])));
        }
    }
    private sealed class Fixture : IAsyncDisposable
    {
        public Actor Actor { get; } = new();
        public ApplicationDbContext Db { get; }
        public Client Calculation { get; } = new();
        public QuestionFormulaRepairService Service { get; }
        public Guid Question { get; } = Guid.NewGuid();
        public Guid Field { get; } = Guid.NewGuid();
        public Guid Response { get; } = Guid.NewGuid();
        public Guid Historical { get; } = Guid.NewGuid();
        public Guid Atr { get; } = Guid.NewGuid();
        public Fixture(string? postgres = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>();
            if (postgres is null) options.UseInMemoryDatabase(Guid.NewGuid().ToString(), o => o.EnableNullChecks(false));
            else options.UseNpgsql(postgres);
            Db = new(options.Options, Actor);
            Service = new(Db, Actor, Calculation);
        }
        public async Task Seed(bool includeOpenResponse = true)
        {
            var role = Guid.NewGuid(); var form = Guid.NewGuid(); var section = Guid.NewGuid(); var product = Guid.NewGuid();
            var batch = Guid.NewGuid(); var bmr = Guid.NewGuid(); var step = Guid.NewGuid(); var ard = Guid.NewGuid();
            Db.Departments.Add(new Department { Id = Actor.DepartmentId!.Value, Name = "Tablets", Type = DepartmentType.Production });
            Db.Users.Add(new User { Id = Actor.UserId!.Value, DepartmentId = Actor.DepartmentId, UserName = "repair-operator" });
            Db.Roles.Add(new Role { Id = role, Name = "Assigned repair operator" });
            Db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = Actor.UserId.Value, RoleId = role });
            string[] grants = [PermissionKeys.CanViewQuestions, PermissionKeys.CanRepairQuestionFormula, PermissionKeys.CanRepairTestFormula,
                PermissionKeys.CanStartProductTest, PermissionKeys.CanViewProductAnalyticalTestRequests, PermissionKeys.CanExecuteFormulaResponse];
            for (var i = 0; i < grants.Length; i++)
            {
                Db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = i + 1, RoleId = role, ClaimType = AppConstants.Permission, ClaimValue = grants[i] });
                Db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = i + 1, Key = grants[i], Type = "Action" });
            }
            Db.Forms.Add(new Form { Id = form, Name = "Assay" });
            Db.FormSections.Add(new FormSection { Id = section, FormId = form, Name = "Results" });
            Db.Questions.Add(new Question { Id = Question, Label = "Assay", Type = QuestionType.Formula });
            Db.QuestionOptions.Add(new QuestionOption { Id = Guid.NewGuid(), QuestionId = Question, Name = Before });
            Db.FormFields.Add(new FormField { Id = Field, FormSectionId = section, QuestionId = Question });
            if (includeOpenResponse)
                Db.Responses.Add(new Response { Id = Response, FormId = form, BatchManufacturingRecordId = bmr, ProductionActivityStepId = step, CreatedById = Actor.UserId });
            Db.Responses.Add(new Response { Id = Historical, FormId = form, CheckedAt = DateTime.UtcNow, Approved = true });
            Db.Products.Add(new Product { Id = product, Name = "Product", DepartmentId = Actor.DepartmentId!.Value });
            Db.ProductionScheduleProducts.Add(new ProductionScheduleProduct { Id = batch, ProductId = product });
            Db.ProductionActivitySteps.Add(new ProductionActivityStep { Id = step });
            Db.BatchManufacturingRecords.Add(new BatchManufacturingRecord
            { Id = bmr, ProductionScheduleProductId = batch, ProductionActivityStepId = step, BatchNumber = "TEST" });
            Db.ProductAnalyticalRawData.Add(new DOMAIN.Entities.ProductAnalyticalRawData.ProductAnalyticalRawData { Id = ard, FormId = form });
            Db.AnalyticalTestRequests.Add(new AnalyticalTestRequest { Id = Atr, ProductionScheduleProductId = batch, BatchManufacturingRecordId = bmr,
                ProductionActivityStepId = step, ChemicalArdId = ard, ArNumber = "AR-TEST", Status = AnalyticalTestStatus.Testing });
            Db.FormAssignees.Add(new FormAssignee { Id = Guid.NewGuid(), FormId = form, BatchManufacturingRecordId = bmr, ProductionActivityStepId = step,
                FieldAssignees = [new FormFieldAssignee { Id = Guid.NewGuid(), FormFieldId = Field, AssigneeId = Actor.UserId }] });
            await Db.SaveChangesAsync();
        }
        /// <summary>Approves <paramref name="definition"/> as the question's effective formula revision.</summary>
        public async Task Approve(string definition, int revision = 2, string? compiled = null)
        {
            var formula = Guid.NewGuid();
            Db.FormulaDefinitions.Add(new FormulaDefinition { Id = formula, Key = $"question-{Question:N}", Name = "Assay", PresentationPreset = "legacy" });
            Db.QuestionFormulaDefinitions.Add(new QuestionFormulaDefinition { Id = Guid.NewGuid(), QuestionId = Question, FormulaDefinitionId = formula });
            var row = new FormulaRevision { Id = Guid.NewGuid(), FormulaDefinitionId = formula, Revision = revision,
                Status = FormulaRevisionStatus.Draft, AuthoringPayloadJson = definition,
                DefinitionJson = compiled ?? "{\"variables\":[],\"results\":[]}", DefinitionHash = new string('d', 64) };
            Db.FormulaRevisions.Add(row);
            await Db.SaveChangesAsync();
            // Revisions are created as drafts and move through review, each step with its audit row.
            foreach (var next in new[] { FormulaRevisionStatus.InReview, FormulaRevisionStatus.Approved })
            {
                var prior = row.Status;
                row.Status = next;
                if (next == FormulaRevisionStatus.Approved)
                { row.ApprovedById = Actor.UserId; row.ApprovedAt = DateTime.UtcNow; row.EffectiveAt = DateTime.UtcNow.AddMinutes(-1); }
                Db.FormulaRevisionAudits.Add(FormulaDefinitionMapping.Audit(row, prior, next, next.ToString(), "Test fixture.", Actor.UserId!.Value, Guid.NewGuid()));
                await Db.SaveChangesAsync();
            }
        }
        /// <summary>Puts the same formula question on the worksheet a second time, assigned to the actor.</summary>
        public async Task<Guid> SecondField()
        {
            var first = await Db.FormFields.Include(f => f.FormSection).SingleAsync(f => f.Id == Field);
            var section = new FormSection { Id = Guid.NewGuid(), FormId = first.FormSection.FormId, Name = "Results (second sample)" };
            var field = new FormField { Id = Guid.NewGuid(), FormSectionId = section.Id, QuestionId = Question };
            Db.FormSections.Add(section); Db.FormFields.Add(field);
            var assignee = await Db.FormAssignees.SingleAsync();
            Db.FormFieldAssignees.Add(new FormFieldAssignee { Id = Guid.NewGuid(), FormAssigneeId = assignee.Id, FormFieldId = field.Id, AssigneeId = Actor.UserId });
            await Db.SaveChangesAsync();
            return field.Id;
        }
        public FormulaRepairCandidate Candidate(bool question = true, bool test = true) => new(After, question, test ? [new(Atr, Field)] : []);
        public async Task<ApplyFormulaRepairRequest> Request(FormulaRepairCandidate? candidate = null)
        {
            candidate ??= Candidate(); var preview = await Service.Preview(Question, candidate);
            Assert.True(preview.IsSuccess, preview.Error?.Description);
            Assert.Empty(preview.Value.Blockers);
            return new(candidate, preview.Value.PreviewToken, Guid.NewGuid(), "Correct a confirmed formula configuration error.");
        }
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    [Fact]
    public async Task Test_repair_before_any_readings_creates_only_an_empty_draft_and_audited_configuration()
    {
        await using var f = new Fixture(); await f.Seed(includeOpenResponse: false);

        var context = await f.Service.Context(f.Question);
        var test = Assert.Single(context.Value.Tests);
        Assert.True(test.Eligible);
        Assert.Null(test.ResponseId);
        Assert.Single(f.Db.Responses);
        var candidate = f.Candidate(false, true);
        var request = await f.Request(candidate);
        Assert.Single(f.Db.Responses);
        Assert.True((await f.Service.Apply(f.Question, request)).IsSuccess);

        var draft = await f.Db.Responses.SingleAsync(r => r.Id != f.Historical);
        Assert.Empty(f.Db.FormResponses);
        Assert.Null(draft.CheckedAt);
        Assert.False(draft.Approved);
        Assert.Equal(Before, (await f.Db.QuestionOptions.SingleAsync()).Name);
        var pin = Assert.Single(await LegacyFormulaConfigurationReader.Latest(f.Db, [draft.Id]));
        Assert.Equal(After, pin.Definition);
        Assert.True(pin.RequiresRecalculation);
        Assert.Equal(draft.Id, Assert.Single((await f.Service.Apply(f.Question, request)).Value.ResponseIds));
        Assert.Equal(2, await f.Db.Responses.CountAsync());
        Assert.Single(f.Db.QuestionFormulaRepairs);
        Assert.True((await f.Service.Evaluate(new(f.Atr, f.Field), pin.DefinitionHash)).IsFailure);
    }

    [Fact]
    public async Task Unsaved_test_repair_rechecks_state_assignment_permission_and_preview_before_creating_a_draft()
    {
        await using var f = new Fixture(); await f.Seed(includeOpenResponse: false);
        var request = await f.Request(f.Candidate(false, true));
        var atr = await f.Db.AnalyticalTestRequests.SingleAsync();
        atr.Status = AnalyticalTestStatus.TestTaken;
        await f.Db.SaveChangesAsync();
        Assert.True((await f.Service.Apply(f.Question, request)).IsFailure);
        Assert.Single(f.Db.Responses);
        atr.Status = AnalyticalTestStatus.Testing;
        await f.Db.SaveChangesAsync();
        request = await f.Request(f.Candidate(false, true));
        var assignment = await f.Db.FormFieldAssignees.SingleAsync();
        assignment.AssigneeId = Guid.NewGuid();
        await f.Db.SaveChangesAsync();
        Assert.Equal(ErrorType.Forbidden, Assert.Single((await f.Service.Apply(f.Question, request)).Errors).Type);
        assignment.AssigneeId = f.Actor.UserId;
        await f.Db.SaveChangesAsync();
        request = await f.Request(f.Candidate(false, true));
        var claim = await f.Db.RoleClaims.SingleAsync(c => c.ClaimValue == PermissionKeys.CanRepairTestFormula);
        f.Db.RoleClaims.Remove(claim);
        await f.Db.SaveChangesAsync();
        Assert.Equal(ErrorType.Forbidden, Assert.Single((await f.Service.Apply(f.Question, request)).Errors).Type);
        Assert.Single(f.Db.Responses);
        Assert.Empty(f.Db.QuestionFormulaRepairs);
    }

    [Fact]
    public async Task A_draft_created_after_preview_requires_a_fresh_review()
    {
        await using var f = new Fixture(); await f.Seed(includeOpenResponse: false);
        var request = await f.Request(f.Candidate(false, true));
        var atr = await f.Db.AnalyticalTestRequests.SingleAsync();
        f.Db.Responses.Add(new Response { Id = f.Response, FormId = (await f.Db.Forms.SingleAsync()).Id,
            BatchManufacturingRecordId = atr.BatchManufacturingRecordId, ProductionActivityStepId = atr.ProductionActivityStepId });
        await f.Db.SaveChangesAsync();
        Assert.Equal("FormulaRepair.StalePreview", (await f.Service.Apply(f.Question, request)).Error.Code);
        Assert.Empty(f.Db.QuestionFormulaRepairs);
    }

    [Fact]
    public async Task An_unrelated_governed_response_does_not_block_repairing_a_legacy_test()
    {
        await using var f = new Fixture(); await f.Seed();
        (await f.Db.Responses.SingleAsync(r => r.Id == f.Historical)).FormRevisionId = Guid.NewGuid();
        await f.Db.SaveChangesAsync();
        Assert.Empty((await f.Service.Context(f.Question)).Value.Blockers);
        var legacyOnly = await f.Request(f.Candidate(false, true));
        Assert.True((await f.Service.Apply(f.Question, legacyOnly)).IsSuccess);
        var shared = await f.Service.Preview(f.Question, f.Candidate(true, false));
        Assert.NotEmpty(shared.Value.Blockers);
    }

    [Fact]
    public async Task Combined_repair_pins_unanswered_history_and_invalidates_only_selected_test()
    {
        await using var f = new Fixture(); await f.Seed();
        var reading = new FormResponse { Id = Guid.NewGuid(), ResponseId = f.Response, FormFieldId = f.Field, Value = "{\"a\":\"1.230000\",\"b\":\"2\"}" };
        f.Db.FormResponses.Add(reading); await f.Db.SaveChangesAsync();
        var original = reading.Value; var request = await f.Request();
        Assert.Empty(f.Db.QuestionFormulaRepairs); Assert.Empty(f.Db.LegacyFormulaConfigurationPins);
        var applied = await f.Service.Apply(f.Question, request); Assert.True(applied.IsSuccess, applied.Error?.Description);
        Assert.Equal(After, (await f.Db.QuestionOptions.SingleAsync()).Name);
        Assert.Equal(original, reading.Value); Assert.True(await LegacyFormulaConfigurationReader.Pending(f.Db, await f.Db.Responses.SingleAsync(r => r.Id == f.Response)));
        var pins = await LegacyFormulaConfigurationReader.Latest(f.Db, [f.Response, f.Historical]);
        Assert.Equal(Before, pins.Single(p => p.ResponseId == f.Historical).Definition);
        Assert.False(pins.Single(p => p.ResponseId == f.Historical).RequiresRecalculation);
        Assert.Equal(After, pins.Single(p => p.ResponseId == f.Response).Definition);
        Assert.Equal(f.Actor.UserId, (await f.Db.QuestionFormulaRepairs.SingleAsync()).ActorId);
        Assert.True((await f.Db.Responses.SingleAsync(r => r.Id == f.Historical)).Approved);
        var retry = await f.Service.Apply(f.Question, request); Assert.True(retry.IsSuccess); Assert.True(retry.Value.AlreadyApplied);
        Assert.Single(f.Db.QuestionFormulaRepairs);
        Assert.True((await f.Service.Apply(f.Question, request with { Reason = "A changed reason for a reused request." })).IsFailure);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Scopes_change_only_the_reviewed_targets(bool master, bool test)
    {
        await using var f = new Fixture(); await f.Seed();
        var request = await f.Request(f.Candidate(master, test)); Assert.True((await f.Service.Apply(f.Question, request)).IsSuccess);
        Assert.Equal(master ? After : Before, (await f.Db.QuestionOptions.SingleAsync()).Name);
        var pins = await LegacyFormulaConfigurationReader.Latest(f.Db, [f.Response, f.Historical]);
        Assert.Equal(test, pins.Single(p => p.ResponseId == f.Response).RequiresRecalculation);
        Assert.Equal(master ? 2 : 1, pins.Count);
    }

    [Fact]
    public async Task Stale_previews_closed_tests_and_revoked_permissions_never_write()
    {
        await using var f = new Fixture(); await f.Seed(); var request = await f.Request();
        var atr = await f.Db.AnalyticalTestRequests.SingleAsync(); atr.Status = AnalyticalTestStatus.TestTaken; await f.Db.SaveChangesAsync();
        Assert.True((await f.Service.Apply(f.Question, request)).IsFailure); Assert.Empty(f.Db.QuestionFormulaRepairs);
        atr.Status = AnalyticalTestStatus.Testing; await f.Db.SaveChangesAsync(); request = await f.Request();
        f.Db.FormResponses.Add(new FormResponse { Id = Guid.NewGuid(), ResponseId = f.Response, FormFieldId = f.Field, Value = "{\"a\":\"9\",\"b\":\"2\"}" }); await f.Db.SaveChangesAsync();
        Assert.True((await f.Service.Apply(f.Question, request)).IsFailure); Assert.Empty(f.Db.QuestionFormulaRepairs);
        request = await f.Request(); f.Db.RoleClaims.RemoveRange(f.Db.RoleClaims.Where(c => c.ClaimValue == PermissionKeys.CanRepairTestFormula)); await f.Db.SaveChangesAsync();
        Assert.True((await f.Service.Apply(f.Question, request)).IsFailure); Assert.Empty(f.Db.QuestionFormulaRepairs);
    }

    [Fact]
    public async Task Assignment_and_test_permissions_are_independent_of_repair_permission()
    {
        await using var f = new Fixture(); await f.Seed(); var assignment = await f.Db.FormFieldAssignees.SingleAsync();
        assignment.AssigneeId = Guid.NewGuid(); await f.Db.SaveChangesAsync();
        Assert.True((await f.Service.Preview(f.Question, f.Candidate())).IsFailure);
        // Assigned again, but without the permission to run product tests.
        assignment.AssigneeId = f.Actor.UserId;
        var start = await f.Db.RoleClaims.SingleAsync(claim => claim.ClaimValue == PermissionKeys.CanStartProductTest);
        f.Db.PermissionTypes.RemoveRange(f.Db.PermissionTypes.Where(type => type.RoleClaimId == start.Id)); f.Db.RoleClaims.Remove(start);
        await f.Db.SaveChangesAsync();
        Assert.True((await f.Service.Preview(f.Question, f.Candidate())).IsFailure);
    }

    [Fact]
    public async Task Offline_validation_and_changed_shape_are_blocked_and_audit_is_append_only()
    {
        await using var f = new Fixture(); await f.Seed(); f.Calculation.Available = false;
        Assert.True((await f.Service.Preview(f.Question, f.Candidate())).IsFailure); Assert.Empty(f.Db.QuestionFormulaRepairs);
        f.Calculation.Available = true;
        var changed = await f.Service.Preview(f.Question, f.Candidate() with { Definition = After.Replace("\"decimalPlaces\":2", "\"decimalPlaces\":4") });
        Assert.True(changed.IsSuccess); Assert.NotEmpty(changed.Value.Blockers);
        var request = await f.Request(); Assert.True((await f.Service.Apply(f.Question, request)).IsSuccess);
        (await f.Db.QuestionFormulaRepairs.SingleAsync()).Reason = "Tampered";
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task Worksheet_saves_must_carry_the_configuration_they_were_opened_with()
    {
        await using var f = new Fixture(); await f.Seed();
        var field = await f.Db.FormFields.Include(a => a.Question).SingleAsync();
        var open = await f.Db.Responses.SingleAsync(r => r.Id == f.Response);
        var historical = await f.Db.Responses.SingleAsync(r => r.Id == f.Historical);
        // Never repaired: legacy clients that send no hash keep working.
        Assert.Null(await LegacyFormulaConfigurationReader.ValidateWrite(f.Db, open, field, null));
        Assert.True((await f.Service.Apply(f.Question, await f.Request())).IsSuccess);
        var repaired = (await LegacyFormulaConfigurationReader.Latest(f.Db, [f.Response])).Single();
        Assert.NotNull(await LegacyFormulaConfigurationReader.ValidateWrite(f.Db, open, field, null));
        Assert.NotNull(await LegacyFormulaConfigurationReader.ValidateWrite(f.Db, open, field, LegacyFormulaRepairDefinition.Hash(Before)));
        Assert.Null(await LegacyFormulaConfigurationReader.ValidateWrite(f.Db, open, field, repaired.DefinitionHash));
        // An unselected test keeps the definition it was opened with.
        Assert.Null(await LegacyFormulaConfigurationReader.ValidateWrite(f.Db, historical, field, LegacyFormulaRepairDefinition.Hash(Before)));
        // A test started after the repair uses the repaired question.
        var fresh = new Response { Id = Guid.NewGuid(), FormId = open.FormId };
        Assert.Null(await LegacyFormulaConfigurationReader.ValidateWrite(f.Db, fresh, field, LegacyFormulaRepairDefinition.Hash(After)));
        Assert.NotNull(await LegacyFormulaConfigurationReader.ValidateWrite(f.Db, fresh, field, LegacyFormulaRepairDefinition.Hash(Before)));
    }

    [Fact]
    public async Task Preview_tokens_and_request_ids_are_bound_to_the_operator()
    {
        await using var f = new Fixture(); await f.Seed();
        var request = await f.Request();
        Assert.True((await f.Service.Apply(f.Question, request)).IsSuccess);
        var intruder = new Actor();
        f.Db.Users.Add(new User { Id = intruder.UserId!.Value, DepartmentId = f.Actor.DepartmentId, UserName = "second-operator" });
        f.Db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = intruder.UserId.Value, RoleId = (await f.Db.UserRoles.FirstAsync()).RoleId });
        await f.Db.SaveChangesAsync();
        var other = new QuestionFormulaRepairService(f.Db, intruder, f.Calculation);
        var reused = await other.Apply(f.Question, request);
        Assert.True(reused.IsFailure); Assert.Equal("FormulaRepair.RequestReused", reused.Error.Code);
        // A token reviewed by one operator cannot be applied by another, even with a fresh request ID.
        var candidate = f.Candidate(true, false) with { Definition = After.Replace("*100", "*1000") };
        var preview = await f.Service.Preview(f.Question, candidate); Assert.True(preview.IsSuccess); Assert.Empty(preview.Value.Blockers);
        var stolen = await other.Apply(f.Question, new(candidate, preview.Value.PreviewToken, Guid.NewGuid(), "Apply another operator's preview."));
        Assert.True(stolen.IsFailure); Assert.Equal("FormulaRepair.StalePreview", stolen.Error.Code);
        Assert.Single(f.Db.QuestionFormulaRepairs);
    }

    [Fact]
    public async Task History_names_the_operator_and_evidence_records_invalidated_readings()
    {
        await using var f = new Fixture(); await f.Seed();
        (await f.Db.Users.SingleAsync()).FirstName = "Ama"; (await f.Db.Users.SingleAsync()).LastName = "Mensah";
        f.Db.FormResponses.Add(new FormResponse { Id = Guid.NewGuid(), ResponseId = f.Response, FormFieldId = f.Field, Value = "{\"a\":\"1.50\",\"b\":\"3\"}" });
        await f.Db.SaveChangesAsync();
        var preview = await f.Service.Preview(f.Question, f.Candidate());
        var test = Assert.Single(preview.Value.Tests);
        Assert.True(test.HasReadings); Assert.True(test.ConfigurationChanges); Assert.False(test.MissingInputs);
        Assert.Contains("AR-TEST", test.Label);
        Assert.True((await f.Service.Apply(f.Question, await f.Request())).IsSuccess);
        var history = Assert.Single((await f.Service.History(f.Question)).Value);
        Assert.Equal("Ama Mensah", history.ActorName); Assert.True(history.UpdatedQuestion);
        Assert.Equal(1, history.RepairedTests); Assert.Equal(2, history.CapturedTests);
        var evidence = JsonDocument.Parse((await f.Db.QuestionFormulaRepairs.SingleAsync()).EvidenceJson).RootElement;
        Assert.Equal(1, evidence.GetProperty("invalidatedResults").GetArrayLength());
        Assert.Contains("1.50", evidence.GetProperty("preImage").GetProperty("answers")[0].GetProperty("Value").GetString());
        Assert.Equal(LegacyFormulaRepairDefinition.Hash(Before), evidence.GetProperty("definitionBeforeHash").GetString());
    }

    [Fact]
    public async Task Unchanged_question_only_repairs_are_blocked()
    {
        await using var f = new Fixture(); await f.Seed();
        var preview = await f.Service.Preview(f.Question, new(Before, true, []));
        Assert.True(preview.IsSuccess); Assert.NotEmpty(preview.Value.Blockers);
    }

    [Fact]
    public async Task Forms_with_repaired_history_refuse_field_replacement()
    {
        await using var f = new Fixture(); await f.Seed();
        Assert.True((await f.Service.Apply(f.Question, await f.Request())).IsSuccess);
        var repository = new APP.Repository.FormRepository(f.Db, null!, null!, null!, null!);
        var form = await f.Db.Forms.SingleAsync();
        var result = await repository.UpdateForm(new DOMAIN.Entities.Forms.Request.CreateFormRequest { Name = "Assay" }, form.Id, f.Actor.UserId!.Value);
        Assert.True(result.IsFailure); Assert.Equal("Form.FormulaRepairHistory", result.Error.Code);
        Assert.Single(f.Db.FormFields);
    }

    [Fact]
    public async Task Recalculation_refuses_stale_configuration_and_incomplete_readings_before_calling_the_engine()
    {
        await using var f = new Fixture(); await f.Seed();
        Assert.True((await f.Service.Apply(f.Question, await f.Request(f.Candidate(false, true)))).IsSuccess);
        var pin = (await LegacyFormulaConfigurationReader.Latest(f.Db, [f.Response])).Single();
        var stale = await f.Service.Evaluate(new(f.Atr, f.Field), LegacyFormulaRepairDefinition.Hash(Before));
        Assert.True(stale.IsFailure); Assert.Equal("FormulaRepair.StaleConfiguration", stale.Error.Code);
        var incomplete = await f.Service.Evaluate(new(f.Atr, f.Field), pin.DefinitionHash);
        Assert.True(incomplete.IsFailure); // No readings saved; the throwing test engine is never reached.
        (await f.Db.FormFieldAssignees.SingleAsync()).AssigneeId = Guid.NewGuid(); await f.Db.SaveChangesAsync();
        Assert.Equal(ErrorType.Forbidden, (await f.Service.Evaluate(new(f.Atr, f.Field), pin.DefinitionHash)).Error.Type);
        Assert.Empty(f.Db.LegacyFormulaRepairEvaluations);
    }

    [PostgresFact]
    public async Task Postgres_repair_can_create_an_empty_test_draft_atomically()
    {
        var admin = new Npgsql.NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ORYX_FORMULA_REPAIR_TEST_CONNECTION"));
        var database = "oryx_formula_unsaved_" + Guid.NewGuid().ToString("N");
        await using var connection = new Npgsql.NpgsqlConnection(admin.ConnectionString);
        await connection.OpenAsync();
        await using (var create = new Npgsql.NpgsqlCommand($"CREATE DATABASE \"{database}\"", connection))
            await create.ExecuteNonQueryAsync();
        var target = new Npgsql.NpgsqlConnectionStringBuilder(admin.ConnectionString) { Database = database }.ConnectionString;
        try
        {
            await using var f = new Fixture(target);
            await f.Db.Database.EnsureCreatedAsync();
            await using (var seed = await f.Db.Database.BeginTransactionAsync())
            {
                await f.Db.Database.ExecuteSqlRawAsync("SET LOCAL session_replication_role = replica");
                await f.Seed(includeOpenResponse: false);
                await seed.CommitAsync();
            }
            var test = Assert.Single((await f.Service.Context(f.Question)).Value.Tests);
            Assert.Null(test.ResponseId);
            var request = await f.Request(f.Candidate(false, true));
            var result = await f.Service.Apply(f.Question, request);
            Assert.True(result.IsSuccess, result.Error?.Description);
            var draft = await f.Db.Responses.SingleAsync(r => r.Id != f.Historical);
            Assert.Equal(draft.Id, Assert.Single(result.Value.ResponseIds));
            Assert.Empty(f.Db.FormResponses);
            Assert.Equal(2, await f.Db.LegacyFormulaConfigurationPins.CountAsync());
            Assert.True((await f.Service.Apply(f.Question, request)).Value.AlreadyApplied);
            Assert.Equal(2, await f.Db.Responses.CountAsync());
        }
        finally
        {
            Npgsql.NpgsqlConnection.ClearAllPools();
            await using var drop = new Npgsql.NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", connection);
            await drop.ExecuteNonQueryAsync();
        }
    }

    [PostgresFact]
    public async Task Postgres_migration_enforces_append_only_history_and_serialises_with_draft_saves()
    {
        var admin = new Npgsql.NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ORYX_FORMULA_REPAIR_TEST_CONNECTION"));
        var database = "oryx_formula_repair_" + Guid.NewGuid().ToString("N");
        await using (var connection = new Npgsql.NpgsqlConnection(admin.ConnectionString))
        { await connection.OpenAsync(); await using var create = new Npgsql.NpgsqlCommand($"CREATE DATABASE \"{database}\"", connection); await create.ExecuteNonQueryAsync(); }
        var target = new Npgsql.NpgsqlConnectionStringBuilder(admin.ConnectionString) { Database = database }.ConnectionString;
        try
        {
            await using var f = new Fixture(target);
            await f.Db.Database.MigrateAsync();
            // The fixture omits unrelated parents (BMRs, steps); skip FK checks for the seed only. Everything under test runs normally.
            await using (var seed = await f.Db.Database.BeginTransactionAsync())
            {
                await f.Db.Database.ExecuteSqlRawAsync("SET LOCAL session_replication_role = replica");
                await f.Seed(); await seed.CommitAsync();
            }
            f.Db.ChangeTracker.Clear();
            var request = await f.Request();
            // A formula draft save in flight holds the question lock shared; the repair must wait for it, not interleave.
            await using (var saver = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(target).Options, f.Actor))
            {
                await using var save = await saver.Database.BeginTransactionAsync();
                await LegacyFormulaConfigurationReader.LockQuestion(saver, f.Question, shared: true);
                var blocked = await Assert.ThrowsAnyAsync<Exception>(() => f.Service.Apply(f.Question, request));
                Assert.Contains("55P03", (blocked as Npgsql.PostgresException ?? blocked.InnerException as Npgsql.PostgresException)?.SqlState ?? blocked.ToString());
            }
            Assert.Empty(await f.Db.QuestionFormulaRepairs.AsNoTracking().ToListAsync());
            Assert.Equal(Before, await f.Db.QuestionOptions.AsNoTracking().Select(o => o.Name).SingleAsync());
            var applied = await f.Service.Apply(f.Question, request);
            Assert.True(applied.IsSuccess, applied.Error?.Description);
            Assert.True((await f.Service.Apply(f.Question, request)).Value.AlreadyApplied);
            Assert.Equal(3, await f.Db.LegacyFormulaConfigurationPins.CountAsync());
            foreach (var sql in new[] { "UPDATE \"QuestionFormulaRepairs\" SET \"Reason\" = 'Tampered reason text'",
                         "DELETE FROM \"LegacyFormulaConfigurationPins\"", "TRUNCATE \"LegacyFormulaRepairEvaluations\" CASCADE" })
            {
                var refused = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => f.Db.Database.ExecuteSqlRawAsync(sql));
                Assert.Contains("append-only", refused.MessageText);
            }
            Assert.Equal("Correct a confirmed formula configuration error.", await f.Db.QuestionFormulaRepairs.AsNoTracking().Select(r => r.Reason).SingleAsync());
        }
        finally
        {
            Npgsql.NpgsqlConnection.ClearAllPools();
            await using var connection = new Npgsql.NpgsqlConnection(admin.ConnectionString); await connection.OpenAsync();
            await using var drop = new Npgsql.NpgsqlCommand($"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)", connection); await drop.ExecuteNonQueryAsync();
        }
    }

    [Theory]
    [InlineData("(:a/:b)*purity")]
    [InlineData(":a/(:b")]
    [InlineData(":a+:b+")]
    [InlineData("process.exit()")]
    public void Invalid_expressions_are_rejected_on_the_server(string expression)
    {
        var candidate = JsonSerializer.Serialize(new { type = 0, expression, decimalPlaces = 2 });
        Assert.NotNull(LegacyFormulaRepairDefinition.Validate(Before, candidate));
    }

    [EngineFact]
    public async Task Real_engine_recalculation_binds_exact_saved_inputs_and_invalidates_after_later_edits()
    {
        await using var f = new Fixture(); await f.Seed();
        var answer = new FormResponse { Id = Guid.NewGuid(), ResponseId = f.Response, FormFieldId = f.Field,
            Value = "{\"a\":\"1.230000\",\"b\":\"2\"}" };
        f.Db.FormResponses.Add(answer); await f.Db.SaveChangesAsync();
        var url = new Uri(Environment.GetEnvironmentVariable("ORYX_FORMULA_REPAIR_SERVICE_URL")!);
        using var http = new HttpClient { BaseAddress = url };
        http.DefaultRequestHeaders.Authorization = new("Bearer", "test-only");
        var client = new FormulaCalculationClient(http, new(true, url, "test-only", TimeSpan.FromSeconds(5)));
        var service = new QuestionFormulaRepairService(f.Db, f.Actor, client);
        var preview = await service.Preview(f.Question, f.Candidate()); Assert.True(preview.IsSuccess, preview.Error?.Description);
        Assert.Empty(preview.Value.Blockers);
        var applied = await service.Apply(f.Question, new(f.Candidate(), preview.Value.PreviewToken, Guid.NewGuid(), "Confirmed against the controlled arithmetic example."));
        Assert.True(applied.IsSuccess, applied.Error?.Description);
        var pin = (await LegacyFormulaConfigurationReader.Latest(f.Db, [f.Response])).Single();
        var field = await f.Db.FormFields.Include(a => a.Question).SingleAsync(); var response = await f.Db.Responses.SingleAsync(r => r.Id == f.Response);
        Assert.NotNull(await LegacyFormulaConfigurationReader.ValidateWrite(f.Db, response, field, null));
        Assert.NotNull(await LegacyFormulaConfigurationReader.ValidateWrite(f.Db, response, field, LegacyFormulaRepairDefinition.Hash(Before)));
        Assert.Null(await LegacyFormulaConfigurationReader.ValidateWrite(f.Db, response, field, pin.DefinitionHash));
        var evaluated = await service.Evaluate(new(f.Atr, f.Field), pin.DefinitionHash);
        Assert.True(evaluated.IsSuccess, evaluated.Error?.Description);
        Assert.False(await LegacyFormulaConfigurationReader.Pending(f.Db, response));
        Assert.Contains("61.50", (await f.Db.LegacyFormulaRepairEvaluations.SingleAsync()).ResultsJson);
        Assert.Equal(evaluated.Value, (await service.Evaluate(new(f.Atr, f.Field), pin.DefinitionHash)).Value);
        answer.Value = "{\"a\":\"9.000000\",\"b\":\"0\"}"; await f.Db.SaveChangesAsync();
        Assert.True(await LegacyFormulaConfigurationReader.Pending(f.Db, response));
        Assert.True((await service.Evaluate(new(f.Atr, f.Field), pin.DefinitionHash)).IsFailure);
        Assert.Single(f.Db.LegacyFormulaRepairEvaluations);
        var repository = new APP.Repository.FormRepository(f.Db, null!, null!, null!, null!);
        var finalize = await repository.SubmitFormResponseFinal(response.Id, f.Actor.UserId!.Value);
        Assert.True(finalize.IsFailure); Assert.Equal("FormulaRepair.RecalculationRequired", finalize.Error.Code);
    }

    [EngineFact]
    public async Task Universal_assay_matches_the_real_engine_for_each_samples_own_weight()
    {
        var raw = """
            {"type":3,"expression":"(:sampleAvg/:standardAvg)*(:standardInput.wt/:sampleInput.wt)*(:wt/:ml*5*1000/:cl)*:purity",
            "decimalPlaces":2,"standardInTable":true,"noOfSamples":2,"standardInputs":[{"key":"wt"}],"sampleInputs":[[{"key":"wt"}],[{"key":"wt"}]],
            "manualVarLabels":{"wt":"Weight per ml","ml":"Volume","cl":"Claim","purity":"Purity"}}
            """;
        var values = """
            {"standardInputs":{"wt":"1"},"sampleInputs":[{"wt":"2"},{"wt":"4"}],"tableData":[["100","110","90"]],
            "manualVars":{"wt":{"raw":"2","evaluated":2},"ml":{"raw":"10","evaluated":10},"cl":{"raw":"5","evaluated":5},"purity":{"raw":"0.98","evaluated":0.98}}}
            """;
        Assert.Null(LegacyFormulaRepairDefinition.Validate(raw, raw));
        var compiled = LegacyFormulaRepairDefinition.Compile(raw);
        var url = new Uri(Environment.GetEnvironmentVariable("ORYX_FORMULA_REPAIR_SERVICE_URL")!);
        using var http = new HttpClient { BaseAddress = url };
        http.DefaultRequestHeaders.Authorization = new("Bearer", "test-only");
        var client = new FormulaCalculationClient(http, new(true, url, "test-only", TimeSpan.FromSeconds(5)));
        var validated = await client.ValidateAsync(LegacyFormulaRepairDefinition.ValidationRequest(raw));
        Assert.True(validated.IsSuccess); Assert.Equal(5, validated.Value.Status); Assert.Equal(compiled.Hash, validated.Value.ComputedDefinitionHash);
        var inputs = LegacyFormulaRepairDefinition.ResolveInputs(raw, values);
        Assert.Equal("2", inputs["sampleInput1.wt"]); Assert.Equal("4", inputs["sampleInput2.wt"]);
        Assert.Equal("110", inputs["sample1Avg"]); Assert.Equal("90", inputs["sample2Avg"]);
        Assert.Equal("0.98", inputs["purity"]);
    }

    [Fact]
    public async Task Context_offers_the_approved_revision_and_each_test_s_current_formula()
    {
        await using var f = new Fixture(); await f.Seed();
        await f.Approve(After, revision: 3);

        var context = await f.Service.Context(f.Question, f.Atr);

        Assert.True(context.IsSuccess);
        Assert.Equal(After, context.Value.ApprovedDefinition);
        Assert.Equal(3, context.Value.ApprovedRevision);
        Assert.Equal(Before, context.Value.Definition);
        Assert.Equal(Before, Assert.Single(context.Value.Tests).CurrentDefinition);
    }

    [Fact]
    public async Task With_an_approved_revision_only_that_formula_can_be_applied()
    {
        await using var f = new Fixture(); await f.Seed();
        await f.Approve(After);
        const string typed = "{\"type\":0,\"expression\":\":a/:b*10\",\"decimalPlaces\":2}";

        var other = await f.Service.Preview(f.Question, new(typed, false, [new(f.Atr, f.Field)]));
        var approved = await f.Service.Preview(f.Question, new(After, false, [new(f.Atr, f.Field)]));

        Assert.Contains(other.Value.Blockers, text => text.Contains("approved revision 2"));
        Assert.Empty(approved.Value.Blockers);
    }

    [Fact]
    public async Task One_repair_fixes_every_field_of_the_formula_on_the_worksheet()
    {
        await using var f = new Fixture(); await f.Seed(includeOpenResponse: false);
        var second = await f.SecondField();

        var context = await f.Service.Context(f.Question, f.Atr);
        Assert.Equal(2, context.Value.Tests.Count(test => test.AtrId == f.Atr && test.Eligible));
        var request = await f.Request(new(After, false, [new(f.Atr, f.Field), new(f.Atr, second)]));
        Assert.True((await f.Service.Apply(f.Question, request)).IsSuccess);

        var draft = await f.Db.Responses.SingleAsync(r => r.Id != f.Historical);
        var pins = await LegacyFormulaConfigurationReader.Latest(f.Db, [draft.Id]);
        Assert.Equal(new[] { f.Field, second }.Order(), pins.Select(pin => pin.FormFieldId).Order());
        Assert.All(pins, pin => Assert.Equal(After, pin.Definition));
    }

    [Fact]
    public async Task The_opened_test_explains_why_a_field_cannot_be_repaired()
    {
        await using var f = new Fixture(); await f.Seed();
        // The field is assigned to someone else on this test.
        foreach (var assignee in f.Db.FormFieldAssignees) assignee.AssigneeId = Guid.NewGuid();
        await f.Db.SaveChangesAsync();

        var listed = await f.Service.Context(f.Question);
        var opened = await f.Service.Context(f.Question, f.Atr);

        Assert.Empty(listed.Value.Tests);
        var test = Assert.Single(opened.Value.Tests);
        Assert.False(test.Eligible);
        Assert.Equal("You are not assigned to this field on this test.", test.Blocker);
    }

    [Fact]
    public async Task An_approved_revision_may_carry_settings_the_original_option_never_had()
    {
        await using var f = new Fixture(); await f.Seed();
        // The formula editor stores variable scopes; a hand-typed repair may not change them, the approved revision may.
        const string edited = "{\"type\":0,\"expression\":\":a/:b*100\",\"decimalPlaces\":2,\"variableScopes\":{}}";

        var typed = await f.Service.Preview(f.Question, new(edited, false, [new(f.Atr, f.Field)]));
        Assert.Contains(typed.Value.Blockers, text => text.EndsWith("Changed: variableScopes."));

        await f.Approve(edited);
        var approved = await f.Service.Preview(f.Question, new(edited, false, [new(f.Atr, f.Field)]));
        Assert.Empty(approved.Value.Blockers);
    }

    [Fact]
    public async Task An_approved_revision_replaces_the_test_s_formula_however_different_it_is()
    {
        await using var f = new Fixture(); await f.Seed();
        // A different type, inputs and precision: the approved revision still overrides what the test has.
        const string universal = "{\"type\":3,\"expression\":\":standardWeight/:sampleWeight*:purity\",\"decimalPlaces\":4,"
            + "\"manualVarLabels\":{\"standardWeight\":\"Standard weight\",\"sampleWeight\":\"Sample weight\",\"purity\":\"Purity\"}}";
        await f.Approve(universal);

        var request = await f.Request(new(universal, false, [new(f.Atr, f.Field)]));
        Assert.True((await f.Service.Apply(f.Question, request)).IsSuccess);

        var pin = (await LegacyFormulaConfigurationReader.Latest(f.Db, [f.Response])).Single(item => item.FormFieldId == f.Field);
        Assert.Equal(universal, pin.Definition);
        Assert.True(pin.RequiresRecalculation);
    }

    [Fact]
    public async Task An_assigned_analyst_repairs_another_department_s_product_and_sees_a_plain_label()
    {
        await using var f = new Fixture(); await f.Seed();
        // QC analysts are assigned to fields on every department's products, as on the worksheet itself.
        foreach (var product in f.Db.Products) product.DepartmentId = Guid.NewGuid();
        foreach (var section in f.Db.FormSections) section.Name = "<p><span style=\"color: black;\"><strong>Assay: </strong>Constituted &amp; stored</span></p>";
        await f.Db.SaveChangesAsync();

        var test = Assert.Single((await f.Service.Context(f.Question, f.Atr)).Value.Tests);

        Assert.True(test.Eligible, test.Blocker);
        Assert.Equal("AR-TEST · Assay: Constituted & stored", test.Label);
    }

    [Fact]
    public async Task The_approved_revision_replaces_the_readings_and_is_calculated_with_its_own_definition()
    {
        await using var f = new Fixture(); await f.Seed();
        f.Db.FormResponses.Add(new FormResponse { Id = Guid.NewGuid(), ResponseId = f.Response, FormFieldId = f.Field, Value = "{\"a\":\"9\",\"b\":\"3\",\"results\":[\"3\"]}" });
        await f.Db.SaveChangesAsync();
        // Named expressions and an averaged result: structures only the revision's compiled definition can calculate.
        const string approved = "{\"type\":0,\"expression\":\":ratio*100\",\"namedExpressions\":[{\"name\":\"ratio\",\"expression\":\":a/:b\"}],\"averageAllResults\":true}";
        const string compiled = "{\"formulaLanguageVersion\":\"oryx-formula-v1\",\"numericPolicyVersion\":\"oryx-decimal-v1-half-up\","
            + "\"variables\":[{\"key\":\"a\"},{\"key\":\"b\"}],\"derivedExpressions\":[{\"key\":\"ratio\",\"expression\":\":a/:b\"}],"
            + "\"results\":[{\"key\":\"sample1\",\"expression\":\":ratio*100\"},{\"key\":\"average\",\"expression\":\":ratio*100\"}]}";
        await f.Approve(approved, compiled: compiled);

        var preview = await f.Service.Preview(f.Question, new(approved, false, [new(f.Atr, f.Field)]));
        Assert.Empty(preview.Value.Blockers);
        Assert.Contains(preview.Value.MissingInputs, text => text.Contains("readings are cleared"));
        var request = new ApplyFormulaRepairRequest(new(approved, false, [new(f.Atr, f.Field)]), preview.Value.PreviewToken, Guid.NewGuid(), "Apply the approved assay revision.");
        Assert.True((await f.Service.Apply(f.Question, request)).IsSuccess);

        // The old readings are cleared (the repair evidence keeps them) and nothing is calculated until they are entered again.
        var cleared = await f.Db.FormResponses.SingleAsync(a => a.ResponseId == f.Response);
        Assert.Equal("{}", cleared.Value);
        Assert.Contains("\\u00229\\u0022", (await f.Db.QuestionFormulaRepairs.SingleAsync()).EvidenceJson.Replace("\\\"", "\\u0022"));
        var pin = (await LegacyFormulaConfigurationReader.Latest(f.Db, [f.Response])).Single();
        Assert.Equal(approved, pin.Definition);
        Assert.True((await f.Service.Evaluate(new(f.Atr, f.Field), pin.DefinitionHash)).IsFailure);

        cleared.Value = "{\"a\":\"2\",\"b\":\"4\"}";
        await f.Db.SaveChangesAsync();
        var evaluated = await f.Service.Evaluate(new(f.Atr, f.Field), pin.DefinitionHash);

        Assert.True(evaluated.IsSuccess, evaluated.Error?.Description);
        var sent = f.Calculation.Evaluated!.Value;
        Assert.Equal(new string('d', 64), sent.GetProperty("definitionHash").GetString());
        Assert.Equal(2, sent.GetProperty("definition").GetProperty("results").GetArrayLength());
        Assert.Equal("2", sent.GetProperty("resolvedInputs").GetProperty("a").GetString());
        Assert.Equal("4", sent.GetProperty("resolvedInputs").GetProperty("b").GetString());
    }
}

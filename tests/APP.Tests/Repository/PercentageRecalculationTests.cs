using System.Reflection;
using System.Text.Json;
using API.Controllers;
using APP.Services.Formulas;
using APP.Utils;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
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
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

/// <summary>Repaired Percentage results stored as fractions are recalculated in percent, keeping the earlier result in the history.</summary>
public sealed class PercentageRecalculationTests
{
    private const string Authoring = "{\"type\":0,\"expression\":\":a/:b\",\"decimalPlaces\":2,\"percentageResults\":true}";
    private const string Approved = """
        {"formulaLanguageVersion":"oryx-formula-v1","numericPolicyVersion":"oryx-decimal-v1-half-up",
         "variables":[{"key":"a","label":"a","dataType":0,"valueShape":0,"scope":0,"unit":null,"required":true},
                      {"key":"b","label":"b","dataType":0,"valueShape":0,"scope":0,"unit":null,"required":true}],
         "results":[{"key":"result","label":"Result","expression":":a/:b","unit":"%","numericScale":2,"rounding":{"kind":0,"digits":2}}]}
        """;

    // ---- The percentage factor ----

    [Theory]
    [InlineData("{\"type\":3,\"expression\":\":x\",\"percentageResults\":true}", true)]
    [InlineData("{\"type\":0,\"expression\":\":x\",\"percent\":true}", true)]
    [InlineData("{\"type\":0,\"expression\":\":x\",\"percentageResults\":false}", false)]
    [InlineData("{\"mode\":\"tablet\",\"sampleCount\":20,\"nominal\":500,\"percent\":true}", false)]
    [InlineData("not json", false)]
    public void Applies_to_percentage_formulas_except_uniformity_of_weight(string authoring, bool applies) =>
        Assert.Equal(applies, PercentageFormulaScale.Applies(authoring));

    [Theory]
    [InlineData("(:a/:b) * 100", true)]
    [InlineData("((:a + :b) / :c) * 100", true)]
    [InlineData("(:a) * (:b) * 100", false)]
    [InlineData(":a/:b * 100", false)]
    [InlineData(":a/:b", false)]
    public void Recognises_a_result_already_multiplied_by_100(string expression, bool scaled) =>
        Assert.Equal(scaled, PercentageFormulaScale.IsScaled(expression));

    [Fact]
    public void Scales_only_percentage_point_results_that_lack_the_factor()
    {
        using var definition = JsonDocument.Parse("""
            {"results":[{"key":"sample1","expression":":a/:b","numericScale":2},
                        {"key":"sample2","expression":"(:c/:d) * 100","numericScale":2},
                        {"key":"weight","expression":":e - :f","numericScale":0}]}
            """);
        var scaled = PercentageFormulaScale.Scale(definition.RootElement);
        Assert.NotNull(scaled);
        var expressions = scaled.Value.GetProperty("results").EnumerateArray().Select(r => r.GetProperty("expression").GetString()).ToList();
        Assert.Equal(["(:a/:b) * 100", "(:c/:d) * 100", ":e - :f"], expressions);
        Assert.Null(PercentageFormulaScale.Scale(scaled.Value));
    }

    // ---- Permission ----

    [Theory]
    [InlineData(nameof(FormulaRepairController.PercentageRecalculation))]
    [InlineData(nameof(FormulaRepairController.ApplyPercentageRecalculation))]
    public void Endpoints_require_the_percentage_recalculation_key(string action)
    {
        var method = typeof(FormulaRepairController).GetMethod(action)!;
        Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>(), attribute => attribute.Policy == PermissionKeys.CanRecalculatePercentageFormulaResults);
        Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Single(PermissionUtils.GeneratePermissions(), permission => permission.Key == PermissionKeys.CanRecalculatePercentageFormulaResults);
    }

    [Fact]
    public async Task Without_the_key_nothing_is_listed_or_recalculated()
    {
        await using var f = new Fixture(); await f.Seed(grant: false);
        Assert.True((await f.Service.PercentagePreview()).IsFailure);
        Assert.True((await f.Service.PercentageApply(f.Request())).IsFailure);
        Assert.Single(f.Db.LegacyFormulaConfigurationPins);
        Assert.Single(f.Db.LegacyFormulaRepairEvaluations);
    }

    // ---- Recalculation ----

    [Fact]
    public async Task Recalculates_in_percent_and_keeps_the_earlier_result_and_its_finality()
    {
        await using var f = new Fixture(); await f.Seed();

        var listed = Assert.Single((await f.Service.PercentagePreview()).Value);
        Assert.Equal("E001K026", listed.Batch);
        Assert.True(listed.Finalized);
        Assert.Null(listed.Blocker);

        var outcome = Assert.Single((await f.Service.PercentageApply(f.Request())).Value);
        Assert.True(outcome.Recalculated, outcome.Problem);
        Assert.Equal("(:a/:b) * 100", f.Calculation.Evaluated!.Value.GetProperty("definition").GetProperty("results")[0].GetProperty("expression").GetString());

        var pins = await f.Db.LegacyFormulaConfigurationPins.OrderBy(p => p.Sequence).ToListAsync();
        Assert.Equal([1, 2], pins.Select(p => p.Sequence));
        Assert.Equal(pins[0].Definition, pins[1].Definition);
        var evaluation = await f.Db.LegacyFormulaRepairEvaluations.SingleAsync(e => e.PinId == pins[1].Id);
        Assert.True(PercentageFormulaScale.Recorded(evaluation.EvidenceJson));
        Assert.Equal(2, await f.Db.LegacyFormulaRepairFinalizations.CountAsync());
        Assert.Contains(f.Db.LegacyFormulaRepairFinalizations, item => item.EvaluationId == evaluation.Id);
        var repair = await f.Db.QuestionFormulaRepairs.SingleAsync(r => r.Reason != "Earlier repair");
        Assert.Equal("Correct the assay results stored without the percentage factor.", repair.Reason);
        Assert.False(repair.UpdatedQuestion);

        // Done once: the list is empty and a repeat changes nothing.
        Assert.Empty((await f.Service.PercentagePreview()).Value);
        Assert.False(Assert.Single((await f.Service.PercentageApply(f.Request())).Value).Recalculated);
        Assert.Equal(2, await f.Db.LegacyFormulaConfigurationPins.CountAsync());
    }

    [Fact]
    public async Task A_released_test_is_listed_with_its_blocker_and_not_recalculated()
    {
        await using var f = new Fixture(); await f.Seed(status: AnalyticalTestStatus.Released);

        Assert.Contains("Released", Assert.Single((await f.Service.PercentagePreview()).Value).Blocker);
        var outcome = Assert.Single((await f.Service.PercentageApply(f.Request())).Value);
        Assert.False(outcome.Recalculated);
        Assert.Single(f.Db.LegacyFormulaConfigurationPins);
        Assert.Single(f.Db.QuestionFormulaRepairs); // only the earlier repair
    }

    [Fact]
    public async Task A_short_reason_is_refused()
    {
        await using var f = new Fixture(); await f.Seed();
        Assert.True((await f.Service.PercentageApply(f.Request() with { Reason = "fix" })).IsFailure);
    }

    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? DepartmentId { get; } = Guid.NewGuid();
        public string DepartmentType => "QualityControl";
    }

    /// <summary>Echoes the hashes it is sent, and answers every result with 102.85, as an engine would for the scaled formula.</summary>
    private sealed class Client : IFormulaCalculationClient
    {
        public JsonElement? Evaluated { get; private set; }
        public Task<Result<bool>> IsReadyAsync(CancellationToken token = default) => Task.FromResult(Result.Success(true));
        public Task<Result<FormulaServiceResponse>> ValidateAsync(JsonElement request, CancellationToken token = default) =>
            Task.FromResult(Result.Success(new FormulaServiceResponse("oryx-formula-service-response-v1", 0, "test", 5, "COMPLETED", "test", new string('a', 64),
                request.GetProperty("definitionHash").GetString(), new string('e', 64), null, null, null, null, null, [], [], [], [], null, [])));
        public Task<Result<FormulaServiceResponse>> EvaluateAsync(JsonElement request, CancellationToken token = default)
        {
            Evaluated = request.Clone();
            var results = request.GetProperty("definition").GetProperty("results").EnumerateArray()
                .Select(result => new { key = result.GetProperty("key").GetString(), exactResult = "102.85", roundedResult = "102.85", displayedResult = "102.85", unit = "%" }).ToArray();
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
        public Guid Response { get; } = Guid.NewGuid();
        public Guid Field { get; } = Guid.NewGuid();

        public Fixture()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(), o => o.EnableNullChecks(false));
            Db = new(options.Options, Actor);
            Service = new(Db, Actor, Calculation);
        }

        public ApplyPercentageRecalculationRequest Request() =>
            new("Correct the assay results stored without the percentage factor.", [new(Response, Field)]);

        public async Task Seed(bool grant = true, AnalyticalTestStatus status = AnalyticalTestStatus.TestTaken)
        {
            var role = Guid.NewGuid(); var form = Guid.NewGuid(); var section = Guid.NewGuid(); var question = Guid.NewGuid();
            var product = Guid.NewGuid(); var schedule = Guid.NewGuid(); var bmr = Guid.NewGuid(); var step = Guid.NewGuid();
            Db.Users.Add(new User { Id = Actor.UserId!.Value, UserName = "qc-officer" });
            Db.Roles.Add(new Role { Id = role, Name = "QC" });
            Db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = Actor.UserId.Value, RoleId = role });
            if (grant)
            {
                Db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = 1, RoleId = role, ClaimType = AppConstants.Permission, ClaimValue = PermissionKeys.CanRecalculatePercentageFormulaResults });
                Db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = 1, Key = PermissionKeys.CanRecalculatePercentageFormulaResults, Type = "Action" });
            }
            Db.Forms.Add(new Form { Id = form, Name = "Blopen Gel - Bulk" });
            Db.FormSections.Add(new FormSection { Id = section, FormId = form, Name = "Assay" });
            Db.Questions.Add(new Question { Id = question, Label = "Gel - Assay", Type = QuestionType.Formula });
            Db.QuestionOptions.Add(new QuestionOption { Id = Guid.NewGuid(), QuestionId = question, Name = Authoring });
            Db.FormFields.Add(new FormField { Id = Field, FormSectionId = section, QuestionId = question });
            Db.Products.Add(new Product { Id = product, Name = "Blopen Gel" });
            Db.ProductionScheduleProducts.Add(new ProductionScheduleProduct { Id = schedule, ProductId = product });
            Db.ProductionActivitySteps.Add(new ProductionActivityStep { Id = step });
            Db.BatchManufacturingRecords.Add(new BatchManufacturingRecord { Id = bmr, ProductionScheduleProductId = schedule, ProductionActivityStepId = step, BatchNumber = "E001K026" });
            Db.AnalyticalTestRequests.Add(new AnalyticalTestRequest { Id = Guid.NewGuid(), ProductionScheduleProductId = schedule, BatchManufacturingRecordId = bmr,
                ProductionActivityStepId = step, ArNumber = "AR QCD/BP/26/0016", Status = status,
                ReleasedAt = status == AnalyticalTestStatus.Released ? DateTime.UtcNow : null });
            Db.Responses.Add(new Response { Id = Response, FormId = form, BatchManufacturingRecordId = bmr, ProductionActivityStepId = step, CreatedById = Actor.UserId });
            const string answer = "{\"a\":\"1.0285\",\"b\":\"1\"}";
            Db.FormResponses.Add(new FormResponse { Id = Guid.NewGuid(), ResponseId = Response, FormFieldId = Field, Value = answer, CreatedAt = DateTime.UtcNow });

            var formula = Guid.NewGuid();
            Db.FormulaDefinitions.Add(new FormulaDefinition { Id = formula, Key = $"question-{question:N}", Name = "Gel - Assay", PresentationPreset = "legacy" });
            Db.QuestionFormulaDefinitions.Add(new QuestionFormulaDefinition { Id = Guid.NewGuid(), QuestionId = question, FormulaDefinitionId = formula });
            var revision = new FormulaRevision { Id = Guid.NewGuid(), FormulaDefinitionId = formula, Revision = 1, Status = FormulaRevisionStatus.Draft,
                AuthoringPayloadJson = Authoring, DefinitionJson = Approved, DefinitionHash = new string('d', 64) };
            Db.FormulaRevisions.Add(revision);
            await Db.SaveChangesAsync();
            // Revisions are created as drafts and move through review, each step with its audit row.
            foreach (var next in new[] { FormulaRevisionStatus.InReview, FormulaRevisionStatus.Approved })
            {
                var prior = revision.Status;
                revision.Status = next;
                if (next == FormulaRevisionStatus.Approved)
                { revision.ApprovedById = Actor.UserId; revision.ApprovedAt = DateTime.UtcNow; revision.EffectiveAt = DateTime.UtcNow.AddMinutes(-1); }
                Db.FormulaRevisionAudits.Add(FormulaDefinitionMapping.Audit(revision, prior, next, next.ToString(), "Test fixture.", Actor.UserId!.Value, Guid.NewGuid()));
                await Db.SaveChangesAsync();
            }

            var repair = Guid.NewGuid(); var pin = Guid.NewGuid(); var evaluation = Guid.NewGuid();
            Db.QuestionFormulaRepairs.Add(new QuestionFormulaRepair { Id = repair, QuestionId = question, ActorId = Actor.UserId.Value, Reason = "Earlier repair", AppliedAt = DateTime.UtcNow });
            Db.LegacyFormulaConfigurationPins.Add(new LegacyFormulaConfigurationPin { Id = pin, ResponseId = Response, FormFieldId = Field, Sequence = 1, RepairId = repair,
                Definition = Authoring, DefinitionHash = LegacyFormulaRepairDefinition.Hash(Authoring), RequiresRecalculation = true, CapturedAt = DateTime.UtcNow });
            Db.LegacyFormulaRepairEvaluations.Add(new LegacyFormulaRepairEvaluation { Id = evaluation, PinId = pin, ActorId = Actor.UserId.Value,
                InputHash = LegacyFormulaRepairDefinition.InputHash(answer), ResultsJson = "[{\"key\":\"result\",\"unit\":\"%\",\"value\":\"1.03\"}]",
                EngineBuildHash = new string('a', 64), EvidenceJson = "{}", EvaluatedAt = DateTime.UtcNow });
            Db.LegacyFormulaRepairFinalizations.Add(new LegacyFormulaRepairFinalization { Id = Guid.NewGuid(), EvaluationId = evaluation, ResponseId = Response,
                ActorId = Actor.UserId.Value, FinalizedAt = DateTime.UtcNow });
            await Db.SaveChangesAsync();
        }

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
}

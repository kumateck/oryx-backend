using System.Text.Json;
using APP.Services.Formulas;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using SHARED;
using Xunit;

namespace APP.Tests.Repository;

public sealed class FormulaResultSourceResolverTests
{
    [Fact]
    public async Task Resolves_latest_valid_authoritative_dependency_result()
    {
        var seeded = await SeedAsync();
        using var source = JsonDocument.Parse(Source("source", "result"));
        var persistedSource = await seeded.Context.ResponseFormulaSnapshots
            .SingleAsync(item => item.PlacementKey == "source");
        persistedSource.Response = seeded.Target.Response;
        Assert.Single(await seeded.Context.FormFieldRevisions.ToListAsync());
        Assert.Equal("{}", (await seeded.Context.FormResponses.SingleAsync()).Value);
        var inputs = await FormulaResponseInputResolver.ResolveAsync(
            seeded.Context, persistedSource, new NoopClient(), default, []);
        Assert.True(inputs.IsSuccess, string.Join(",", inputs.Errors.Select(item => item.Code)));

        var result = await FormulaResultSourceResolver.ResolveAsync(
            seeded.Context, seeded.Target, source.RootElement, new NoopClient(),
            default, []);

        Assert.Equal("12.50", result);
    }

    [Fact]
    public async Task Rejects_unknown_result_and_self_dependency()
    {
        var seeded = await SeedAsync();
        using var unknown = JsonDocument.Parse(Source("source", "unknown"));
        using var self = JsonDocument.Parse(Source("target", "result"));

        Assert.Null(await FormulaResultSourceResolver.ResolveAsync(
            seeded.Context, seeded.Target, unknown.RootElement, new NoopClient(),
            default, []));
        Assert.Null(await FormulaResultSourceResolver.ResolveAsync(
            seeded.Context, seeded.Target, self.RootElement, new NoopClient(),
            default, []));
    }

    [Fact]
    public async Task Rejects_reference_that_disagrees_with_dependency()
    {
        var seeded = await SeedAsync();
        using var mismatched = JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            sourceType = 8,
            reference = "placement/different/result/result",
            dependencies = new[] { new { placementKey = "source", resultKey = "result" } }
        }));

        Assert.Null(await FormulaResultSourceResolver.ResolveAsync(
            seeded.Context, seeded.Target, mismatched.RootElement, new NoopClient(),
            default, []));
    }

    private static string Source(string placementKey, string resultKey) =>
        JsonSerializer.Serialize(new
        {
            sourceType = 8, reference = $"placement/{placementKey}/result/{resultKey}",
            dependencies = new[] { new { placementKey, resultKey } }
        });

    private static async Task<Seeded> SeedAsync()
    {
        var actor = Guid.NewGuid();
        var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            new TestUser(actor));
        var response = new Response
        {
            Id = Guid.NewGuid(), CreatedById = actor, FormRevisionId = Guid.NewGuid()
        };
        var form = new Form { Id = Guid.NewGuid(), Name = "Dependency test" };
        var section = new FormSection
        {
            Id = Guid.NewGuid(), FormId = form.Id, Form = form,
            Name = "Results", Description = ""
        };
        var question = new Question
        {
            Id = Guid.NewGuid(), Label = "Source", Type = QuestionType.ShortAnswer,
            Validation = QuestionValidationType.None
        };
        var field = new FormField
        {
            Id = Guid.NewGuid(), FormSectionId = section.Id, FormSection = section,
            QuestionId = question.Id, Question = question
        };
        var fieldRevision = new FormFieldRevision
        {
            Id = Guid.NewGuid(), FormRevisionId = response.FormRevisionId.Value,
            PlacementKey = "source", FormFieldId = field.Id, QuestionId = question.Id,
            FieldHash = new string('e', 64)
        };
        var source = Snapshot(response, "source");
        var target = Snapshot(response, "target");
        context.AddRange(form, section, question, response, field, fieldRevision, new FormResponse
        {
            Id = Guid.NewGuid(), ResponseId = response.Id,
            FormFieldId = field.Id, Value = "{}"
        }, source, target, new FormulaExecution
        {
            Id = Guid.NewGuid(), ResponseFormulaSnapshotId = source.Id,
            ResponseFormulaSnapshot = source, Status = FormulaExecutionStatus.Valid,
            Authority = FormulaExecutionAuthority.AuthoritativeServer,
            EngineVersion = "test", EngineBuildHash = new string('c', 64),
            FormulaLanguageVersion = "oryx-formula-v1",
            NumericPolicyVersion = "oryx-decimal-v1-half-up",
            InputHash = FormulaCanonicalJson.HashCanonical(
                "oryx:formula-resolved-inputs:v1", "{}"), ResolvedInputsJson = "{}",
            RawResultsJson = "[{\"key\":\"result\",\"value\":\"12.50\",\"unit\":null}]",
            RoundedResultsJson = "[{\"key\":\"result\",\"value\":\"12.50\",\"unit\":null}]",
            DisplayResultsJson = "[{\"key\":\"result\",\"value\":\"12.50\",\"unit\":null}]",
            CalculationTraceJson = "[]", IdempotencyKey = "test:0123456789abcdef",
            ExecutedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        return new Seeded(context, target);
    }

    private static ResponseFormulaSnapshot Snapshot(Response response, string key) => new()
    {
        Id = Guid.NewGuid(), ResponseId = response.Id, Response = response,
        PlacementKey = key, Sequence = 1, FormulaRevisionId = Guid.NewGuid(),
        DefinitionHash = new string('a', 64), ConfigurationHash = new string('b', 64),
        ExecutableDefinitionJson = "{}", BindingsJson = "[]", ResultTargetsJson = "[]",
        TableShapeJson = "{}", CalculationPolicyJson = "{}",
        DisplayPolicyJson = "{}", MethodReference = "method", CapturedAt = DateTime.UtcNow
    };

    private sealed record Seeded(
        ApplicationDbContext Context, ResponseFormulaSnapshot Target);

    private sealed class TestUser(Guid id) : ICurrentUserService
    {
        public Guid? UserId => id;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    private sealed class NoopClient : IFormulaCalculationClient
    {
        public Task<Result<FormulaServiceResponse>> ValidateAsync(
            JsonElement request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<Result<FormulaServiceResponse>> EvaluateAsync(
            JsonElement request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<Result<bool>> IsReadyAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success(true));
    }
}

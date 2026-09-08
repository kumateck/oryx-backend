using System.Text.Json;
using APP.Services.Formulas;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

internal sealed class RuntimeTestUser(Guid userId) : ICurrentUserService
{
    public Guid? UserId => userId;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

internal sealed class RuntimeFormulaClient : IFormulaCalculationClient
{
    public Task<Result<FormulaServiceResponse>> ValidateAsync(
        JsonElement request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Result<FormulaServiceResponse>> EvaluateAsync(
        JsonElement request, CancellationToken cancellationToken = default)
    {
        var definitionHash = request.GetProperty("definitionHash").GetString();
        var configurationHash = request.GetProperty("configurationHash").GetString();
        var placement = request.GetProperty("configuration")
            .GetProperty("placementKey").GetString();
        var inputs = request.GetProperty("resolvedInputs");
        var inputHash = FormulaCanonicalJson.HashJson(
            "oryx:formula-resolved-inputs:v1", inputs.GetRawText(), 262_144);
        var evaluation = JsonSerializer.SerializeToElement(new
        {
            evaluatorVersion = "oryx-formula-evaluator-spike-v1",
            status = 0,
            results = new[] { new
            {
                key = "result", exactResult = "3", roundedResult = "3.00",
                displayedResult = "3.00", unit = (string?)null
            } },
            errors = Array.Empty<object>(), trace = Array.Empty<object>(),
            traceTruncated = false, operationCount = 1
        });
        return Task.FromResult(Result.Success(new FormulaServiceResponse(
            "oryx-formula-service-response-v1", 1, "test", 5, "COMPLETED",
            "oryx-formula-evaluator-spike-v1", new string('a', 64),
            definitionHash, definitionHash, configurationHash, configurationHash,
            inputHash, placement, null, [], [], [], [], evaluation, [])));
    }

    public Task<Result<bool>> IsReadyAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success(true));
}

public sealed partial class FormulaResponseRuntimeServiceTests
{
    [Fact]
    public async Task ApprovedPlacement_CreatesSnapshotAndIdempotentExecution()
    {
        var actorId = Guid.NewGuid();
        await using var context = CreateContext(actorId);
        var setup = await SeedAsync(context, actorId);
        var service = new FormulaResponseRuntimeService(context, new RuntimeFormulaClient());

        var snapshots = await service.EnsureInitialSnapshotsAsync(
            setup.ResponseId, actorId);
        Assert.True(snapshots.IsSuccess);
        Assert.Single(snapshots.Value);

        const string key = "attempt:0123456789abcdef";
        var first = await service.EvaluateStoredInputsAsync(
            setup.ResponseId, setup.PlacementKey, key, actorId);
        var repeated = await service.EvaluateStoredInputsAsync(
            setup.ResponseId, setup.PlacementKey, key, actorId);

        Assert.True(first.IsSuccess);
        Assert.Equal(FormulaExecutionAuthority.AuthoritativeServer.ToString(),
            first.Value.AuthorityName);
        Assert.Equal(first.Value.Id, repeated.Value.Id);
        Assert.Single(await context.FormulaExecutions.ToListAsync());

        var submissionService = new FormulaSubmissionService(
            context, service, new RuntimeFormulaClient());
        var submission = await submissionService.FinalizeAsync(setup.ResponseId, actorId);
        var repeatedSubmission = await submissionService.FinalizeAsync(
            setup.ResponseId, actorId);
        Assert.True(submission.IsSuccess);
        Assert.NotNull(submission.Value);
        Assert.Equal(submission.Value!.Id, repeatedSubmission.Value!.Id);
        Assert.Single(await context.ResponseFormulaSubmissionSets.ToListAsync());
        Assert.Equal(2, await context.FormulaExecutions.CountAsync());

        var source = await context.FormResponses.SingleAsync();
        var projected = new FormResponseDto
        {
            Id = source.Id, ResponseId = source.ResponseId
        };
        await FormulaResultProjectionService.AttachAsync(context, [source], [projected]);
        Assert.True(projected.FormulaGoverned);
        Assert.True(projected.FormulaResultFinalized);
        Assert.Equal(Assert.Single(submission.Value.ExecutionIds), projected.FormulaExecutionId);
        Assert.Contains("3.00", projected.FormulaDisplayResultsJson);
    }

    [Fact]
    public async Task OtherUser_CannotSnapshotOrEvaluateUnassignedResponse()
    {
        var actorId = Guid.NewGuid();
        await using var context = CreateContext(actorId);
        var setup = await SeedAsync(context, actorId);
        var service = new FormulaResponseRuntimeService(context, new RuntimeFormulaClient());

        var snapshots = await service.EnsureInitialSnapshotsAsync(
            setup.ResponseId, actorId);
        var forbiddenSnapshot = await service.EnsureInitialSnapshotsAsync(
            setup.ResponseId, Guid.NewGuid());
        var forbiddenExecution = await service.EvaluateStoredInputsAsync(
            setup.ResponseId, setup.PlacementKey, "attempt:0123456789abcdef",
            Guid.NewGuid());

        Assert.True(snapshots.IsSuccess);
        Assert.Equal("FormulaRuntime.Unauthorized",
            Assert.Single(forbiddenSnapshot.Errors).Code);
        Assert.Equal("FormulaRuntime.Unauthorized",
            Assert.Single(forbiddenExecution.Errors).Code);
    }

    [Fact]
    public async Task ExistingSnapshots_ReturnLatestGenerationForEachPlacement()
    {
        var actorId = Guid.NewGuid();
        await using var context = CreateContext(actorId);
        var setup = await SeedAsync(context, actorId);
        var service = new FormulaResponseRuntimeService(context, new RuntimeFormulaClient());
        var initial = await service.EnsureInitialSnapshotsAsync(setup.ResponseId, actorId);
        var predecessor = await context.ResponseFormulaSnapshots.SingleAsync();
        var replacement = CopySnapshot(predecessor, actorId);
        context.ResponseFormulaSnapshots.Add(replacement);
        await context.SaveChangesAsync();

        var snapshots = await service.EnsureInitialSnapshotsAsync(setup.ResponseId, actorId);

        Assert.True(initial.IsSuccess);
        Assert.True(snapshots.IsSuccess);
        var latest = Assert.Single(snapshots.Value);
        Assert.Equal(replacement.Id, latest.Id);
        Assert.Equal(2, latest.Sequence);
    }

    [Fact]
    public async Task ExistingSnapshots_WithUnexpectedPlacement_AreRejected()
    {
        var actorId = Guid.NewGuid();
        await using var context = CreateContext(actorId);
        var setup = await SeedAsync(context, actorId);
        var service = new FormulaResponseRuntimeService(context, new RuntimeFormulaClient());
        await service.EnsureInitialSnapshotsAsync(setup.ResponseId, actorId);
        var source = await context.ResponseFormulaSnapshots.SingleAsync();
        context.ResponseFormulaSnapshots.Add(CopySnapshot(
            source, actorId, placementKey: "unexpected", initial: true));
        await context.SaveChangesAsync();

        var snapshots = await service.EnsureInitialSnapshotsAsync(setup.ResponseId, actorId);

        Assert.True(snapshots.IsFailure);
        Assert.Equal("FormulaRuntime.ConfigurationUnavailable",
            Assert.Single(snapshots.Errors).Code);
    }

}

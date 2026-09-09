using APP.Services.Formulas;
using DOMAIN.Entities.Formulas;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.Repository;

public sealed partial class FormulaResponseRuntimeServiceTests
{
    [Fact]
    public async Task ReplayedExecutionKey_StillRequiresResponseAuthorization()
    {
        var actor = Guid.NewGuid();
        await using var context = CreateContext(actor);
        var setup = await SeedAsync(context, actor);
        var runtime = new FormulaResponseRuntimeService(context, new RuntimeFormulaClient());
        await runtime.EnsureInitialSnapshotsAsync(setup.ResponseId, actor);
        const string key = "attempt:authorized-retry";
        Assert.True((await runtime.EvaluateStoredInputsAsync(
            setup.ResponseId, setup.PlacementKey, key, actor)).IsSuccess);

        var denied = await runtime.EvaluateStoredInputsAsync(
            setup.ResponseId, setup.PlacementKey, key, Guid.NewGuid());
        Assert.True(denied.IsFailure);
        Assert.Equal("FormulaRuntime.Unauthorized", Assert.Single(denied.Errors).Code);
        Assert.Single(await context.FormulaExecutions.ToListAsync());
    }

    [Fact]
    public async Task RetiredPinnedRevision_CanFinishExistingResponseButNotStartAnother()
    {
        var actor = Guid.NewGuid();
        await using var context = CreateContext(actor);
        var setup = await SeedAsync(context, actor);
        var runtime = new FormulaResponseRuntimeService(context, new RuntimeFormulaClient());
        var revision = await context.FormRevisions.SingleAsync();
        revision.Status = FormRevisionStatus.Retired;
        revision.RetiredAt = DateTime.UtcNow;
        context.FormRevisionAudits.Add(FormAudit(revision,
            FormRevisionStatus.Approved, FormRevisionStatus.Retired, actor));
        await context.SaveChangesAsync();

        var submission = await new FormulaSubmissionService(
                context, runtime, new RuntimeFormulaClient())
            .FinalizeAsync(setup.ResponseId, actor);
        Assert.True(submission.IsSuccess);
        Assert.Single(await context.ResponseFormulaSubmissionSets.ToListAsync());
        Assert.Null(await FormRevisionSelection.EffectiveIdAsync(context, revision.FormId));
        Assert.True((await FormRevisionSelection.ForNewResponseAsync(
            context, revision.FormId)).IsFailure);
    }

    [Fact]
    public async Task RequiredFields_UsePinnedRevisionNotLiveTemplate()
    {
        var actor = Guid.NewGuid();
        await using var context = CreateContext(actor);
        var setup = await SeedAsync(context, actor);
        var response = await context.Responses.Include(item => item.FormResponses).SingleAsync();
        (await context.FormFields.SingleAsync()).Required = true;
        context.FormResponses.RemoveRange(response.FormResponses.ToList());
        await context.SaveChangesAsync();

        Assert.Empty(await FormRevisionSelection.MissingRequiredFieldsAsync(context, response));
        Assert.False((await context.FormFieldRevisions.SingleAsync()).Required);
        Assert.Equal(setup.ResponseId, response.Id);
    }
}

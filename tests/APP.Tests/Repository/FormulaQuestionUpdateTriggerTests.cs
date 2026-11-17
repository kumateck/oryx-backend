using System.Text.Json;
using API.Controllers;
using APP.Services.Formulas;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Forms.Request;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file sealed class UpdateTestUser(Guid userId) : ICurrentUserService
{
    public Guid? UserId => userId;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

file sealed class PassingFormulaClient : IFormulaCalculationClient
{
    public Task<Result<FormulaServiceResponse>> ValidateAsync(JsonElement request, CancellationToken cancellationToken = default)
    {
        var hash = request.GetProperty("definitionHash").GetString();
        var results = JsonSerializer.SerializeToElement(new { result = "1.00" });
        int[] categories = [0, 1, 2, 3, 5];
        var outcomes = categories.Select((category, index) => new FormulaServiceTestOutcome(
            $"case-{index}", $"Case {index}", category, true,
            category == 2 ? 1 : category == 3 ? 2 : 0, category == 2 ? 1 : category == 3 ? 2 : 0,
            results, results, [])).ToList();
        return Task.FromResult(Result.Success(new FormulaServiceResponse(
            "oryx-formula-service-response-v1", 0, "test", 5, "COMPLETED",
            "oryx-formula-evaluator-v1", new string('a', 64), hash, hash,
            null, null, null, null, null, [], [], [], [], null, outcomes)));
    }

    public Task<Result<FormulaServiceResponse>> EvaluateAsync(JsonElement request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Result<bool>> IsReadyAsync(CancellationToken cancellationToken = default) => Task.FromResult(Result.Success(true));
}

// PUT /api/v1/formula-questions/{id} was reported failing with 55000 from the revision status-audit trigger.
// Editing never changes a revision status: create, update, and a new draft over an approved revision all commit.
public sealed class FormulaQuestionUpdateTriggerTests
{
    [RepairPostgresFact]
    public async Task Editing_a_formula_question_commits_under_the_revision_audit_triggers_in_every_revision_state()
    {
        var authorId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        await using var database = new FormulaTriggerDatabase();
        await database.CreateAsync(new UpdateTestUser(authorId), authorId, reviewerId, approverId);
        await using var context = database.Open(new UpdateTestUser(authorId));
        var question = new Question { Id = Guid.NewGuid(), Label = "Assay", Type = QuestionType.Formula, Validation = QuestionValidationType.None };
        context.Add(question);
        await context.SaveChangesAsync();
        var service = new FormulaDefinitionService(context, new PassingFormulaClient());
        var controller = new FormulaQuestionController(context, null!, service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.HttpContext.Items["Sub"] = authorId.ToString();
        var request = new SaveGovernedFormulaQuestionRequest(
            new CreateQuestionRequest { Label = "Assay", Type = QuestionType.Formula }, DraftRequest());

        Ok(await controller.Update(question.Id, request, CancellationToken.None));
        var draft = await context.FormulaRevisions.AsNoTracking().SingleAsync();
        var transition = new FormulaRevisionTransitionRequest(request.Formula.DefinitionHash, "Validated against the controlled golden corpus.");
        await GrantReview(context, reviewerId);
        Assert.True((await service.SubmitForReviewAsync(draft.Id, transition, authorId, Guid.NewGuid())).IsSuccess);
        Assert.True((await service.RecordReviewAsync(draft.Id, transition, reviewerId, Guid.NewGuid())).IsSuccess);
        var approved = await service.ApproveAsync(draft.Id, transition, approverId, Guid.NewGuid());
        Assert.True(approved.IsSuccess, string.Join("; ", approved.Errors.Select(e => e.Description)));
        context.ChangeTracker.Clear();

        Ok(await controller.Update(question.Id, request, CancellationToken.None));
        Ok(await controller.Update(question.Id, request, CancellationToken.None));
    }

    private static void Ok(IResult result) =>
        Assert.True(result is not Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult, (result as Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult)?.ProblemDetails.Detail);

    private static async Task GrantReview(ApplicationDbContext context, Guid user)
    {
        var role = new DOMAIN.Entities.Roles.Role { Id = Guid.NewGuid(), Name = $"Formula reviewer {user:N}" };
        var claim = new Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>
            { RoleId = role.Id, ClaimType = APP.Utils.AppConstants.Permission, ClaimValue = APP.Utils.PermissionKeys.CanReviewFormulaRevision };
        context.Roles.Add(role);
        context.UserRoles.Add(new Microsoft.AspNetCore.Identity.IdentityUserRole<Guid> { UserId = user, RoleId = role.Id });
        context.RoleClaims.Add(claim);
        await context.SaveChangesAsync();
        context.PermissionTypes.Add(new DOMAIN.Entities.Permissions.PermissionType { Id = Guid.NewGuid(), RoleClaimId = claim.Id, Key = claim.ClaimValue, Type = "Access" });
        await context.SaveChangesAsync();
    }

    internal static FormulaRevisionDraftRequest DraftRequest()
    {
        using var definition = JsonDocument.Parse(
            "{\"formulaLanguageVersion\":\"oryx-formula-v1\",\"numericPolicyVersion\":\"oryx-decimal-v1-approved\",\"results\":[],\"variables\":[]}");
        using var tests = JsonDocument.Parse("[]");
        var hash = FormulaCanonicalJson.HashJson("oryx:formula-definition:v1", definition.RootElement.GetRawText(), 1_048_576);
        return new FormulaRevisionDraftRequest(hash, "oryx-formula-v1", "oryx-decimal-v1-approved", "Simple",
            definition.RootElement.Clone(), tests.RootElement.Clone(), "{\"type\":\"Universal\",\"expression\":\":x+1\"}");
    }
}

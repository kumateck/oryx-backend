using System.Text.Json;
using APP.Services.Formulas;
using APP.Services.Approvals;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file sealed class DefinitionTestUser(Guid userId) : ICurrentUserService
{
    public Guid? UserId => userId;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

file sealed class ValidFormulaClient(bool testsPass = true) : IFormulaCalculationClient
{
    public Task<Result<FormulaServiceResponse>> ValidateAsync(
        JsonElement request, CancellationToken cancellationToken = default)
    {
        var hash = request.GetProperty("definitionHash").GetString();
        var results = JsonSerializer.SerializeToElement(new { result = "1.00" });
        int[] categories = [0, 1, 2, 3, 5];
        var outcomes = categories.Select((category, index) =>
            new FormulaServiceTestOutcome(
                $"case-{index}", $"Case {index}", category, testsPass,
                category == 2 ? 1 : category == 3 ? 2 : 0,
                category == 2 ? 1 : category == 3 ? 2 : 0,
                results, results, [])).ToList();
        var response = new FormulaServiceResponse(
            "oryx-formula-service-response-v1", 0, "test", 5, "COMPLETED",
            "oryx-formula-evaluator-v1", new string('a', 64), hash, hash,
            null, null, null, null, null, [], [], [], [], null, outcomes);
        return Task.FromResult(Result.Success(response));
    }

    public Task<Result<FormulaServiceResponse>> EvaluateAsync(
        JsonElement request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Result<bool>> IsReadyAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success(true));
}

public sealed class FormulaDefinitionServiceTests
{
    [Theory]
    [InlineData(0, 1)] // Author also reviews; another user approves.
    [InlineData(1, 0)] // Author also approves; another user reviews.
    [InlineData(1, 1)] // Another user both reviews and approves.
    [InlineData(1, 2)] // Three separate users remain supported.
    public Task Lifecycle_AllowsTwoRolesAndSupersedesEffectiveRevision(int reviewer, int approver) =>
        Lifecycle(postgres: false, reviewer, approver);

    /// <summary>Approval commits inside its own transaction; the deferred audit trigger must accept it.</summary>
    [RepairPostgresFact]
    public Task Lifecycle_passes_the_database_audit_triggers_including_approval_and_supersession() => Lifecycle(postgres: true);

    private static async Task Lifecycle(bool postgres, int reviewer = 1, int approver = 2)
    {
        var authorId = Guid.NewGuid();
        Guid[] participants = [authorId, Guid.NewGuid(), Guid.NewGuid()];
        var reviewerId = participants[reviewer];
        var approverId = participants[approver];
        await using var database = new FormulaTriggerDatabase();
        if (postgres) await database.CreateAsync(new DefinitionTestUser(authorId), authorId, reviewerId, approverId);
        await using var context = postgres ? database.Open(new DefinitionTestUser(authorId)) : CreateContext(authorId);
        var question = FormulaQuestion();
        var option = new QuestionOption
        {
            Id = Guid.NewGuid(), QuestionId = question.Id,
            Name = "{\"type\":\"0\",\"expression\":\":x+1\"}"
        };
        context.AddRange(question, option);
        await context.SaveChangesAsync();
        var service = new FormulaDefinitionService(context, new ValidFormulaClient());
        var request = DraftRequest();

        var created = await service.CreateDraftAsync(
            question.Id, request, authorId, Guid.NewGuid());
        Assert.True(created.IsSuccess);
        Assert.Equal("{\"type\":\"Universal\",\"expression\":\":x+1\"}",
            created.Value.AuthoringPayload);
        Assert.Equal(64, created.Value.AuthoringPayloadHash?.Length);
        Assert.Single(await context.QuestionOptions.Where(item => item.QuestionId == question.Id)
            .ToListAsync());

        var transition = new FormulaRevisionTransitionRequest(
            request.DefinitionHash, "Validated against the controlled golden corpus.");
        // Recording a review re-checks the reviewer's stored key (it is the revision's document review).
        await GrantReview(context, reviewerId);
        Assert.True((await service.SubmitForReviewAsync(
            created.Value.Id, transition, authorId, Guid.NewGuid())).IsSuccess);
        Assert.True((await service.RecordReviewAsync(created.Value.Id, transition,
            reviewerId, Guid.NewGuid())).IsSuccess);

        var approved = await service.ApproveAsync(
            created.Value.Id, transition, approverId, Guid.NewGuid());
        Assert.True(approved.IsSuccess);
        Assert.Equal(FormulaRevisionStatus.Approved.ToString(), approved.Value.StatusName);
        Assert.Equal(authorId, approved.Value.CreatedById);
        Assert.Equal(reviewerId, approved.Value.ReviewedById);
        Assert.Equal(approverId, approved.Value.ApprovedById);
        Assert.Equal(4, await context.FormulaRevisionAudits.CountAsync());
        Assert.Contains(await context.FormulaRevisionAudits.ToListAsync(),
            item => item.Action == "ReviewCompleted" && item.ActorId == reviewerId);
        Assert.Contains(await context.FormulaRevisionAudits.ToListAsync(),
            item => item.Action == "Approved" && item.ActorId == approverId);

        var library = await FormulaQuestionPreviewTests.Repository(context).GetQuestions(new QuestionFilter());
        Assert.Equal(approved.Value.AuthoringPayload, Assert.Single(Assert.Single(library.Value.Data).Options).Name);
        Assert.Equal(option.Name, (await context.QuestionOptions.SingleAsync()).Name);

        var replacement = await service.CreateDraftAsync(
            question.Id, request, authorId, Guid.NewGuid());
        var withDraft = await FormulaQuestionPreviewTests.Repository(context).GetQuestions(new QuestionFilter());
        Assert.Equal(approved.Value.AuthoringPayload, Assert.Single(Assert.Single(withDraft.Value.Data).Options).Name);
        var replacementTransition = new FormulaRevisionTransitionRequest(
            request.DefinitionHash, "Approve a controlled replacement revision.");
        Assert.True((await service.SubmitForReviewAsync(replacement.Value.Id,
            replacementTransition, authorId, Guid.NewGuid())).IsSuccess);
        Assert.True((await service.RecordReviewAsync(replacement.Value.Id,
            replacementTransition, reviewerId, Guid.NewGuid())).IsSuccess);
        var replaced = await service.ApproveAsync(replacement.Value.Id,
            replacementTransition, approverId, Guid.NewGuid());
        Assert.True(replaced.IsSuccess, string.Join("; ", replaced.Errors.Select(error => $"{error.Code}: {error.Description}")));

        var original = await context.FormulaRevisions.SingleAsync(
            item => item.Id == approved.Value.Id);
        Assert.Equal(FormulaRevisionStatus.Retired, original.Status);
        Assert.NotNull(original.RetiredAt);
        Assert.Single(await context.FormulaRevisions.Where(item =>
            item.FormulaDefinitionId == original.FormulaDefinitionId &&
            item.Status == FormulaRevisionStatus.Approved).ToListAsync());
        Assert.Contains(await context.FormulaRevisionAudits.ToListAsync(),
            item => item.FormulaRevisionId == original.Id && item.Action == "Superseded");
    }

    [Fact]
    public async Task Approval_RequiresReviewAndRefusesOneUserInAllThreeRoles()
    {
        var authorId = Guid.NewGuid();
        await using var context = CreateContext(authorId);
        var question = FormulaQuestion();
        context.Questions.Add(question);
        await context.SaveChangesAsync();
        var service = new FormulaDefinitionService(context, new ValidFormulaClient());
        var request = DraftRequest();
        var created = await service.CreateDraftAsync(
            question.Id, request, authorId, Guid.NewGuid());
        var transition = new FormulaRevisionTransitionRequest(
            request.DefinitionHash, "Reviewed against the controlled formula definition.");
        await service.SubmitForReviewAsync(created.Value.Id, transition, authorId, Guid.NewGuid());
        await GrantReview(context, authorId);

        var beforeReview = await service.ApproveAsync(
            created.Value.Id, transition, Guid.NewGuid(), Guid.NewGuid());
        Assert.Equal("FormulaDefinition.SegregationOfDuties", Assert.Single(beforeReview.Errors).Code);
        Assert.True((await service.RecordReviewAsync(
            created.Value.Id, transition, authorId, Guid.NewGuid())).IsSuccess);
        var refused = await service.ApproveAsync(
            created.Value.Id, transition, authorId, Guid.NewGuid());
        Assert.Equal("FormulaDefinition.SegregationOfDuties", Assert.Single(refused.Errors).Code);
        Assert.DoesNotContain(await context.FormulaRevisionAudits.ToListAsync(),
            item => item.Action == "Approved");
        Assert.Equal(FormulaRevisionStatus.InReview,
            (await context.FormulaRevisions.SingleAsync()).Status);

        var stale = await service.ApproveAsync(created.Value.Id,
            transition with { ExpectedDefinitionHash = new string('f', 64) },
            Guid.NewGuid(), Guid.NewGuid());
        Assert.Equal("FormulaDefinition.Conflict", Assert.Single(stale.Errors).Code);
        var approved = await service.ApproveAsync(
            created.Value.Id, transition, Guid.NewGuid(), Guid.NewGuid());
        Assert.True(approved.IsSuccess);
    }

    [Theory]
    [InlineData(0, 1, true)]
    [InlineData(1, 0, true)]
    [InlineData(1, 1, true)]
    [InlineData(1, 2, true)]
    [InlineData(0, 0, false)]
    public async Task PendingQueue_UsesTwoUserRuleAndPreservesAssignmentsAndPermissions(
        int reviewer, int approver, bool eligible)
    {
        Guid[] actors = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];
        await using var context = CreateContext(actors[0]);
        var question = FormulaQuestion();
        context.Questions.Add(question);
        context.Approvals.Add(new Approval
        {
            Id = Guid.NewGuid(), ItemType = "FormulaRevision",
            ApprovalStages = [
                new() { Id = Guid.NewGuid(), Order = 1, Required = true, UserId = actors[reviewer] },
                new() { Id = Guid.NewGuid(), Order = 2, Required = true, UserId = actors[approver] }
            ]
        });
        await context.SaveChangesAsync();
        var service = new FormulaDefinitionService(context, new ValidFormulaClient());
        var draft = DraftRequest();
        var created = await service.CreateDraftAsync(question.Id, draft, actors[0], Guid.NewGuid());
        var transition = new FormulaRevisionTransitionRequest(draft.DefinitionHash, "Checked the controlled formula.");
        await service.SubmitForReviewAsync(created.Value.Id, transition, actors[0], Guid.NewGuid());
        var permissions = new PendingRevisionPermissions(true, true, false, false, false, false, false);
        // A revision waiting for review is listed in Pending reviews (its document review), not in this feed.
        Assert.Empty(await PendingRevisionApprovals.GetAsync(context, actors[reviewer], [], permissions, default));
        Assert.Equal(DOMAIN.Entities.Reviews.DocumentReviewStatus.AwaitingReview,
            (await context.DocumentReviews.SingleAsync(item => item.ModelId == created.Value.Id)).Status);

        await GrantReview(context, actors[reviewer]);
        await service.RecordReviewAsync(created.Value.Id, transition, actors[reviewer], Guid.NewGuid());
        var approvalQueue = await PendingRevisionApprovals.GetAsync(
            context, actors[approver], [], permissions, default);
        Assert.Equal(eligible, approvalQueue.Count == 1);
        if (eligible) Assert.Equal("approve", Assert.Single(approvalQueue).Action);
        Assert.Empty(await PendingRevisionApprovals.GetAsync(
            context, Guid.NewGuid(), [], permissions, default));
        Assert.Empty(await PendingRevisionApprovals.GetAsync(context, actors[approver], [],
            permissions with { FormulaReview = false, FormulaApprove = false }, default));
    }

    [Fact]
    public async Task Definition_IsNotEditableAfterReviewStarts()
    {
        var authorId = Guid.NewGuid();
        await using var context = CreateContext(authorId);
        var question = FormulaQuestion();
        context.Questions.Add(question);
        await context.SaveChangesAsync();
        var service = new FormulaDefinitionService(context, new ValidFormulaClient());
        var request = DraftRequest();
        var created = await service.CreateDraftAsync(
            question.Id, request, authorId, Guid.NewGuid());
        var transition = new FormulaRevisionTransitionRequest(
            request.DefinitionHash, "Validated against the controlled golden corpus.");
        await service.SubmitForReviewAsync(
            created.Value.Id, transition, authorId, Guid.NewGuid());

        var updated = await service.UpdateDraftAsync(
            created.Value.Id, request, authorId, Guid.NewGuid());

        Assert.True(updated.IsFailure);
        Assert.Equal("FormulaDefinition.RevisionInReview", Assert.Single(updated.Errors).Code);
    }

    [Fact]
    public async Task AuthoritativeValidation_AppendsEngineAndOutcomeEvidence()
    {
        var authorId = Guid.NewGuid();
        await using var context = CreateContext(authorId);
        var question = FormulaQuestion();
        context.Questions.Add(question);
        await context.SaveChangesAsync();
        var service = new FormulaDefinitionService(context, new ValidFormulaClient());
        var created = await service.CreateDraftAsync(
            question.Id, DraftRequest(), authorId, Guid.NewGuid());

        var validation = await service.ValidateAsync(
            created.Value.Id, authorId, Guid.NewGuid());

        Assert.True(validation.IsSuccess);
        Assert.All(validation.Value.Validation.TestOutcomes,
            outcome => Assert.True(outcome.Passed));
        var audit = await context.FormulaRevisionAudits.SingleAsync(item =>
            item.FormulaRevisionId == created.Value.Id &&
            item.Action == "ValidationExecuted");
        Assert.Contains("Tests=5/5", audit.Reason);
        Assert.Contains("Engine=", audit.Reason);
    }

    [Fact]
    public async Task SubmitForReview_IsBlockedWhenAuthoritativeTestFails()
    {
        var authorId = Guid.NewGuid();
        await using var context = CreateContext(authorId);
        var question = FormulaQuestion();
        context.Questions.Add(question);
        await context.SaveChangesAsync();
        var service = new FormulaDefinitionService(
            context, new ValidFormulaClient(testsPass: false));
        var request = DraftRequest();
        var created = await service.CreateDraftAsync(
            question.Id, request, authorId, Guid.NewGuid());

        var result = await service.SubmitForReviewAsync(
            created.Value.Id,
            new FormulaRevisionTransitionRequest(
                request.DefinitionHash, "Attempt release after failed formula tests."),
            authorId, Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("FormulaDefinition.ValidationFailed",
            Assert.Single(result.Errors).Code);
    }

    private static ApplicationDbContext CreateContext(Guid userId) => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new DefinitionTestUser(userId));

    private static Question FormulaQuestion() => new()
    {
        Id = Guid.NewGuid(), Label = "Assay", Type = QuestionType.Formula,
        Validation = QuestionValidationType.None
    };

    private static FormulaRevisionDraftRequest DraftRequest()
    {
        using var definition = JsonDocument.Parse(
            "{\"formulaLanguageVersion\":\"oryx-formula-v1\",\"numericPolicyVersion\":\"oryx-decimal-v1-approved\",\"results\":[],\"variables\":[]}");
        using var tests = JsonDocument.Parse("[]");
        var hash = FormulaCanonicalJson.HashJson(
            "oryx:formula-definition:v1", definition.RootElement.GetRawText(), 1_048_576);
        return new FormulaRevisionDraftRequest(hash, "oryx-formula-v1",
            "oryx-decimal-v1-approved", "Simple", definition.RootElement.Clone(),
            tests.RootElement.Clone(),
            "{\"type\":\"Universal\",\"expression\":\":x+1\"}");
    }

    [Fact]
    public async Task A_reviewer_returns_a_revision_to_draft_and_the_author_submits_it_again()
    {
        var authorId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        await using var context = CreateContext(authorId);
        var question = FormulaQuestion();
        context.Questions.Add(question);
        context.ReviewConfigurations.Add(new DOMAIN.Entities.Reviews.ReviewConfiguration { Id = Guid.NewGuid(), ItemType = "FormulaRevision",
            Assignees = [new DOMAIN.Entities.Reviews.ReviewConfigurationAssignee { Id = Guid.NewGuid(), UserId = reviewerId }] });
        await context.SaveChangesAsync();
        await GrantReview(context, reviewerId, APP.Utils.PermissionKeys.CanReturnFormulaRevision, APP.Utils.PermissionKeys.CanViewReviewFormulaRevision);
        await GrantReview(context, outsiderId);
        var service = new FormulaDefinitionService(context, new ValidFormulaClient());
        var reviews = new APP.Services.Reviews.DocumentReviewService(context, null!, null!);
        var draft = DraftRequest();
        var created = await service.CreateDraftAsync(question.Id, draft, authorId, Guid.NewGuid());
        var transition = new FormulaRevisionTransitionRequest(draft.DefinitionHash, "Checked the controlled formula.");
        await service.SubmitForReviewAsync(created.Value.Id, transition, authorId, Guid.NewGuid());

        // Only a named reviewer (or the author) may record the review; the generic confirm is refused for formulas.
        Assert.Equal("DocumentReview.NotReviewer", Assert.Single((await service.RecordReviewAsync(created.Value.Id, transition, outsiderId, Guid.NewGuid())).Errors).Code);
        Assert.Equal("DocumentReview.FormulaRecord", (await reviews.Confirm("FormulaRevision", created.Value.Id, new(), reviewerId)).Errors[0].Code);
        var state = (await reviews.GetState("FormulaRevision", created.Value.Id, reviewerId)).Value;
        Assert.False(state.CanConfirm);
        Assert.True(state.CanReturn);

        Assert.True((await reviews.Return("FormulaRevision", created.Value.Id, new() { Comments = "Rounding must be to 2 decimals" }, reviewerId)).IsSuccess);
        var revision = await context.FormulaRevisions.AsNoTracking().SingleAsync();
        Assert.Equal(FormulaRevisionStatus.Draft, revision.Status);
        Assert.Contains(await context.FormulaRevisionAudits.ToListAsync(), item => item.Action == "ReturnedForCorrection"
            && item.PriorStatus == FormulaRevisionStatus.InReview && item.NewStatus == FormulaRevisionStatus.Draft && item.ActorId == reviewerId);
        Assert.Equal("DocumentReview.FormulaSubmit", (await reviews.Resubmit("FormulaRevision", created.Value.Id, new(), authorId)).Errors[0].Code);

        context.ChangeTracker.Clear();
        Assert.True((await service.SubmitForReviewAsync(created.Value.Id, transition, authorId, Guid.NewGuid())).IsSuccess);
        Assert.Equal(DOMAIN.Entities.Reviews.DocumentReviewStatus.AwaitingReview, (await context.DocumentReviews.AsNoTracking().SingleAsync()).Status);
        Assert.True((await service.RecordReviewAsync(created.Value.Id, transition, reviewerId, Guid.NewGuid())).IsSuccess);
        var confirmed = await context.DocumentReviews.AsNoTracking().SingleAsync();
        Assert.Equal(DOMAIN.Entities.Reviews.DocumentReviewStatus.Confirmed, confirmed.Status);
        Assert.Equal(reviewerId, confirmed.ReviewedById);
    }

    /// <summary>Gives <paramref name="user"/> a role holding the formula review key.</summary>
    private static async Task GrantReview(ApplicationDbContext context, Guid user, params string[] more)
    {
        if (!await context.Users.AnyAsync(item => item.Id == user))
            context.Users.Add(new DOMAIN.Entities.Users.User { Id = user, UserName = $"reviewer-{user:N}", FirstName = "Formula", LastName = "Reviewer",
                Email = $"reviewer-{user:N}@example.test", CreatedAt = DateTime.UtcNow });
        var role = new DOMAIN.Entities.Roles.Role { Id = Guid.NewGuid(), Name = $"Formula reviewer {user:N}" };
        var claim = new Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>
            { RoleId = role.Id, ClaimType = APP.Utils.AppConstants.Permission, ClaimValue = APP.Utils.PermissionKeys.CanReviewFormulaRevision };
        context.Roles.Add(role);
        context.UserRoles.Add(new Microsoft.AspNetCore.Identity.IdentityUserRole<Guid> { UserId = user, RoleId = role.Id });
        context.RoleClaims.Add(claim);
        await context.SaveChangesAsync();
        context.PermissionTypes.Add(new DOMAIN.Entities.Permissions.PermissionType
            { Id = Guid.NewGuid(), RoleClaimId = claim.Id, Key = claim.ClaimValue, Type = "Access" });
        foreach (var key in more)
        {
            var extra = new Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid> { RoleId = role.Id, ClaimType = APP.Utils.AppConstants.Permission, ClaimValue = key };
            context.RoleClaims.Add(extra);
            await context.SaveChangesAsync();
            context.PermissionTypes.Add(new DOMAIN.Entities.Permissions.PermissionType { Id = Guid.NewGuid(), RoleClaimId = extra.Id, Key = key, Type = "Access" });
        }
        await context.SaveChangesAsync();
    }
}

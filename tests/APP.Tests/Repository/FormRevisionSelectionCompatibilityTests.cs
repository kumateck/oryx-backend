using System.Text.Json;
using APP.Repository;
using APP.Services.Formulas;
using APP.Utils;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Forms.Request;
using DOMAIN.Entities.Formulas;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file sealed class RevisionSelectionCurrentUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public sealed class FormRevisionSelectionCompatibilityTests
{
    [Fact]
    public async Task LegacyFormulaInForm_DoesNotBlockOrdinaryDraftAnswer()
    {
        await using var context = CreateContext();
        var setup = CreateMixedForm();
        context.AddRange(setup.Form, setup.Section, setup.ShortAnswerField,
            setup.LegacyFormulaField);
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).SaveFormResponseDraft(
            RequestFor(setup.Form.Id, setup.ShortAnswerField.Id), Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Single(context.FormResponses);
        Assert.Null(Assert.Single(context.Responses).FormRevisionId);
    }

    [Fact]
    public async Task LinkedQuestionWithoutFormRevision_UsesLegacyDraftAndFinalization()
    {
        await using var context = CreateContext();
        await VerifyLegacyDraftAsync(context, Guid.NewGuid());
    }

    [RepairPostgresFact]
    public async Task Published_question_without_a_form_revision_does_not_require_worksheet_approval()
    {
        var actorId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var user = new RevisionSelectionCurrentUser();
        await using var database = new FormulaTriggerDatabase();
        await database.CreateAsync(user, actorId, approverId);
        await using var context = database.Open(user);
        var (draftId, formId, _, formulaFieldId) = await VerifyLegacyDraftAsync(context, actorId);
        await ApproveFormulaAsync(context, actorId, approverId);
        Assert.Empty(context.FormRevisions);
        var request = RequestFor(formId, formulaFieldId);
        request.ResponseId = draftId;
        var result = await CreateRepository(context).SaveFormResponseDraft(request, actorId);
        Assert.True(result.IsSuccess);
        Assert.Null((await context.Responses.SingleAsync()).FormRevisionId);
    }

    [Fact]
    public async Task Explicit_formula_placement_still_requires_an_approved_form_revision()
    {
        await using var context = CreateContext();
        var setup = CreateMixedForm();
        context.AddRange(setup.Form, setup.Section, setup.ShortAnswerField, setup.LegacyFormulaField);
        context.FormFieldFormulaConfigurations.Add(new FormFieldFormulaConfiguration
        {
            Id = Guid.NewGuid(), BindingsJson = "[]", ResultTargetsJson = "[]", DisplayPolicyJson = "{}",
            ConfigurationHash = new string('a', 64), MethodReference = "assay",
            FormFieldRevision = new FormFieldRevision
            {
                Id = Guid.NewGuid(), FormFieldId = setup.LegacyFormulaField.Id, QuestionId = setup.LegacyFormulaField.QuestionId,
                PlacementKey = "assay", FieldHash = new string('b', 64),
                FormRevision = new FormRevision { Id = Guid.NewGuid(), FormId = setup.Form.Id,
                    Status = FormRevisionStatus.Draft, ContentHash = new string('c', 64) }
            }
        });
        await context.SaveChangesAsync();
        var result = await CreateRepository(context).SaveFormResponseDraft(
            RequestFor(setup.Form.Id, setup.LegacyFormulaField.Id), Guid.NewGuid());
        Assert.Equal("FormulaRuntime.ConfigurationUnavailable", result.Error.Code);
        Assert.Empty(context.Responses);
    }

    [RepairPostgresFact]
    public async Task GovernedWorksheet_PreservesDraftWhenApprovalBecomesAvailable_Postgres()
    {
        var actorId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var user = new RevisionSelectionCurrentUser();
        await using var database = new FormulaTriggerDatabase();
        await database.CreateAsync(user, actorId, reviewerId, approverId);
        await using var context = database.Open(user);
        var (draftId, formId, textFieldId, formulaFieldId) = await VerifyLegacyDraftAsync(context, actorId);
        var formulaRevisionId = await ApproveFormulaAsync(context, actorId, approverId);
        var revisions = new FormRevisionService(context, new RuntimeFormulaClient());
        var placement = new FormulaPlacementDraftRequest(formulaFieldId, "assay", formulaRevisionId,
            new string('c', 64), JsonSerializer.SerializeToElement(Array.Empty<object>()),
            JsonSerializer.SerializeToElement(Array.Empty<object>()),
            JsonSerializer.SerializeToElement(new { }), null);
        var revision = (await revisions.CreateDraftAsync(formId,
            new FormRevisionDraftRequest([placement]), actorId, Guid.NewGuid())).Value;
        var transition = new FormRevisionTransitionRequest(revision.ContentHash, "Approve worksheet test revision.");
        Assert.True((await revisions.SubmitForReviewAsync(revision.Id, transition, actorId, Guid.NewGuid())).IsSuccess);
        Assert.True((await revisions.RecordReviewAsync(revision.Id, transition, reviewerId, Guid.NewGuid())).IsSuccess);
        Assert.True((await revisions.ApproveAsync(revision.Id, transition, approverId, Guid.NewGuid())).IsSuccess);

        async Task<Guid> SaveAsync(Guid fieldId, string value)
        {
            await using var db = database.Open(user);
            var request = RequestFor(formId, fieldId);
            request.ResponseId = draftId;
            request.Value = value;
            var result = await CreateRepository(db).SaveFormResponseDraft(request, actorId);
            Assert.True(result.IsSuccess, string.Join(", ", result.Errors.Select(error => error.Description)));
            return result.Value;
        }

        const string formulaReadings = """{"a":1,"b":2}""";
        var savedIds = await Task.WhenAll(SaveAsync(textFieldId, "complies"), SaveAsync(formulaFieldId, formulaReadings));
        Assert.All(savedIds, id => Assert.Equal(draftId, id));
        context.ChangeTracker.Clear();
        Assert.Equal(revision.Id, (await context.Responses.SingleAsync()).FormRevisionId);
        var fieldRevisions = await context.FormFieldRevisions.Where(field => field.FormRevisionId == revision.Id)
            .ToDictionaryAsync(field => field.FormFieldId, field => field.Id);
        var answers = await context.FormResponses.ToDictionaryAsync(answer => answer.FormFieldId);
        Assert.Equal(2, answers.Count);
        Assert.Equal(fieldRevisions[textFieldId], answers[textFieldId].FormFieldRevisionId);
        Assert.Equal(fieldRevisions[formulaFieldId], answers[formulaFieldId].FormFieldRevisionId);
        Assert.Equal("complies", answers[textFieldId].Value);
        Assert.Equal(formulaReadings, answers[formulaFieldId].Value);
    }

    private static async Task<(Guid DraftId, Guid FormId, Guid TextFieldId, Guid FormulaFieldId)> VerifyLegacyDraftAsync(
        ApplicationDbContext context, Guid actorId)
    {
        var setup = CreateMixedForm();
        var definition = new FormulaDefinition
        {
            Id = Guid.NewGuid(), Key = "governed", Name = "Governed",
            PresentationPreset = "Universal", CreatedById = actorId
        };
        context.AddRange(setup.Form, setup.Section, setup.ShortAnswerField,
            setup.LegacyFormulaField, definition,
            new QuestionFormulaDefinition
            {
                Id = Guid.NewGuid(), QuestionId = setup.LegacyFormulaField.QuestionId,
                Question = setup.LegacyFormulaField.Question,
                FormulaDefinitionId = definition.Id, FormulaDefinition = definition,
                CreatedById = actorId
            });
        await GrantStartProductTestAsync(context, actorId);
        var repository = CreateRepository(context);
        var formulaRequest = RequestFor(setup.Form.Id, setup.LegacyFormulaField.Id);
        var formulaFirst = await repository.SaveFormResponseDraft(formulaRequest, actorId);
        Assert.True(formulaFirst.IsSuccess);
        Assert.Null((await context.Responses.SingleAsync()).FormRevisionId);

        var textRequest = RequestFor(setup.Form.Id, setup.ShortAnswerField.Id);
        textRequest.ResponseId = formulaFirst.Value;
        var draft = await repository.SaveFormResponseDraft(textRequest, actorId);
        Assert.True(draft.IsSuccess);
        formulaRequest.ResponseId = draft.Value;
        var formulaLater = await repository.SaveFormResponseDraft(formulaRequest, actorId);
        Assert.True(formulaLater.IsSuccess);

        var submission = await new FormulaSubmissionService(context, null!, null!).FinalizeAsync(draft.Value, actorId);
        Assert.True(submission.IsSuccess);
        Assert.Null(submission.Value);
        Assert.Null((await context.Responses.SingleAsync()).FormRevisionId);
        Assert.Equal("complies", (await context.FormResponses.SingleAsync(answer => answer.FormFieldId == setup.ShortAnswerField.Id)).Value);
        return (draft.Value, setup.Form.Id, setup.ShortAnswerField.Id, setup.LegacyFormulaField.Id);
    }

    private static async Task<Guid> ApproveFormulaAsync(ApplicationDbContext context, Guid actorId, Guid approverId)
    {
        var revision = new FormulaRevision
        {
            Id = Guid.NewGuid(), FormulaDefinitionId = (await context.FormulaDefinitions.SingleAsync()).Id,
            Revision = 1, Status = FormulaRevisionStatus.Draft, CreatedById = actorId,
            DefinitionJson = """{"formulaLanguageVersion":"oryx-formula-v1","numericPolicyVersion":"oryx-decimal-v1-approved","results":[],"variables":[]}""",
            TestCasesJson = "[]", DefinitionHash = new string('a', 64), ReleaseEvidenceHash = new string('b', 64),
            FormulaLanguageVersion = "oryx-formula-v1", NumericPolicyVersion = "oryx-decimal-v1-approved"
        };
        context.FormulaRevisions.Add(revision);
        await context.SaveChangesAsync();

        async Task TransitionAsync(FormulaRevisionStatus status, Guid actor)
        {
            context.FormulaRevisionAudits.Add(new FormulaRevisionAudit
            {
                Id = Guid.NewGuid(), FormulaRevisionId = revision.Id, PriorStatus = revision.Status,
                NewStatus = status, DefinitionHash = revision.DefinitionHash, Action = "Test",
                Reason = "Controlled test transition.", ActorId = actor,
                OccurredAt = DateTime.UtcNow, CorrelationId = Guid.NewGuid()
            });
            revision.Status = status;
            await context.SaveChangesAsync();
        }

        await TransitionAsync(FormulaRevisionStatus.InReview, actorId);
        revision.ApprovedById = approverId;
        revision.ApprovedAt = DateTime.UtcNow;
        revision.EffectiveAt = revision.ApprovedAt;
        await TransitionAsync(FormulaRevisionStatus.Approved, approverId);
        return revision.Id;
    }

    private static async Task GrantStartProductTestAsync(ApplicationDbContext context, Guid actorId)
    {
        if (!await context.Users.AnyAsync(user => user.Id == actorId))
            context.Users.Add(new User { Id = actorId, UserName = "analyst", Email = "analyst@example.test" });
        var role = new Role { Id = Guid.NewGuid(), Name = "Analyst", NormalizedName = "ANALYST", DisplayName = "Analyst" };
        context.Roles.Add(role);
        context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = actorId, RoleId = role.Id });
        var claim = new IdentityRoleClaim<Guid>
        {
            RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = PermissionKeys.CanStartProductTest
        };
        context.RoleClaims.Add(claim);
        await context.SaveChangesAsync();
        context.PermissionTypes.Add(new PermissionType
        {
            RoleClaimId = claim.Id, Key = claim.ClaimValue, Type = "Access"
        });
        await context.SaveChangesAsync();
    }

    private static SaveResponseDraftRequest RequestFor(Guid formId, Guid fieldId) => new()
    {
        FormId = formId,
        FormFieldId = fieldId,
        Value = "complies"
    };

    private static (Form Form, FormSection Section, FormField ShortAnswerField,
        FormField LegacyFormulaField) CreateMixedForm()
    {
        var form = new Form { Id = Guid.NewGuid(), Name = "Mixed legacy form" };
        var section = new FormSection
        {
            Id = Guid.NewGuid(), FormId = form.Id, Form = form, Name = "Results"
        };
        var shortQuestion = new Question
        {
            Id = Guid.NewGuid(), Label = "Appearance", Type = QuestionType.ShortAnswer
        };
        var formulaQuestion = new Question
        {
            Id = Guid.NewGuid(), Label = "Legacy calculation", Type = QuestionType.Formula
        };
        return (form, section,
            Field(section, shortQuestion), Field(section, formulaQuestion));
    }

    private static FormField Field(FormSection section, Question question) => new()
    {
        Id = Guid.NewGuid(), FormSectionId = section.Id, FormSection = section,
        QuestionId = question.Id, Question = question
    };

    private static FormRepository CreateRepository(ApplicationDbContext context) =>
        new(context, null!, null!, null!, new FormulaSubmissionService(context, null!, null!));

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new RevisionSelectionCurrentUser());
}

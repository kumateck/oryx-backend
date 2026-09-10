using System.Data;
using APP.Extensions;
using APP.IRepository;
using APP.Services.Formulas;
using APP.Utils;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/formula-questions")]
[Authorize]
public sealed class FormulaQuestionController(
    ApplicationDbContext context,
    IFormRepository forms,
    IFormulaDefinitionService definitions) : ControllerBase
{
    [HttpPost]
    [Authorize(PermissionKeys.CanCreateQuestions)]
    public async Task<IResult> Create(
        SaveGovernedFormulaQuestionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return TypedResults.Unauthorized();
        if (request.Question.Type != QuestionType.Formula)
            return TypedResults.BadRequest();
        var correlationId = CorrelationId();
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken)
            : null;
        var question = await forms.CreateQuestion(
            request.Question, actorId, allowGovernedFormula: true);
        if (question.IsFailure) return question.ToProblemDetails();
        var revision = await definitions.CreateDraftAsync(
            question.Value, request.Formula, actorId, correlationId, cancellationToken);
        if (revision.IsFailure) return revision.ToProblemDetails();
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        var validation = await definitions.ValidateAsync(
            revision.Value.Id, actorId, correlationId, cancellationToken);
        return TypedResults.Ok(new GovernedFormulaQuestionDto(
            question.Value, revision.Value,
            validation.IsSuccess ? validation.Value.Validation : null));
    }

    [HttpPut("{questionId:guid}")]
    [Authorize(PermissionKeys.CanEditQuestions)]
    public async Task<IResult> Update(
        Guid questionId,
        SaveGovernedFormulaQuestionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return TypedResults.Unauthorized();
        if (request.Question.Type != QuestionType.Formula)
            return TypedResults.BadRequest();
        var correlationId = CorrelationId();
        var revisions = await context.QuestionFormulaDefinitions.AsNoTracking()
            .Where(item => item.QuestionId == questionId)
            .SelectMany(item => item.FormulaDefinition.Revisions)
            .OrderByDescending(item => item.Revision).ToListAsync(cancellationToken);
        if (revisions.Any(item => item.Status == FormulaRevisionStatus.InReview))
            return SHARED.Result.Failure(FormulaDefinitionErrors.RevisionInReview)
                .ToProblemDetails();
        var draft = revisions.SingleOrDefault(item => item.Status == FormulaRevisionStatus.Draft);

        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken)
            : null;
        var question = await FormulaQuestionDraftPersistence.UpdateMetadataAsync(
            context, request.Question, questionId, actorId, cancellationToken);
        if (question.IsFailure) return question.ToProblemDetails();
        var revision = draft is null
            ? await definitions.CreateDraftAsync(
                questionId, request.Formula, actorId, correlationId, cancellationToken)
            : await definitions.UpdateDraftAsync(
                draft.Id, request.Formula, actorId, correlationId, cancellationToken);
        if (revision.IsFailure) return revision.ToProblemDetails();
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        var validation = await definitions.ValidateAsync(
            revision.Value.Id, actorId, correlationId, cancellationToken);
        return TypedResults.Ok(new GovernedFormulaQuestionDto(
            questionId, revision.Value,
            validation.IsSuccess ? validation.Value.Validation : null));
    }

    private bool TryActor(out Guid actorId) =>
        Guid.TryParse(HttpContext.Items["Sub"] as string, out actorId);

    private Guid CorrelationId() =>
        Guid.TryParse(Request.Headers["X-Correlation-ID"].FirstOrDefault(), out var value)
            ? value
            : Guid.NewGuid();
}

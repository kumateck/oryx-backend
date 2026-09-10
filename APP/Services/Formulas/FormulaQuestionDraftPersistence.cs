using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Forms.Request;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Formulas;

public static class FormulaQuestionDraftPersistence
{
    public static async Task<Result> UpdateMetadataAsync(
        ApplicationDbContext context,
        CreateQuestionRequest request,
        Guid questionId,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var question = await context.Questions.SingleOrDefaultAsync(
            item => item.Id == questionId && item.Type == QuestionType.Formula,
            cancellationToken);
        if (question is null) return FormulaDefinitionErrors.QuestionNotFound;

        question.Label = request.Label;
        question.Description = request.Description;
        question.Reference = request.Reference;
        question.Validation = request.Validation;
        question.IsMultiSelect = false;
        question.LastUpdatedById = actorId;
        question.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

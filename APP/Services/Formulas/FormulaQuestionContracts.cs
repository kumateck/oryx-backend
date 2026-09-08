using DOMAIN.Entities.Forms.Request;

#nullable enable

namespace APP.Services.Formulas;

public sealed record SaveGovernedFormulaQuestionRequest(
    CreateQuestionRequest Question,
    FormulaRevisionDraftRequest Formula);

public sealed record GovernedFormulaQuestionDto(
    Guid QuestionId,
    FormulaRevisionDto Revision,
    FormulaServiceResponse? Validation);

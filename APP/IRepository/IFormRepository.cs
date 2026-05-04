using APP.Utils;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Forms.Request;
using SHARED;

namespace APP.IRepository;

public interface IFormRepository
{
    Task<Result<Guid>> CreateForm(CreateFormRequest request);
    Task<Result<FormDto>> GetForm(Guid formId);
    Task<Result<Paginateable<IEnumerable<FormDto>>>> GetForms(FormFilter filter);
    Task<Result<Paginateable<IEnumerable<FormSectionDto>>>> GetFormSections(FormFilter filter);
    Task<Result> UpdateForm(CreateFormRequest request, Guid formId, Guid userId);

    //Task<Result> ResetForm(Guid formId, Guid userId);
    Task<Result> DeleteForm(Guid formId, Guid userId);
    Task<Result<Guid?>> GetResponseId(GetResponseIdRequest request);
    Task<Result<Guid?>> GetFormAssigneeId(GetResponseIdRequest request);
    Task<Result> SaveFormResponseDraft(SaveResponseDraftRequest request, Guid userId);
    Task<Result> SubmitFormResponseFinal(Guid responseId);
    Task<Result> SubmitFormResponse(CreateResponseRequest request, Guid userId);
    Task<Result> SubmitFormSectionValue(
        List<SubmitFormSectionValue> requests,
        Guid? materialSpecificationId,
        Guid? productSpecificationId
    );
    Task<Result> UpdateFormSectionValue(
        List<SubmitFormSectionValue> requests,
        Guid? materialSpecificationId = null,
        Guid? productSpecificationId = null
    );
    Task<Result<ResponseDetailDto>> GetFormResponse(Guid formResponseId);

    Task<Result<Guid>> CreateQuestion(CreateQuestionRequest request, Guid userId);
    Task<Result<QuestionDto>> GetQuestion(Guid questionId);
    Task<Result<Paginateable<IEnumerable<QuestionDto>>>> GetQuestions(QuestionFilter filter);
    Task<Result> UpdateQuestion(CreateQuestionRequest request, Guid id, Guid userId);
    Task<Result> DeleteQuestion(Guid id, Guid userId);
    Task<Result> GenerateCertificateOfAnalysis(
        Guid materialBatchId,
        Guid userId,
        List<CertificateOfAnalysisComplies> complies
    );
    Task<Result<IEnumerable<FormResponseDto>>> GetFormResponseByMaterialBatch(Guid materialBatchId);
    Task<Result<IEnumerable<FormResponseDto>>> GetFormResponseByBmr(
        Guid batchManufacturingRecordId
    );

    //  Task<Result<IEnumerable<FormDto>>> GetFormWithResponseByMaterialSpecification(
    //      Guid materialSpecificationId);
    // Task<Result<IEnumerable<FormDto>>> GetFormWithResponseByProductSpecification(
    //      Guid productSpecificationId);
    Task<Result<IEnumerable<FormDto>>> GetFormWithResponseByMaterialBatch(Guid materialBatchId);
    Task<Result<IEnumerable<FormDto>>> GetFormWithResponseByBmr(Guid batchManufacturingRecordId);
    Task<Result<IEnumerable<FormResponseDto>>> GetFormResponseByMaterialSpecification(
        Guid materialSpecificationId
    );
    Task<Result<IEnumerable<FormResponseDto>>> GetFormResponseByProductSpecification(
        Guid productSpecificationId
    );
    Task<Result> GenerateCertificateOfAnalysisForProduct(
        Guid batchManufacturingRecordId,
        Guid productionActivityStepId,
        Guid userId,
        List<CertificateOfAnalysisComplies> complies
    );

    /// <summary>
    /// Saves or updates a draft FormAssignee for the specified form field.
    /// If the FormAssignee does not exist, it creates one.
    /// </summary>
    /// <param name="request">The draft request data.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <returns>A Result object indicating success or failure.</returns>
    Task<Result> SaveFormAssigneeDraft(SaveFormAssigneeDraftRequest request, Guid userId);

    /// <summary>
    /// Submits and finalizes an existing FormAssignee record.
    /// Performs validation to ensure all required fields are filled before final submission.
    /// </summary>
    /// <param name="formAssigneeId">The unique identifier of the FormAssignee being finalized.</param>
    /// <returns>A Result object indicating success or failure.</returns>
    Task<Result> SubmitFormAssigneeFinal(Guid formAssigneeId);

    /// <summary>
    /// Creates and submits a new FormAssignee with all its field assignments in a single operation.
    /// </summary>
    /// <param name="request">The creation request containing form and field assignment data.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <returns>A Result object indicating success or failure.</returns>
    Task<Result> SubmitFormAssignee(CreateFormAssigneeRequest request, Guid userId);

    Task<Result<FormAssigneeDto>> GetFormAssignee(Guid formAssigneeId);
    Task<Result<FormAssigneeDto>> GetFormAssigneeByBatch(Guid materialBatchId);
    Task<Result<FormAssigneeDto>> GetFormAssigneeByBmr(Guid bmrId);
}

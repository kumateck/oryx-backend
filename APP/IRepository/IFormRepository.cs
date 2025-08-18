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
    Task<Result> SubmitFormResponse(CreateResponseRequest request, Guid userId);
    Task<Result<ResponseDto>> GetFormResponse(Guid formResponseId);

   Task<Result<Guid>> CreateQuestion(CreateQuestionRequest request, Guid userId);
   Task<Result<QuestionDto>> GetQuestion(Guid questionId);
   Task<Result<Paginateable<IEnumerable<QuestionDto>>>>
       GetQuestions(FormFilter filter);
   Task<Result> UpdateQuestion(CreateQuestionRequest request, Guid id, Guid userId);
   Task<Result> DeleteQuestion(Guid id, Guid userId);
   Task<Result> GenerateCertificateOfAnalysis(Guid materialBatchId, Guid productionActivityStepId, Guid userId);
   Task<Result<IEnumerable<FormResponseDto>>> GetFormResponseByMaterialBatch(Guid materialBatchId);
   Task<Result<IEnumerable<FormResponseDto>>> GetFormResponseByBmr(Guid batchManufacturingRecordId);
  //  Task<Result<IEnumerable<FormDto>>> GetFormWithResponseByMaterialSpecification(
  //      Guid materialSpecificationId);
  // Task<Result<IEnumerable<FormDto>>> GetFormWithResponseByProductSpecification(
  //      Guid productSpecificationId);
   Task<Result<IEnumerable<FormDto>>> GetFormWithResponseByMaterialBatch(Guid materialBatchId);
   Task<Result<IEnumerable<FormDto>>> GetFormWithResponseByBmr(Guid batchManufacturingRecordId);
   // Task<Result<IEnumerable<FormResponseDto>>> GetFormResponseByMaterialSpecification(
   //     Guid materialSpecificationId);
   // Task<Result<IEnumerable<FormResponseDto>>> GetFormResponseByProductSpecification(
   //     Guid productSpecificationId);
   Task<Result> GenerateCertificateOfAnalysisForProduct(Guid batchManufacturingRecordId, Guid productionActivityStepId, Guid userId);
}
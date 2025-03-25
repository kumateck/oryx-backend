using APP.Utils;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Forms.Request;
using SHARED;

namespace APP.IRepository;

public interface IFormRepository
{
    Task<Result<Guid>> CreateForm(CreateFormRequest request);
    Task<Result<FormDto>> GetForm(Guid formId, string userId);
    Task<Result<Paginateable<IEnumerable<FormDto>>>> GetForms(FormFilter filter, string searchQuery);
    Task<Result> UpdateForm(CreateFormRequest request, Guid formId, Guid userId);
    //Task<Result> ResetForm(Guid formId, Guid userId);
    Task<Result> DeleteForm(Guid formId, Guid userId);
    Task<Result> SubmitFormResponse(CreateResponseRequest request, Guid userId);
    Task<Result<FormResponseDto>> GetFormResponse(Guid formResponseId);

   Task<Result<Guid>> CreateQuestion(CreateQuestionRequest request, Guid userId);
   Task<Result<QuestionDto>> GetQuestion(Guid questionId);
   Task<Result<Paginateable<IEnumerable<QuestionDto>>>>
       GetQuestions(FormFilter filter, string searchQuery);
   Task<Result> UpdateQuestion(CreateQuestionRequest request, Guid id, Guid userId);
   Task<Result> DeleteQuestion(Guid id, Guid userId);
}
using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Forms.Request;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class FormRepository(ApplicationDbContext context, IMapper mapper, IFileRepository fileRepository) : IFormRepository
{
    public async Task<Result<Guid>> CreateForm(CreateFormRequest request)
    {
        var form = mapper.Map<Form>(request);

        var validate = FormValidator.Validate(form);

        if (validate.IsFailure)
            return Result.Failure<Guid>(validate.Errors);

        await context.Forms.AddAsync(form);
        await context.SaveChangesAsync();

        return form.Id;
    }

    public async Task<Result<FormDto>> GetForm(Guid formId, string userId)
    {
        var form = await context.Forms
            .FirstOrDefaultAsync(f => f.Id == formId);

        if (form == null)
            return FormErrors.NotFound(formId);

        return mapper.Map<FormDto>(form);
    }

    public async Task<Result<Paginateable<IEnumerable<FormDto>>>> GetForms(FormFilter filter)
    {
        var query = context.Forms
            .AsSplitQuery()
            .OrderByDescending(f => f.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filter.SearchQuery))
        {
            query = query.WhereSearch(filter.SearchQuery, f => f.Name, f => f.CreatedBy.FirstName, f => f.CreatedBy.LastName);
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            filter,
            mapper.Map<FormDto>
        );
    }

    public async Task<Result> UpdateForm(CreateFormRequest request, Guid formId, Guid userId)
    {
        var form = await context.Forms
            .AsSplitQuery()
            .Include(form => form.Reviewers).Include(form => form.Assignees)
            .FirstOrDefaultAsync(f => f.Id == formId);

        if (form == null)
            return FormErrors.NotFound(formId);

        context.FormSections.RemoveRange(form.Sections);
        context.FormReviewers.RemoveRange(form.Reviewers);
        context.FormAssignees.RemoveRange(form.Assignees);
        mapper.Map(request, form);

        var validate = FormValidator.Validate(form);

        if (validate.IsFailure)
            return Result.Failure<FormDto>(validate.Errors);

        form.LastUpdatedById = userId;
        form.UpdatedAt = DateTime.UtcNow;
        context.Forms.Update(form);

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteForm(Guid formId, Guid userId)
    {
        var form = await context.Forms.FirstOrDefaultAsync(f => f.Id == formId);

        if (form == null)
            return FormErrors.NotFound(formId);

        form.LastDeletedById = userId;
        form.DeletedAt = DateTime.UtcNow;
        context.Forms.Update(form);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> SubmitFormResponse(CreateResponseRequest request, Guid userId)
    {
        var newResponse = new Response
        {
            FormId = request.FormId,
            FormResponses = [],
            CreatedById = userId
        };
        
        foreach (var response in request.FormResponses)
        {
            var formField = await context.FormFields
                .AsSplitQuery()
                .Include(f => f.Question)
                .ThenInclude(q => q.Type)
                .FirstOrDefaultAsync(field => field.Id == response.FormFieldId);

            if (formField == null)
            {
                continue;
            }

            var type = formField.Question.Type;

            if (type is QuestionType.Signature or QuestionType.FileUpload)
            {
                var values = response.Value.Split("|");
                var attachments = new List<string>();
                foreach (var value in values)
                {
                    var reference = Guid.NewGuid().ToString();
                    await fileRepository.SaveBlobItem(nameof(FormField).ToLower(), formField.Id, reference,value.ConvertFromBase64(), userId);
                    attachments.Add(reference);
                }
                newResponse.FormResponses.Add(new FormResponse
                {
                    FormFieldId = formField.Id,
                    Value = string.Join("|", attachments)
                });
            }
            else
            {
                newResponse.FormResponses.Add(mapper.Map<FormResponse>(response));
            }
        }

        await context.Responses.AddAsync(newResponse);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<FormResponseDto>> GetFormResponse(Guid formResponseId)
    {
        var formResponse = await context.Responses
            .AsSplitQuery()
            .Include(fr => fr.Form)
            //.ThenInclude(fr => fr.Sections).ThenInclude(fr => fr.Fields)
            .Include(fr => fr.CreatedBy)
            .Include(fr => fr.FormResponses)
            .ThenInclude(r => r.FormField)
            //.ThenInclude(r => r.Question)
            //.ThenInclude(r => r.Options)
            .FirstOrDefaultAsync(fr => fr.Id == formResponseId);

        if (formResponse == null)
            return FormErrors.NotFound(formResponseId);

        return mapper.Map<FormResponseDto>(formResponse);
    }

    public async Task<Result<Guid>> CreateQuestion(CreateQuestionRequest request, Guid userId)
    {
        var question = mapper.Map<Question>(request);
        question.CreatedById = userId;
        
        await context.Questions.AddAsync(question);
        await context.SaveChangesAsync();
        return question.Id;
    }

    public async Task<Result<QuestionDto>> GetQuestion(Guid questionId)
    {
        return mapper.Map<QuestionDto>(await context.Questions.FirstOrDefaultAsync(q => q.Id == questionId));
    }

    public async Task<Result<Paginateable<IEnumerable<QuestionDto>>>> GetQuestions(FormFilter filter)
    {
        var query = context.Questions
            .AsSplitQuery()
            .OrderByDescending(f => f.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filter.SearchQuery))
        {
            query = query.WhereSearch(filter.SearchQuery, q => q.Label, q => q.CreatedBy.FirstName, q => q.CreatedBy.LastName);
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            filter,
            mapper.Map<QuestionDto>
        );
    }

    public async Task<Result> UpdateQuestion(CreateQuestionRequest request, Guid id, Guid userId)
    {
        var question = await context.Questions.Include(question => question.Options)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (question == null)
            return FormErrors.NotFound(id);
        
        context.QuestionOptions.RemoveRange(question.Options);
        mapper.Map(request, question);

        question.LastUpdatedById = userId;
        question.UpdatedAt = DateTime.UtcNow;
        context.Questions.Update(question);

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteQuestion(Guid id, Guid userId)
    {
        var question = await context.Questions.FirstOrDefaultAsync(q => q.Id == id);
        
        if (question == null)
            return FormErrors.NotFound(id);

        question.LastDeletedById = userId;
        question.DeletedAt = DateTime.UtcNow;
        context.Questions.Update(question);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
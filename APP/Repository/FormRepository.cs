using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Forms.Request;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class FormRepository(ApplicationDbContext context, IMapper mapper, IFileRepository fileRepository, IApprovalRepository approvalRepository) : IFormRepository
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

    public async Task<Result<FormDto>> GetForm(Guid formId)
    {
        var form = await context.Forms
            .AsSplitQuery()
            .Include(f => f.Sections)
            .ThenInclude(s => s.Fields)
            .ThenInclude(f => f.Question)
            .ThenInclude(q => q.Options)
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

        if (filter.Type.HasValue)
        {
            query = query.Where(q => q.Type == filter.Type);
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            filter,
            mapper.Map<FormDto>
        );
    }
    
    public async Task<Result<Paginateable<IEnumerable<FormSectionDto>>>> GetFormSections(FormFilter filter)
    {
        var query = context.FormSections
            .GroupBy(f => new { f.Name, f.InstrumentId })
            .Select(g => g.OrderByDescending(f => f.CreatedAt).First())
            .AsQueryable();

        if (!string.IsNullOrEmpty(filter.SearchQuery))
        {
            query = query.WhereSearch(filter.SearchQuery, f => f.Name, f => f.CreatedBy.FirstName, f => f.CreatedBy.LastName);
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            filter,
            mapper.Map<FormSectionDto>
        );
    }

    public async Task<Result> UpdateForm(CreateFormRequest request, Guid formId, Guid userId)
    {
        var form = await context.Forms
            .AsSplitQuery()
            .Include(form => form.Sections)
            .Include(form => form.Reviewers)
            .Include(form => form.Assignees)
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
            MaterialBatchId = request.MaterialBatchId,
            BatchManufacturingRecordId = request.BatchManufacturingRecordId,
            FormResponses = [],
            CreatedById = userId
        };
        
        foreach (var response in request.FormResponses)
        {
            var formField = await context.FormFields
                .AsSplitQuery()
                .Include(f => f.Question)
                .FirstOrDefaultAsync(field => field.Id == response.FormFieldId);

            if (formField == null)
            {
                return Error.Validation("Response.FormField", $"FormField not found {response.FormFieldId}");
            }

            var type = formField.Question.Type;

            if (type is QuestionType.Signature or QuestionType.FileUpload)
            {
                var values = response.Value.Split("|");
                var formResponse = new FormResponse
                {
                    FormFieldId = formField.Id,
                    Value = "form response attachment.",
                };
                newResponse.FormResponses.Add(formResponse);
                foreach (var value in values)
                {
                    var reference = Guid.NewGuid().ToString();
                    await fileRepository.SaveBlobItem(nameof(FormResponse).ToLower(), formResponse.Id, reference,value.ConvertFromBase64(), userId);
                }
            }
            else
            {
                newResponse.FormResponses.Add(mapper.Map<FormResponse>(response));
            }
        }

        await context.Responses.AddAsync(newResponse);
        
        if (request.BatchManufacturingRecordId.HasValue || request.MaterialBatchId.HasValue)
        {
            if (request.MaterialBatchId.HasValue)
            {
                var batch = await context.MaterialBatches.FirstOrDefaultAsync(b => b.Id == request.MaterialBatchId);
                batch.Status = BatchStatus.TestTaken;
                context.MaterialBatches.Update(batch);
            }
            else if (request.BatchManufacturingRecordId.HasValue)
            {
                var bmr = await context.BatchManufacturingRecords
                    .FirstOrDefaultAsync(b => b.Id == request.BatchManufacturingRecordId);
                bmr.Status = BatchManufacturingStatus.TestTaken;
                context.BatchManufacturingRecords.Update(bmr);
            }
        }

        if (request.MaterialSpecificationId.HasValue)
        {
            var materialSpecification = await context.MaterialSpecifications.FirstOrDefaultAsync(s => s.Id == request.MaterialSpecificationId);
            if(materialSpecification is null) return Error.NotFound("MaterialSpecification.NotFound", "Material specification not found");
            
            materialSpecification.ResponseId = newResponse.Id;
            context.MaterialSpecifications.Update(materialSpecification);
        }

        if (request.ProductSpecificationId.HasValue)
        {
            var productSpecification = await context.ProductSpecifications.FirstOrDefaultAsync(s => s.Id == request.ProductSpecificationId);
            if (productSpecification is null) return Error.NotFound("ProductSpecification.NotFound", "Product specification not found");
            
            productSpecification.ResponseId = newResponse.Id;
            context.ProductSpecifications.Update(productSpecification);
        }
        
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> GenerateCertificateOfAnalysis(Guid materialBatchId, Guid userId)
    {
        var response = await context.Responses.FirstOrDefaultAsync(r => r.MaterialBatchId == materialBatchId);
        if (response == null) return FormErrors.NotFound(materialBatchId);
        
        var batch = await context.MaterialBatches.FirstOrDefaultAsync(b => b.Id == response.MaterialBatchId);
        if (batch == null) return MaterialErrors.NotFound(materialBatchId);
        
        var approval = await context.Approvals.FirstOrDefaultAsync(a => a.ItemType == nameof(Response));
        if (approval == null)
            return Error.Validation("Response.Approval",
                "Approval configuration for response does not exist. Kindly create an approval in the settings.");
        
        response.CheckedAt = DateTime.UtcNow;
        response.CheckedById = userId;
        context.Responses.Update(response);
        
        batch.Status = BatchStatus.Checked;
        context.MaterialBatches.Update(batch);
        
        await approvalRepository.CreateInitialApprovalsAsync(nameof(Response), response.Id);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result> GenerateCertificateOfAnalysisForProduct(Guid batchManufacturingRecordId, Guid userId)
    {
        var response = await context.Responses.FirstOrDefaultAsync(r => r.BatchManufacturingRecordId == batchManufacturingRecordId);
        if (response == null) return FormErrors.NotFound(batchManufacturingRecordId);
        
        var bmr = await context.BatchManufacturingRecords.FirstOrDefaultAsync(b => b.Id == response.BatchManufacturingRecordId);
        if (bmr == null) return MaterialErrors.NotFound(batchManufacturingRecordId);
        
        var approval = await context.Approvals.FirstOrDefaultAsync(a => a.ItemType == nameof(Response));
        if (approval == null)
            return Error.Validation("Response.Approval",
                "Approval configuration for response does not exist. Kindly create an approval in the settings.");
        
        response.CheckedAt = DateTime.UtcNow;
        response.CheckedById = userId;
        context.Responses.Update(response);
        
        bmr.Status = BatchManufacturingStatus.Checked;
        context.BatchManufacturingRecords.Update(bmr);
        
        await approvalRepository.CreateInitialApprovalsAsync(nameof(Response), response.Id);
        await context.SaveChangesAsync();
        return Result.Success();
    }
        
    public async Task<Result<ResponseDto>> GetFormResponse(Guid formResponseId)
    {
        var formResponse = await context.Responses
            .AsSplitQuery()
            .Include(fr => fr.Form)
            .Include(fr => fr.CreatedBy)
            .Include(fr => fr.FormResponses)
            .ThenInclude(r => r.FormField)
            .ThenInclude(r => r.Question)
            .ThenInclude(r => r.Options)
            .FirstOrDefaultAsync(fr => fr.Id == formResponseId);

        if (formResponse == null)
            return FormErrors.NotFound(formResponseId);

        return mapper.Map<ResponseDto>(formResponse);
    }
    
    public async Task<Result<IEnumerable<FormDto>>> GetFormWithResponseByMaterialBatch(Guid materialBatchId)
    {
        var form = await context.Forms
            .AsSplitQuery()
            .Include(f => f.Sections)
            .ThenInclude(s => s.Fields)
            .ThenInclude(fld => fld.Question)
            .ThenInclude(q => q.Options)
            .Include(f => f.Responses)
            .ThenInclude(r => r.CreatedBy)
            .Include(f => f.Responses)
            .ThenInclude(r => r.FormField)
            .Include(f => f.Responses)
            .ThenInclude(r => r.Response)
            .ThenInclude(res => res.CheckedBy)
            .FirstOrDefaultAsync(f => f.Responses.Any(r => r.Response.MaterialBatchId == materialBatchId));

        return mapper.Map<List<FormDto>>(form, opts => opts.Items[AppConstants.ModelType]  = typeof(FormResponse));
    }
    
    public async Task<Result<IEnumerable<FormDto>>> GetFormWithResponseByBmr(Guid batchManufacturingRecordId)
    {
        var form = await context.Forms
            .AsSplitQuery()
            .Include(f => f.Sections)
            .ThenInclude(s => s.Fields)
            .ThenInclude(fld => fld.Question)
            .ThenInclude(q => q.Options)
            .Include(f => f.Responses)
            .ThenInclude(r => r.CreatedBy)
            .Include(f => f.Responses)
            .ThenInclude(r => r.FormField)
            .Include(f => f.Responses)
            .ThenInclude(r => r.Response)
            .ThenInclude(res => res.CheckedBy)
            .FirstOrDefaultAsync(f => f.Responses.Any(r => r.Response.MaterialBatchId == batchManufacturingRecordId));

        return mapper.Map<List<FormDto>>(form, opts => opts.Items[AppConstants.ModelType]  = typeof(FormResponse));
    }
    
    /*public async Task<Result<IEnumerable<FormDto>>> GetFormWithResponseByMaterialSpecification(Guid materialSpecificationId)
    {
        var form = await context.Forms
            .AsSplitQuery()
            .Include(f => f.Sections)
            .ThenInclude(s => s.Fields)
            .ThenInclude(fld => fld.Question)
            .ThenInclude(q => q.Options)
            .Include(f => f.Responses)
            .ThenInclude(r => r.CreatedBy)
            .Include(f => f.Responses)
            .ThenInclude(r => r.FormField)
            .Include(f => f.Responses)
            .ThenInclude(r => r.Response)
            .ThenInclude(res => res.CheckedBy)
            .FirstOrDefaultAsync(f => f.Responses.Any(r => r.Response.MaterialSpecificationId == materialSpecificationId));

        return mapper.Map<List<FormDto>>(form, opts => opts.Items[AppConstants.ModelType]  = typeof(FormResponse));
    }
    
    public async Task<Result<IEnumerable<FormDto>>> GetFormWithResponseByProductSpecification(Guid productSpecificationId)
    {
        var form = await context.Forms
            .AsSplitQuery()
            .Include(f => f.Sections)
            .ThenInclude(s => s.Fields)
            .ThenInclude(fld => fld.Question)
            .ThenInclude(q => q.Options)
            .Include(f => f.Responses)
            .ThenInclude(r => r.CreatedBy)
            .Include(f => f.Responses)
            .ThenInclude(r => r.FormField)
            .Include(f => f.Responses)
            .ThenInclude(r => r.Response)
            .ThenInclude(res => res.CheckedBy)
            .FirstOrDefaultAsync(f => f.Responses.Any(r => r.Response.ProductSpecificationId == productSpecificationId));

        return mapper.Map<List<FormDto>>(form, opts => opts.Items[AppConstants.ModelType]  = typeof(FormResponse));
    }*/
    
    public async Task<Result<IEnumerable<FormResponseDto>>> GetFormResponseByMaterialBatch(Guid materialBatchId)
    {
        var formResponse = await context.FormResponses
            .AsSplitQuery()
            .Include(fr => fr.CreatedBy)
            .Include(fr => fr.Response)
            .ThenInclude(fr => fr.CheckedBy)
            .Include(r => r.FormField)
            .ThenInclude(r => r.Question)
            .ThenInclude(r => r.Options)
            .Where(fr => fr.Response.MaterialBatchId == materialBatchId)
            .ToListAsync();

        return mapper.Map<List<FormResponseDto>>(formResponse, opts => opts.Items[AppConstants.ModelType]  = typeof(FormResponse));
    }
    
    public async Task<Result<IEnumerable<FormResponseDto>>> GetFormResponseByBmr(Guid batchManufacturingRecordId)
    {
        var formResponse = await context.FormResponses
            .AsSplitQuery()
            .Include(fr => fr.CreatedBy)
            .Include(fr => fr.Response)
            .ThenInclude(fr => fr.CheckedBy)
            .Include(r => r.FormField)
            .ThenInclude(r => r.Question)
            .ThenInclude(r => r.Options)
            .Where(fr => fr.Response.BatchManufacturingRecordId == batchManufacturingRecordId)
            .ToListAsync();

        return mapper.Map<List<FormResponseDto>>(formResponse, opt => opt.Items[AppConstants.ModelType]  = typeof(FormResponse));
    }
    
    /*public async Task<Result<IEnumerable<FormResponseDto>>> GetFormResponseByMaterialSpecification(Guid materialSpecificationId)
    {
        var formResponse = await context.FormResponses
            .AsSplitQuery()
            .Include(fr => fr.CreatedBy)
            .Include(fr => fr.Response)
            .ThenInclude(fr => fr.CheckedBy)
            .Include(r => r.FormField)
            .ThenInclude(r => r.Question)
            .ThenInclude(r => r.Options)
            .Where(fr => fr.Response.MaterialSpecificationId == materialSpecificationId)
            .ToListAsync();

        return mapper.Map<List<FormResponseDto>>(formResponse, opts => opts.Items[AppConstants.ModelType]  = typeof(FormResponse));
    }
    
    public async Task<Result<IEnumerable<FormResponseDto>>> GetFormResponseByProductSpecification(Guid productSpecificationId)
    {
        var formResponse = await context.FormResponses
            .AsSplitQuery()
            .Include(fr => fr.CreatedBy)
            .Include(fr => fr.Response)
            .ThenInclude(fr => fr.CheckedBy)
            .Include(r => r.FormField)
            .ThenInclude(r => r.Question)
            .ThenInclude(r => r.Options)
            .Where(fr => fr.Response.ProductSpecificationId == productSpecificationId)
            .ToListAsync();

        return mapper.Map<List<FormResponseDto>>(formResponse, opts => opts.Items[AppConstants.ModelType]  = typeof(FormResponse));
    }*/
    
    


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
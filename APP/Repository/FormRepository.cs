using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.AnalyticalTestRequests;
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
            return validate.Errors;

        await context.Forms.AddAsync(form);
        await context.SaveChangesAsync();

        return form.Id;
    }

    public async Task<Result<FormDto>> GetForm(Guid formId)
    {
        var form = await context.Forms
            .AsSplitQuery()
            .Include(f => f.Sections.OrderBy(s => s.Order))
            .ThenInclude(s => s.Fields.OrderBy(f => f.Rank))
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
            .Include(f =>
                f.Sections.OrderByDescending(s => s.Order))
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

        if (filter.MaterialSpecificationId.HasValue)
        {
            query = query.Where(f => f.MaterialSpecificationId == filter.MaterialSpecificationId);
        }

        if (filter.ProductSpecificationId.HasValue)
        {
            query = query.Where(f => f.ProductSpecificationId == filter.ProductSpecificationId);
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

    public async Task<Result> SaveFormResponseDraft(SaveResponseDraftRequest request, Guid userId)
    {
        var response = await context.Responses
            .Include(r => r.FormResponses)
            .FirstOrDefaultAsync(r => r.Id == request.ResponseId);

        if (response is null)
        {
            // Create a new draft if not yet started
            response = new Response
            {
                FormId = request.FormId,
                MaterialBatchId = request.MaterialBatchId,
                BatchManufacturingRecordId = request.BatchManufacturingRecordId,
                ProductionActivityStepId = request.ProductionActivityStepId,
                CreatedById = userId,
                FormResponses = []
            };
            await context.Responses.AddAsync(response);
        }

        var formField = await context.FormFields
            .AsSplitQuery()
            .Include(f => f.Question)
            .FirstOrDefaultAsync(f => f.Id == request.FormFieldId);

        if (formField is null)
            return Error.Validation("Response.FormField", $"FormField not found {request.FormFieldId}");
        
        // 🧩 VALIDATION: Check if user is allowed in this specific context
        var fieldAssignee = await context.FormFieldAssignees.FirstOrDefaultAsync(a =>
            a.FormFieldId == formField.Id &&
            a.FormAssignee.MaterialBatchId == request.MaterialBatchId &&
            a.FormAssignee.BatchManufacturingRecordId == request.BatchManufacturingRecordId &&
            a.FormAssignee.ProductionActivityStepId == request.ProductionActivityStepId);

        if (fieldAssignee != null && fieldAssignee.AssigneeId != userId)
            return Error.Validation("Response.Unauthorized", "You are not assigned to this field in this context.");

        // Handle file-based questions
        if (formField.Question.Type is QuestionType.Signature or QuestionType.FileUpload)
        {
            var values = request.Value.Split("|");
            var formResponse = response.FormResponses.FirstOrDefault(fr => fr.FormFieldId == formField.Id);

            if (formResponse == null)
            {
                formResponse = new FormResponse
                {
                    FormFieldId = formField.Id,
                    Value = "form response attachment."
                };
                response.FormResponses.Add(formResponse);
            }

            foreach (var value in values)
            {
                var reference = Guid.NewGuid().ToString();
                await fileRepository.SaveBlobItem(
                    nameof(FormResponse).ToLower(),
                    formResponse.Id,
                    reference,
                    value.ConvertFromBase64(),
                    userId
                );
            }
        }
        else
        {
            // Update or insert text-based responses
            var existingResponse = response.FormResponses.FirstOrDefault(fr => fr.FormFieldId == formField.Id);
            if (existingResponse != null)
            {
                existingResponse.Value = request.Value;
                context.FormResponses.Update(existingResponse);
            }
            else
            {
                response.FormResponses.Add(new FormResponse
                {
                    FormFieldId = formField.Id,
                    Value = request.Value
                });
            }
        }

        await context.SaveChangesAsync();
        return Result.Success(response.Id);
    }

    public async Task<Result> SubmitFormResponseFinal(Guid responseId)
    {
        var response = await context.Responses
            .Include(r => r.FormResponses)
            .FirstOrDefaultAsync(r => r.Id == responseId);

        if (response == null)
            return Error.NotFound("Response.NotFound", "Response not found");

        // Validate that all required fields are filled
        var formFields = await context.FormFields
            .Where(f => f.FormSection.FormId == response.FormId)
            .ToListAsync();

        var missingFields = formFields
            .Where(f => f.Required && response.FormResponses.All(r => r.FormFieldId != f.Id))
            .ToList();

        if (missingFields.Any())
        {
            var missingList = string.Join(", ", missingFields.Select(f => f.Id));
            return Error.Validation("Response.MissingFields", $"Missing required fields: {missingList}");
        }

        // Perform final entity updates
        if (response.BatchManufacturingRecordId.HasValue || response.MaterialBatchId.HasValue)
        {
            if (response.MaterialBatchId.HasValue)
            {
                var batch = await context.MaterialBatches.FirstOrDefaultAsync(b => b.Id == response.MaterialBatchId);
                if (batch != null)
                {
                    batch.Status = BatchStatus.TestTaken;
                    context.MaterialBatches.Update(batch);
                }
            }
            else if (response.BatchManufacturingRecordId.HasValue)
            {
                var bmr = await context.BatchManufacturingRecords.FirstOrDefaultAsync(b => b.Id == response.BatchManufacturingRecordId);
                if (bmr != null)
                {
                    bmr.Status = BatchManufacturingStatus.TestTaken;
                    context.BatchManufacturingRecords.Update(bmr);
                }
            }
        }

        if (response.ProductionActivityStepId.HasValue)
        {
            var step = await context.ProductionActivitySteps
                .FirstOrDefaultAsync(s => s.Id == response.ProductionActivityStepId);
            if (step == null)
                return Error.NotFound("ProductionActivityStep", $"Not found {response.ProductionActivityStepId}");

            var atr = await context.AnalyticalTestRequests
                .FirstOrDefaultAsync(a => a.ProductionActivityStepId == response.ProductionActivityStepId);
            if (atr == null)
                return Error.NotFound("ATR", $"ATR not found {response.ProductionActivityStepId}");

            atr.Status = AnalyticalTestStatus.TestTaken;
            context.AnalyticalTestRequests.Update(atr);
        }

        // Optional: Handle linking with Material/Product Specifications
        var materialSpec = await context.MaterialSpecifications.FirstOrDefaultAsync(s => s.ResponseId == response.Id);
        if (materialSpec != null)
        {
            materialSpec.ResponseId = response.Id;
            context.MaterialSpecifications.Update(materialSpec);
        }

        var productSpec = await context.ProductSpecifications.FirstOrDefaultAsync(s => s.ResponseId == response.Id);
        if (productSpec != null)
        {
            productSpec.ResponseId = response.Id;
            context.ProductSpecifications.Update(productSpec);
        }

        await context.SaveChangesAsync();
        return Result.Success("Form successfully submitted and finalized.");
    }

    public async Task<Result> SubmitFormResponse(CreateResponseRequest request, Guid userId)
    {
        var newResponse = new Response
        {
            FormId = request.FormId,
            MaterialBatchId = request.MaterialBatchId,
            BatchManufacturingRecordId = request.BatchManufacturingRecordId,
            ProductionActivityStepId = request.ProductionActivityStepId,
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
            
            // 🧩 VALIDATION: Check if user is allowed in this specific context
            var fieldAssignee = await context.FormFieldAssignees.FirstOrDefaultAsync(a =>
                a.FormFieldId == formField.Id &&
                a.FormAssignee.MaterialBatchId == request.MaterialBatchId &&
                a.FormAssignee.BatchManufacturingRecordId == request.BatchManufacturingRecordId &&
                a.FormAssignee.ProductionActivityStepId == request.ProductionActivityStepId);

            if (fieldAssignee != null && fieldAssignee.AssigneeId != userId)
                return Error.Validation("Response.Unauthorized", "You are not assigned to this field in this context.");

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
                    await fileRepository.SaveBlobItem(nameof(FormResponse).ToLower(), formResponse.Id, reference, value.ConvertFromBase64(), userId);
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

        if (request.ProductionActivityStepId.HasValue)
        {
            var step = await context.ProductionActivitySteps.FirstOrDefaultAsync(s => s.Id == request.ProductionActivityStepId);
            if (step is null) return Error.NotFound("ProductionActivityStep", $"ProductionActivityStep not found {request.ProductionActivityStepId}");

            var atr = await context.AnalyticalTestRequests.FirstOrDefaultAsync(a => a.ProductionActivityStepId == request.ProductionActivityStepId);
            if (atr is null) return Error.NotFound("ATR", $"ATR not found {request.ProductionActivityStepId}");

            atr.Status = AnalyticalTestStatus.TestTaken;
            context.AnalyticalTestRequests.Update(atr);
        }

        if (request.MaterialSpecificationId.HasValue)
        {
            var materialSpecification = await context.MaterialSpecifications.FirstOrDefaultAsync(s => s.Id == request.MaterialSpecificationId);
            if (materialSpecification is null) return Error.NotFound("MaterialSpecification.NotFound", "Material specification not found");

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

    public async Task<Result> SubmitFormSectionValue(List<SubmitFormSectionValue> requests, Guid? materialSpecificationId, Guid? productSpecificationId)
    {
        var formSections = await context.FormSections
            .Where(s => requests.Select(r => r.FormSectionId).Contains(s.Id))
            .ToDictionaryAsync(k => k.Id, v => v);

        foreach (var request in requests)
        {
            if (!formSections.TryGetValue(request.FormSectionId, out var formSection)) continue;
            formSection.Value = request.Value;
            formSection.MaterialSpecificationId = materialSpecificationId;
            formSection.ProductSpecificationId = productSpecificationId;
        }

        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result> SaveFormAssigneeDraft(SaveFormAssigneeDraftRequest request, Guid userId)
    {
        var formAssignee = await context.FormAssignees
            .Include(r => r.FieldAssignees)
            .FirstOrDefaultAsync(r => r.Id == request.FormAssigneeId);

        if (formAssignee is null)
        {
            // Create a new draft if not yet started
            formAssignee = new FormAssignee
            {
                FormId = request.FormId,
                MaterialBatchId = request.MaterialBatchId,
                BatchManufacturingRecordId = request.BatchManufacturingRecordId,
                ProductionActivityStepId = request.ProductionActivityStepId,
                CreatedById = userId,
                FieldAssignees = []
            };
            await context.FormAssignees.AddAsync(formAssignee);
        }

        var formField = await context.FormFields
            .AsSplitQuery()
            .Include(f => f.Question)
            .FirstOrDefaultAsync(f => f.Id == request.FormFieldId);

        if (formField is null)
            return Error.Validation("Response.FormField", $"FormField not found {request.FormFieldId}");
        
        
        // Update or insert text-based responses
        var existingFieldAssignees = formAssignee
            .FieldAssignees.FirstOrDefault(fr => fr.FormFieldId == formField.Id);
        
        if (existingFieldAssignees != null)
        {
            existingFieldAssignees.AssigneeId = request.AssigneeId;
            context.FormFieldAssignees.Update(existingFieldAssignees);
        }
        else
        {
            formAssignee.FieldAssignees.Add(new FormFieldAssignee
            {
                FormFieldId = formField.Id,
                AssigneeId = request.AssigneeId
            });
        }
        

        await context.SaveChangesAsync();
        return Result.Success(formAssignee.Id);
    }

    public async Task<Result> SubmitFormAssigneeFinal(Guid formAssigneeId)
    {
        var formAssignee = await context.FormAssignees
            .Include(r => r.FieldAssignees)
            .FirstOrDefaultAsync(r => r.Id == formAssigneeId);

        if (formAssignee == null)
            return Error.NotFound("FormAssignee.NotFound", "Form assignee not found");

        // Validate that all required fields are filled
        var formFields = await context.FormFields
            .Where(f => f.FormSection.FormId == formAssignee.FormId)
            .ToListAsync();

        var missingFields = formFields
            .Where(f => f.Required && formAssignee.FieldAssignees.All(r => r.FormFieldId != f.Id))
            .ToList();

        if (missingFields.Any())
        {
            var missingList = string.Join(", ", missingFields.Select(f => f.Id));
            return Error.Validation("Response.MissingFields", $"Missing required fields: {missingList}");
        }

        await context.SaveChangesAsync();
        return Result.Success("Form successfully submitted and finalized.");
    }

    public async Task<Result> SubmitFormAssignee(CreateFormAssigneeRequest request, Guid userId)
    {
        var formAssignee = new FormAssignee
        {
            FormId = request.FormId,
            MaterialBatchId = request.MaterialBatchId,
            BatchManufacturingRecordId = request.BatchManufacturingRecordId,
            ProductionActivityStepId = request.ProductionActivityStepId,
            FieldAssignees = [],
            CreatedById = userId
        };

        foreach (var fieldAssignee in request.FormFieldAssignees)
        {
            var formField = await context.FormFields
                .AsSplitQuery()
                .Include(f => f.Question)
                .FirstOrDefaultAsync(field => field.Id == fieldAssignee.FormFieldId);

            if (formField == null)
            {
                return Error.Validation("Response.FormField", $"FormField not found {fieldAssignee.FormFieldId}");
            }

            formAssignee.FieldAssignees.Add(new FormFieldAssignee()
            {
                FormFieldId = formField.Id,
                AssigneeId = fieldAssignee.AssigneeId
            });
            
        }

        await context.FormAssignees.AddAsync(formAssignee);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> GenerateCertificateOfAnalysis(Guid materialBatchId, Guid userId)
    {
        var response = await context.Responses.FirstOrDefaultAsync(r =>
            r.MaterialBatchId == materialBatchId);
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

    public async Task<Result> GenerateCertificateOfAnalysisForProduct(Guid batchManufacturingRecordId, Guid productionActivityStepId, Guid userId)
    {
        var response = await context.Responses.FirstOrDefaultAsync(r =>
            r.BatchManufacturingRecordId == batchManufacturingRecordId && r.ProductionActivityStepId == productionActivityStepId);
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

    public async Task<Result<ResponseDetailDto>> GetFormResponse(Guid formResponseId)
    {
        var formResponse = await context.Responses
            .AsSplitQuery()
            .Include(fr => fr.BatchManufacturingRecord)
            .Include(fr => fr.MaterialBatch)
            .Include(fr => fr.CheckedBy)
            .Include(fr => fr.Form)
            .Include(fr => fr.CreatedBy)
            .Include(fr => fr.FormResponses)
            .ThenInclude(r => r.FormField)
            .ThenInclude(r => r.Question)
            .ThenInclude(r => r.Options)
            .FirstOrDefaultAsync(fr => fr.Id == formResponseId);

        if (formResponse == null)
            return FormErrors.NotFound(formResponseId);

        return mapper.Map<ResponseDetailDto>(formResponse);
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

        return mapper.Map<List<FormDto>>(form, opts => opts.Items[AppConstants.ModelType] = typeof(FormResponse));
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

        return mapper.Map<List<FormDto>>(form, opts => opts.Items[AppConstants.ModelType] = typeof(FormResponse));
    }
    
    public async Task<Result<FormAssigneeDto>> GetFormAssignee(Guid formAssigneeId)
    {
        var formAssignee = await context.FormAssignees
            .AsSplitQuery()
            .Include(fa => fa.Form)
            .ThenInclude(f => f.Sections)
            .ThenInclude(s => s.Fields)
            .ThenInclude(fld => fld.Question)
            .ThenInclude(q => q.Options)
            .Include(fa => fa.FieldAssignees)
            .ThenInclude(af => af.FormField)
            .Include(fa => fa.FieldAssignees)
            .ThenInclude(af => af.Assignee)
            .Include(fa => fa.CreatedBy)
            .FirstOrDefaultAsync(fa => fa.Id == formAssigneeId);

        if (formAssignee == null)
            return FormErrors.NotFound(formAssigneeId);

        return mapper.Map<FormAssigneeDto>(formAssignee);
    }
    
    
    public async Task<Result<FormAssigneeDto>> GetFormAssigneeByBatch(Guid materialBatchId)
    {
        var formAssignee = await context.FormAssignees
            .AsSplitQuery()
            .Include(fa => fa.Form)
            .ThenInclude(f => f.Sections)
            .ThenInclude(s => s.Fields)
            .ThenInclude(fld => fld.Question)
            .ThenInclude(q => q.Options)
            .Include(fa => fa.FieldAssignees)
            .ThenInclude(af => af.FormField)
            .Include(fa => fa.FieldAssignees)
            .ThenInclude(af => af.Assignee)
            .Include(fa => fa.CreatedBy)
            .FirstOrDefaultAsync(fa => fa.MaterialBatchId == materialBatchId);

        if (formAssignee == null)
            return FormErrors.NotFound(materialBatchId);

        return mapper.Map<FormAssigneeDto>(formAssignee);
    }
    
    public async Task<Result<FormAssigneeDto>> GetFormAssigneeByBmr(Guid bmrId)
    {
        var formAssignee = await context.FormAssignees
            .AsSplitQuery()
            .Include(fa => fa.Form)
            .ThenInclude(f => f.Sections)
            .ThenInclude(s => s.Fields)
            .ThenInclude(fld => fld.Question)
            .ThenInclude(q => q.Options)
            .Include(fa => fa.FieldAssignees)
            .ThenInclude(af => af.FormField)
            .Include(fa => fa.FieldAssignees)
            .ThenInclude(af => af.Assignee)
            .Include(fa => fa.CreatedBy)
            .FirstOrDefaultAsync(fa => fa.BatchManufacturingRecordId == bmrId);

        if (formAssignee == null)
            return FormErrors.NotFound(bmrId);

        return mapper.Map<FormAssigneeDto>(formAssignee);
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
            .IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(fr => fr.CreatedBy)
            .Include(fr => fr.Response)
            .ThenInclude(fr => fr.CheckedBy)
            .Include(r => r.FormField)
            .ThenInclude(r => r.Question)
            .ThenInclude(r => r.Options)
            .Include(r => r.FormField)
            .ThenInclude(f => f.FormSection)
            .Include(f => f.CreatedBy)
            .Where(fr => fr.Response.MaterialBatchId == materialBatchId)
            .ToListAsync();

        return mapper.Map<List<FormResponseDto>>(formResponse, opts => opts.Items[AppConstants.ModelType] = typeof(FormResponse));
    }

    public async Task<Result<IEnumerable<FormResponseDto>>> GetFormResponseByBmr(Guid batchManufacturingRecordId)
    {
        var formResponse = await context.FormResponses
            .IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(fr => fr.CreatedBy)
            .Include(fr => fr.Response)
            .ThenInclude(fr => fr.CheckedBy)
            .Include(r => r.FormField)
            .ThenInclude(r => r.Question)
            .ThenInclude(r => r.Options)
            .Where(fr => fr.Response.BatchManufacturingRecordId == batchManufacturingRecordId)
            .ToListAsync();

        return mapper.Map<List<FormResponseDto>>(formResponse, opt => opt.Items[AppConstants.ModelType] = typeof(FormResponse));
    }

    public async Task<Result<IEnumerable<FormResponseDto>>> GetFormResponseByMaterialSpecification(Guid materialSpecificationId)
    {
        var materialSpec = await context.MaterialSpecifications.FirstOrDefaultAsync(m => m.Id == materialSpecificationId);
        if (materialSpec is null) return Error.NotFound("Material.Spec", "Material Specification not found");

        var formResponse = await context.FormResponses
            .IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(fr => fr.CreatedBy)
            .Include(fr => fr.Response)
            .ThenInclude(fr => fr.CheckedBy)
            .Include(r => r.FormField)
            .ThenInclude(r => r.Question)
            .ThenInclude(r => r.Options)
            .Include(r => r.FormField)
            .ThenInclude(f => f.FormSection)
            .Include(f => f.CreatedBy)
            .Where(fr => fr.ResponseId == materialSpec.ResponseId)
            .ToListAsync();

        return mapper.Map<List<FormResponseDto>>(formResponse, opts => opts.Items[AppConstants.ModelType] = typeof(FormResponse));
    }

    public async Task<Result<IEnumerable<FormResponseDto>>> GetFormResponseByProductSpecification(Guid productSpecificationId)
    {
        var productSpec = await context.ProductSpecifications.FirstOrDefaultAsync(p => p.Id == productSpecificationId);
        if (productSpec is null) return Error.NotFound("Product.Spec", "Product specification not found");
        var formResponse = await context.FormResponses
            .IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(fr => fr.CreatedBy)
            .Include(fr => fr.Response)
            .ThenInclude(fr => fr.CheckedBy)
            .Include(r => r.FormField)
            .ThenInclude(r => r.Question)
            .ThenInclude(r => r.Options)
            .Include(r => r.FormField)
            .ThenInclude(f => f.FormSection)
            .Include(f => f.CreatedBy)
            .Where(fr => fr.ResponseId == productSpec.ResponseId)
            .ToListAsync();

        return mapper.Map<List<FormResponseDto>>(formResponse, opts => opts.Items[AppConstants.ModelType] = typeof(FormResponse));
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
    public async Task<Result<Paginateable<IEnumerable<QuestionDto>>>> GetQuestions(QuestionFilter filter)
    {
        var query = context.Questions
            .AsSplitQuery()
            .OrderByDescending(f => f.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filter.SearchQuery))
        {
            query = query.WhereSearch(filter.SearchQuery, q => q.Label, q => q.CreatedBy.FirstName, q => q.CreatedBy.LastName);
        }

        if (filter.FormType.HasValue)
        {
            query = filter.FormType.Value switch
            {
                FormType.Default => query.Where(q => q.Type != QuestionType.Specification),
                FormType.Specification => query.Where(q => q.Type == QuestionType.Specification),
                _ => query
            };
        }


        if (filter.Type.Count != 0)
        {
            var typesToFilter = filter.Type.Where(t => t.HasValue).Select(t => t.Value).ToList();
            query = query.Where(q => typesToFilter.Contains(q.Type));
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
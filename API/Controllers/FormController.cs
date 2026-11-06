using API.Authorization;
using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Forms.Request;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using APP.Services.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/form")]
[ApiController]
public class FormController(IFormRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new form.
    /// </summary>
    /// <param name="request">The CreateFormRequest object.</param>
    /// <returns>Returns the ID of the created form.</returns>
    [HttpPost]
    [AnyPermission("CanCreatePackagingMaterialArd", "CanCreatePackagingMaterialSpecification", "CanCreateProductArd", "CanCreateProductSpecification", "CanCreateProductionProduct", "CanCreateRawMaterialArd", "CanCreateRawMaterialSpecification", "CanCreateTemplate", "CanManageRoutineSetup")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateForm([FromBody] CreateFormRequest request)
    {
        var result = await repository.CreateForm(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific form by its ID.
    /// </summary>
    /// <param name="formId">The ID of the form.</param>
    /// <returns>Returns the form details.</returns>
    [HttpGet("{formId}")]
    [AnyPermission("CanAssignProductTest", "CanCheckPackagingMaterialTestResult", "CanCheckProductTest", "CanCheckRawMaterialTestResult", "CanCreatePackagingMaterialSpecification", "CanCreateProductSpecification", "CanCreateProductionProduct", "CanCreateQuestions", "CanCreateRawMaterialSpecification", "CanCreateTemplate", "CanEditPackagingMaterialSpecification", "CanEditProductSpecification", "CanEditQuestions", "CanEditRawMaterialSpecification", "CanEditTemplate", "CanReassignPackagingMaterialTest", "CanReassignProductTest", "CanReassignRawMaterialTest", "CanRepairQuestionFormula", "CanRepairTestFormula", "CanStartPackagingMaterialTest", "CanStartProductTest", "CanStartRawMaterialTest", "CanViewAnalyticalTestRequests", "CanViewAnalyticalTestRequestsForSampling", "CanViewPackagingMaterialArds", "CanViewPackagingMaterialGoodsReceiptNotes", "CanViewPackagingMaterialSpecifications", "CanViewPayrollRuns", "CanViewPendingApprovals", "CanViewPerformanceReviews", "CanViewProductAnalyticalTestRequests", "CanViewProductAnalyticalTestRequestsForAnalysing", "CanViewProductArds", "CanViewProductSpecifications", "CanViewProductionProductArds", "CanViewProductionProductBom", "CanViewProductionProductInfo", "CanViewProductionProductPackingMaterials", "CanViewProductionProductPackingOrder", "CanViewProductionProductPackingStyles", "CanViewProductionProductProcedure", "CanViewProductionProductSpecifications", "CanViewProductionProductStps", "CanViewProductionProducts", "CanViewQaProductArds", "CanViewQaProductBom", "CanViewQaProductInfo", "CanViewQaProductPackingMaterials", "CanViewQaProductPackingOrder", "CanViewQaProductPackingStyles", "CanViewQaProductProcedure", "CanViewQaProductSpecifications", "CanViewQaProductStps", "CanViewQaProducts", "CanViewQuestions", "CanViewRawMaterialArds", "CanViewRawMaterialGoodsReceiptNotes", "CanViewRawMaterialSpecifications", "CanViewResponseReworkTaskDetail", "CanViewResponseReworkTasks", "CanViewRoutineSetup", "CanViewRoutineTests", "CanViewShipmentDiscrepancy", "CanViewStockDiscrepancy", "CanViewTemplate", "CanViewWaterQuality")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FormDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetForm(Guid formId, [FromQuery] Guid? responseId = null)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.GetForm(formId, responseId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of forms.
    /// </summary>
    /// <param name="filter">The FormFilter object for filtering forms.</param>
    /// <returns>Returns a paginated list of forms.</returns>
    [HttpGet]
    [AnyPermission("CanAssignProductTest", "CanCheckPackagingMaterialTestResult", "CanCheckProductTest", "CanCheckRawMaterialTestResult", "CanCreatePackagingMaterialSpecification", "CanCreateProductSpecification", "CanCreateProductionProduct", "CanCreateQuestions", "CanCreateRawMaterialSpecification", "CanCreateTemplate", "CanEditPackagingMaterialSpecification", "CanEditProductSpecification", "CanEditQuestions", "CanEditRawMaterialSpecification", "CanEditTemplate", "CanReassignPackagingMaterialTest", "CanReassignProductTest", "CanReassignRawMaterialTest", "CanRepairQuestionFormula", "CanRepairTestFormula", "CanStartPackagingMaterialTest", "CanStartProductTest", "CanStartRawMaterialTest", "CanViewAnalyticalTestRequests", "CanViewAnalyticalTestRequestsForSampling", "CanViewPackagingMaterialArds", "CanViewPackagingMaterialGoodsReceiptNotes", "CanViewPackagingMaterialSpecifications", "CanViewPayrollRuns", "CanViewPendingApprovals", "CanViewPerformanceReviews", "CanViewProductAnalyticalTestRequests", "CanViewProductAnalyticalTestRequestsForAnalysing", "CanViewProductArds", "CanViewProductSpecifications", "CanViewProductionProductArds", "CanViewProductionProductBom", "CanViewProductionProductInfo", "CanViewProductionProductPackingMaterials", "CanViewProductionProductPackingOrder", "CanViewProductionProductPackingStyles", "CanViewProductionProductProcedure", "CanViewProductionProductSpecifications", "CanViewProductionProductStps", "CanViewProductionProducts", "CanViewQaProductArds", "CanViewQaProductBom", "CanViewQaProductInfo", "CanViewQaProductPackingMaterials", "CanViewQaProductPackingOrder", "CanViewQaProductPackingStyles", "CanViewQaProductProcedure", "CanViewQaProductSpecifications", "CanViewQaProductStps", "CanViewQaProducts", "CanViewQuestions", "CanViewRawMaterialArds", "CanViewRawMaterialGoodsReceiptNotes", "CanViewRawMaterialSpecifications", "CanViewResponseReworkTaskDetail", "CanViewResponseReworkTasks", "CanViewRoutineSetup", "CanViewRoutineTests", "CanViewShipmentDiscrepancy", "CanViewStockDiscrepancy", "CanViewTemplate", "CanViewWaterQuality")]
    [Authorize]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<FormDto>>)
    )]
    public async Task<IResult> GetForms([FromQuery] FormFilter filter)
    {
        var result = await repository.GetForms(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of forms.
    /// </summary>
    /// <param name="filter">The FormFilter object for filtering form sections.</param>
    /// <returns>Returns a paginated list of forms.</returns>
    [HttpGet("section")]
    [AnyPermission("CanAssignProductTest", "CanCheckPackagingMaterialTestResult", "CanCheckProductTest", "CanCheckRawMaterialTestResult", "CanCreatePackagingMaterialSpecification", "CanCreateProductSpecification", "CanCreateProductionProduct", "CanCreateQuestions", "CanCreateRawMaterialSpecification", "CanCreateTemplate", "CanEditPackagingMaterialSpecification", "CanEditProductSpecification", "CanEditQuestions", "CanEditRawMaterialSpecification", "CanEditTemplate", "CanReassignPackagingMaterialTest", "CanReassignProductTest", "CanReassignRawMaterialTest", "CanRepairQuestionFormula", "CanRepairTestFormula", "CanStartPackagingMaterialTest", "CanStartProductTest", "CanStartRawMaterialTest", "CanViewAnalyticalTestRequests", "CanViewAnalyticalTestRequestsForSampling", "CanViewPackagingMaterialArds", "CanViewPackagingMaterialGoodsReceiptNotes", "CanViewPackagingMaterialSpecifications", "CanViewPayrollRuns", "CanViewPendingApprovals", "CanViewPerformanceReviews", "CanViewProductAnalyticalTestRequests", "CanViewProductAnalyticalTestRequestsForAnalysing", "CanViewProductArds", "CanViewProductSpecifications", "CanViewProductionProductArds", "CanViewProductionProductBom", "CanViewProductionProductInfo", "CanViewProductionProductPackingMaterials", "CanViewProductionProductPackingOrder", "CanViewProductionProductPackingStyles", "CanViewProductionProductProcedure", "CanViewProductionProductSpecifications", "CanViewProductionProductStps", "CanViewProductionProducts", "CanViewQaProductArds", "CanViewQaProductBom", "CanViewQaProductInfo", "CanViewQaProductPackingMaterials", "CanViewQaProductPackingOrder", "CanViewQaProductPackingStyles", "CanViewQaProductProcedure", "CanViewQaProductSpecifications", "CanViewQaProductStps", "CanViewQaProducts", "CanViewQuestions", "CanViewRawMaterialArds", "CanViewRawMaterialGoodsReceiptNotes", "CanViewRawMaterialSpecifications", "CanViewResponseReworkTaskDetail", "CanViewResponseReworkTasks", "CanViewRoutineSetup", "CanViewRoutineTests", "CanViewShipmentDiscrepancy", "CanViewStockDiscrepancy", "CanViewTemplate", "CanViewWaterQuality")]
    [Authorize]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<FormSectionDto>>)
    )]
    public async Task<IResult> GetFormSection([FromQuery] FormFilter filter)
    {
        var result = await repository.GetFormSections(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific form by its ID.
    /// </summary>
    /// <param name="request">The CreateFormRequest object containing updated form data.</param>
    /// <param name="formId">The ID of the form to be updated.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPut("{formId}")]
    [AnyPermission("CanEditPackagingMaterialArd", "CanEditPackagingMaterialSpecification", "CanEditProductArd", "CanEditProductSpecification", "CanEditRawMaterialArd", "CanEditRawMaterialSpecification", "CanEditTemplate", "CanManageRoutineSetup")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> UpdateForm([FromBody] CreateFormRequest request, Guid formId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.UpdateForm(request, formId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates form metadata.
    /// </summary>
    [HttpPut("{formId}/metadata")]
    [AnyPermission("CanEditPackagingMaterialArd", "CanEditPackagingMaterialSpecification", "CanEditProductArd", "CanEditProductSpecification", "CanEditRawMaterialArd", "CanEditRawMaterialSpecification", "CanEditTemplate", "CanManageRoutineSetup")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateFormMetadata(
        [FromRoute] Guid formId,
        [FromBody] UpdateFormMetadataRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.UpdateFormMetadata(formId, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Adds a section to a form.
    /// </summary>
    [HttpPost("{formId}/section")]
    [AnyPermission("CanEditPackagingMaterialArd", "CanEditPackagingMaterialSpecification", "CanEditProductArd", "CanEditProductSpecification", "CanEditRawMaterialArd", "CanEditRawMaterialSpecification", "CanEditTemplate", "CanManageRoutineSetup")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> AddFormSection(
        [FromRoute] Guid formId,
        [FromBody] CreateFormSectionRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.AddFormSection(formId, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a form section.
    /// </summary>
    [HttpPut("section/{sectionId}")]
    [AnyPermission("CanEditPackagingMaterialArd", "CanEditPackagingMaterialSpecification", "CanEditProductArd", "CanEditProductSpecification", "CanEditRawMaterialArd", "CanEditRawMaterialSpecification", "CanEditTemplate", "CanManageRoutineSetup")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateFormSection(
        [FromRoute] Guid sectionId,
        [FromBody] UpdateFormSectionRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.UpdateFormSection(sectionId, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a form section.
    /// </summary>
    [HttpDelete("section/{sectionId}")]
    [AnyPermission("CanEditPackagingMaterialArd", "CanEditPackagingMaterialSpecification", "CanEditProductArd", "CanEditProductSpecification", "CanEditRawMaterialArd", "CanEditRawMaterialSpecification", "CanEditTemplate", "CanManageRoutineSetup")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteFormSection([FromRoute] Guid sectionId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.DeleteFormSection(sectionId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Adds a field to a section.
    /// </summary>
    [HttpPost("section/{sectionId}/field")]
    [AnyPermission("CanEditPackagingMaterialArd", "CanEditPackagingMaterialSpecification", "CanEditProductArd", "CanEditProductSpecification", "CanEditRawMaterialArd", "CanEditRawMaterialSpecification", "CanEditTemplate", "CanManageRoutineSetup")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> AddFormField(
        [FromRoute] Guid sectionId,
        [FromBody] CreateFormFieldRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.AddFormField(sectionId, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a form field.
    /// </summary>
    [HttpPut("field/{fieldId}")]
    [AnyPermission("CanEditPackagingMaterialArd", "CanEditPackagingMaterialSpecification", "CanEditProductArd", "CanEditProductSpecification", "CanEditRawMaterialArd", "CanEditRawMaterialSpecification", "CanEditTemplate", "CanManageRoutineSetup")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateFormField(
        [FromRoute] Guid fieldId,
        [FromBody] UpdateFormFieldRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.UpdateFormField(fieldId, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a form field.
    /// </summary>
    [HttpDelete("field/{fieldId}")]
    [AnyPermission("CanEditPackagingMaterialArd", "CanEditPackagingMaterialSpecification", "CanEditProductArd", "CanEditProductSpecification", "CanEditRawMaterialArd", "CanEditRawMaterialSpecification", "CanEditTemplate", "CanManageRoutineSetup")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteFormField([FromRoute] Guid fieldId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.DeleteFormField(fieldId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Adds a reviewer to a form.
    /// </summary>
    [HttpPost("{formId}/reviewer")]
    [AnyPermission("CanEditPackagingMaterialArd", "CanEditPackagingMaterialSpecification", "CanEditProductArd", "CanEditProductSpecification", "CanEditRawMaterialArd", "CanEditRawMaterialSpecification", "CanEditTemplate", "CanManageRoutineSetup")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> AddFormReviewer(
        [FromRoute] Guid formId,
        [FromBody] CreateFormReviewerRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.AddFormReviewer(formId, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a reviewer from a form.
    /// </summary>
    [HttpDelete("{formId}/reviewer/{reviewerUserId}")]
    [AnyPermission("CanEditPackagingMaterialArd", "CanEditPackagingMaterialSpecification", "CanEditProductArd", "CanEditProductSpecification", "CanEditRawMaterialArd", "CanEditRawMaterialSpecification", "CanEditTemplate", "CanManageRoutineSetup")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteFormReviewer(
        [FromRoute] Guid formId,
        [FromRoute] Guid reviewerUserId
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.DeleteFormReviewer(
            formId,
            reviewerUserId,
            Guid.Parse(userId)
        );
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific form by its ID.
    /// </summary>
    /// <param name="formId">The ID of the form to be deleted.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpDelete("{formId}")]
    [AnyPermission("CanDeletePackagingMaterialArd", "CanDeletePackagingMaterialSpecification", "CanDeleteProductArd", "CanDeleteProductSpecification", "CanDeleteRawMaterialArd", "CanDeleteRawMaterialSpecification", "CanDeleteTemplate", "CanManageRoutineSetup")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteForm(Guid formId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.DeleteForm(formId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Saves or updates a single field response as a draft.
    /// </summary>
    /// <param name="request">The SaveResponseDraftRequest object containing field response data.</param>
    /// <returns>Returns the ResponseId if successful, or a failure result.</returns>
    [HttpPost("responses/draft")]
    [AnyPermission("CanCreatePackagingMaterialSpecification", "CanCreateProductSpecification", "CanCreateRawMaterialSpecification", "CanEditPackagingMaterialSpecification", "CanEditProductSpecification", "CanEditRawMaterialSpecification", "CanExecuteFormulaResponse", "CanPerformRoutineTest", "CanStartPackagingMaterialTest", "CanStartProductTest", "CanStartRawMaterialTest")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> SaveFormResponseDraft([FromBody] SaveResponseDraftRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.SaveFormResponseDraft(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Finalizes a form response submission after all required fields have been filled.
    /// </summary>
    /// <param name="responseId">The unique identifier of the Response being finalized.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("responses/finalize/{responseId:guid}")]
    [Authorize(PermissionKeys.CanExecuteFormulaResponse)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> SubmitFormResponseFinal(Guid responseId)
    {
        var userId = HttpContext.Items["Sub"] as string;
        if (!Guid.TryParse(userId, out var actorId)) return TypedResults.Unauthorized();
        var result = await repository.SubmitFormResponseFinal(responseId, actorId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Submits a response to a form.
    /// </summary>
    /// <param name="request">The CreateResponseRequest object containing response data.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("responses")]
    [Authorize(PermissionKeys.CanExecuteFormulaResponse)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> SubmitFormResponse([FromBody] CreateResponseRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.SubmitFormResponse(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Submits a response to a form section.
    /// </summary>
    /// <param name="request">The CreateResponseRequest object containing response data.</param>
    /// <param name="materialSpecificationId">The id of the material spec</param>
    /// <param name="productSpecificationId">The id of the product spec</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("responses/section")]
    [AnyPermission("CanCreatePackagingMaterialSpecification", "CanCreateProductSpecification", "CanCreateRawMaterialSpecification", "CanEditPackagingMaterialSpecification", "CanEditProductSpecification", "CanEditRawMaterialSpecification", "CanExecuteFormulaResponse", "CanPerformRoutineTest", "CanStartPackagingMaterialTest", "CanStartProductTest", "CanStartRawMaterialTest")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> SubmitFormSectionResponse(
        [FromBody] List<SubmitFormSectionValue> request,
        [FromQuery] Guid? materialSpecificationId,
        [FromQuery] Guid? productSpecificationId
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.SubmitFormSectionValue(
            request,
            materialSpecificationId,
            productSpecificationId
        );
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Submits a response to a form section.
    /// </summary>
    /// <param name="request">The CreateResponseRequest object containing response data.</param>
    /// <param name="materialSpecificationId">The id of the material spec</param>
    /// <param name="productSpecificationId">The id of the product spec</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPut("responses/section")]
    [AnyPermission("CanCreatePackagingMaterialSpecification", "CanCreateProductSpecification", "CanCreateRawMaterialSpecification", "CanEditPackagingMaterialSpecification", "CanEditProductSpecification", "CanEditRawMaterialSpecification", "CanExecuteFormulaResponse", "CanPerformRoutineTest", "CanStartPackagingMaterialTest", "CanStartProductTest", "CanStartRawMaterialTest")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> UpdateFormSectionResponse(
        [FromBody] List<SubmitFormSectionValue> request,
        [FromQuery] Guid? materialSpecificationId,
        [FromQuery] Guid? productSpecificationId
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.UpdateFormSectionValue(
            request,
            materialSpecificationId,
            productSpecificationId
        );
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific form response by its ID.
    /// </summary>
    /// <param name="formResponseId">The ID of the form response.</param>
    /// <returns>Returns the form response details.</returns>
    [HttpGet("responses/{formResponseId}")]
    [AnyPermission("CanAssignProductTest", "CanCheckProductTest", "CanReassignProductTest", "CanStartProductTest", "CanViewAnalyticalTestRequests", "CanViewAnalyticalTestRequestsForSampling", "CanViewPayrollRuns", "CanViewPendingApprovals", "CanViewPerformanceReviews", "CanViewProductAnalyticalTestRequests", "CanViewProductAnalyticalTestRequestsForAnalysing", "CanViewResponseReworkTaskDetail", "CanViewResponseReworkTasks", "CanViewRoutineSetup", "CanViewRoutineTests", "CanViewShipmentDiscrepancy", "CanViewStockDiscrepancy", "CanViewWaterQuality")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDetailDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetFormResponse(Guid formResponseId)
    {
        var result = await repository.GetFormResponse(formResponseId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Creates a new question.
    /// </summary>
    /// <param name="request">The CreateQuestionRequest object containing question data.</param>
    /// <returns>Returns the ID of the created question.</returns>
    [HttpPost("question")]
    [AnyPermission("CanCreateQuestions")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateQuestion([FromBody] CreateQuestionRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.CreateQuestion(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific question by its ID.
    /// </summary>
    /// <param name="questionId">The ID of the question.</param>
    /// <returns>Returns the question details.</returns>
    [HttpGet("question/{questionId}")]
    [AnyPermission("CanCreateQuestions", "CanEditQuestions", "CanRepairQuestionFormula", "CanRepairTestFormula", "CanViewQuestions")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(QuestionDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetQuestion(Guid questionId)
    {
        var result = await repository.GetQuestion(questionId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of questions.
    /// </summary>
    /// <param name="filter">The PagedQuery object for pagination and filtering.</param>
    /// <returns>Returns a paginated list of questions.</returns>
    [HttpGet("question")]
    [AnyPermission("CanCreateQuestions", "CanCreateTemplate", "CanEditQuestions", "CanEditTemplate", "CanRepairQuestionFormula", "CanRepairTestFormula", "CanViewQuestions", "CanViewTemplate")]
    [Authorize]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<QuestionDto>>)
    )]
    public async Task<IResult> GetQuestions([FromQuery] QuestionFilter filter)
    {
        var result = await repository.GetQuestions(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific question by its ID.
    /// </summary>
    /// <param name="request">The CreateQuestionRequest object containing updated question data.</param>
    /// <param name="questionId">The ID of the question to be updated.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPut("question/{questionId}")]
    [AnyPermission("CanEditQuestions")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateQuestion(
        [FromBody] CreateQuestionRequest request,
        Guid questionId
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.UpdateQuestion(request, questionId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific question by its ID.
    /// </summary>
    /// <param name="questionId">The ID of the question to be deleted.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpDelete("question/{questionId}")]
    [AnyPermission("CanDeleteQuestions")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteQuestion(Guid questionId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.DeleteQuestion(questionId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Generates the Certificate of Analysis for a given material batch.
    /// </summary>
    /// <param name="materialBatchId">The ID of the material batch.</param>
    /// <param name="complies">The list of section ids that comply</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("generate-certificate/{materialBatchId}")]
    [PermissionCheckedInAction("Needs the raw or packaging check key for the batch's material.")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GenerateCertificateOfAnalysis(
        Guid materialBatchId,
        List<CertificateOfAnalysisComplies> complies,
        [FromServices] ApplicationDbContext context,
        [FromQuery] Guid? expectedResponseId = null
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();
        var kind = await context.MaterialBatches.Where(item => item.Id == materialBatchId)
            .Select(item => (DOMAIN.Entities.Materials.MaterialKind?)item.Material.Kind).SingleOrDefaultAsync();
        if (kind is null) return TypedResults.NotFound();
        var grants = await CurrentPermissionSet.Load(context, Guid.Parse(userId));
        var permission = kind == DOMAIN.Entities.Materials.MaterialKind.Raw
            ? PermissionKeys.CanCheckRawMaterialTestResult : PermissionKeys.CanCheckPackagingMaterialTestResult;
        if (!grants.Contains(permission)) return TypedResults.Forbid();
        var result = await repository.GenerateCertificateOfAnalysis(
            materialBatchId,
            Guid.Parse(userId),
            complies, expectedResponseId
        );
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Generates the Certificate of Analysis for a given material batch.
    /// </summary>
    /// <param name="batchManufacturingRecordId">The ID of the batch manufacturing.</param>
    /// <param name="productionActivityStepId">The id of production activity step</param>
    /// <param name="complies">The list of section ids that comply</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost(
        "generate-certificate/product/{batchManufacturingRecordId}/{productionActivityStepId}"
    )]
    [AnyPermission("CanCheckProductTest")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GenerateCertificateOfAnalysisForProduct(
        [FromRoute] Guid batchManufacturingRecordId,
        [FromRoute] Guid productionActivityStepId,
        [FromBody] List<CertificateOfAnalysisComplies> complies,
        [FromServices] ApplicationDbContext context,
        [FromQuery] Guid? expectedResponseId = null
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();
        var grants = await CurrentPermissionSet.Load(context, Guid.Parse(userId));
        if (!grants.Contains(PermissionKeys.CanCheckProductTest)) return TypedResults.Forbid();
        var result = await repository.GenerateCertificateOfAnalysisForProduct(
            batchManufacturingRecordId,
            productionActivityStepId,
            Guid.Parse(userId),
            complies, expectedResponseId
        );
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets form response by material batch ID.
    /// </summary>
    /// <param name="materialBatchId">The ID of the material batch.</param>
    /// <returns>Returns the form response.</returns>
    [HttpGet("responses/material-batch/{materialBatchId}")]
    [AnyPermission("CanCheckPackagingMaterialTestResult", "CanCheckRawMaterialTestResult", "CanReassignPackagingMaterialTest", "CanReassignRawMaterialTest", "CanStartPackagingMaterialTest", "CanStartRawMaterialTest", "CanViewPackagingMaterialGoodsReceiptNotes", "CanViewPayrollRuns", "CanViewPendingApprovals", "CanViewPerformanceReviews", "CanViewRawMaterialGoodsReceiptNotes", "CanViewShipmentDiscrepancy", "CanViewStockDiscrepancy")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<FormResponseDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetFormResponseByMaterialBatch(Guid materialBatchId)
    {
        var result = await repository.GetFormResponseByMaterialBatch(materialBatchId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets form response by batch manufacturing record ID.
    /// </summary>
    /// <param name="batchManufacturingRecordId">The ID of the batch manufacturing record.</param>
    /// <param name="productionActivityStepId">Optional production stage step filter.</param>
    /// <returns>Returns the form response.</returns>
    [HttpGet("responses/bmr/{batchManufacturingRecordId}")]
    [AnyPermission("CanViewPayrollRuns", "CanViewPendingApprovals", "CanViewPerformanceReviews", "CanViewShipmentDiscrepancy", "CanViewStockDiscrepancy")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<FormResponseDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetFormResponseByBmr(
        Guid batchManufacturingRecordId,
        [FromQuery] Guid? productionActivityStepId = null
    )
    {
        var result = await repository.GetFormResponseByBmr(
            batchManufacturingRecordId,
            productionActivityStepId
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets form response by batch manufacturing record ID.
    /// </summary>
    /// <param name="materialSpecificationId">The ID of the material spec.</param>
    /// <returns>Returns the form response.</returns>
    [HttpGet("responses/material-specification/{materialSpecificationId}")]
    [AnyPermission("CanEditPackagingMaterialSpecification", "CanEditProductSpecification", "CanEditRawMaterialSpecification", "CanViewPackagingMaterialSpecifications", "CanViewRawMaterialSpecifications")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<FormResponseDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetFormResponseByMaterialSpec(Guid materialSpecificationId)
    {
        var result = await repository.GetFormResponseByMaterialSpecification(
            materialSpecificationId
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets form response by batch manufacturing record ID.
    /// </summary>
    /// <param name="productSpecificationId">The ID of the product spec</param>
    /// <returns>Returns the form response.</returns>
    [HttpGet("responses/product-specification/{productSpecificationId}")]
    [AnyPermission("CanEditPackagingMaterialSpecification", "CanEditProductSpecification", "CanEditRawMaterialSpecification", "CanViewProductSpecifications")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<FormResponseDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetFormResponseByProductSpecification(Guid productSpecificationId)
    {
        var result = await repository.GetFormResponseByProductSpecification(productSpecificationId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets form with response by material batch ID.
    /// </summary>
    /// <param name="materialBatchId">The ID of the material batch.</param>
    /// <returns>Returns the form response.</returns>
    [HttpGet("with-responses/material-batch/{materialBatchId}")]
    [AnyPermission("CanStartPackagingMaterialTest", "CanStartRawMaterialTest", "CanViewPackagingMaterialGoodsReceiptNotes", "CanViewPendingApprovals", "CanViewRawMaterialGoodsReceiptNotes")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FormDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetFormWithResponseByMaterialBatch(Guid materialBatchId)
    {
        var result = await repository.GetFormWithResponseByMaterialBatch(materialBatchId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets form response by batch manufacturing record ID.
    /// </summary>
    /// <param name="batchManufacturingRecordId">The ID of the batch manufacturing record.</param>
    /// <returns>Returns the form response.</returns>
    [HttpGet("with-responses/bmr/{batchManufacturingRecordId}")]
    [AnyPermission("CanCheckProductTest", "CanStartProductTest", "CanViewAnalyticalTestRequests", "CanViewPendingApprovals", "CanViewProductAnalyticalTestRequests")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FormDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetFormWithResponseByBmr(Guid batchManufacturingRecordId)
    {
        var result = await repository.GetFormWithResponseByBmr(batchManufacturingRecordId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Saves or updates a single field assignment as a draft.
    /// </summary>
    /// <param name="request">The SaveFormAssigneeDraftRequest object containing field assignment data.</param>
    /// <returns>Returns the FormAssigneeId if successful, or a failure result.</returns>
    [HttpPost("assignees/draft")]
    [AnyPermission("CanExecuteFormulaResponse", "CanPerformRoutineTest", "CanStartPackagingMaterialTest", "CanStartProductTest", "CanStartRawMaterialTest")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> SaveFormAssigneeDraft(
        [FromBody] SaveFormAssigneeDraftRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.SaveFormAssigneeDraft(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Finalizes a form assignee submission after all required fields have been filled.
    /// </summary>
    /// <param name="formAssigneeId">The unique identifier of the FormAssignee being finalized.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("assignees/finalize/{formAssigneeId:guid}")]
    [AnyPermission("CanExecuteFormulaResponse", "CanPerformRoutineTest", "CanStartPackagingMaterialTest", "CanStartProductTest", "CanStartRawMaterialTest")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> SubmitFormAssigneeFinal(Guid formAssigneeId)
    {
        var result = await repository.SubmitFormAssigneeFinal(formAssigneeId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Reassigns form fields from one user to another or removes assignments.
    /// </summary>
    /// <param name="request">The ReassignFormAssigneeRequest object containing reassignment data.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("assignees/reassign")]
    [AnyPermission("CanAssignRoutineTest", "CanCreateRoutineTest", "CanReassignPackagingMaterialTest", "CanReassignProductTest", "CanReassignRawMaterialTest")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> ReassignFormAssignee([FromBody] ReassignFormAssigneeRequest request)
    {
        var result = await repository.ReassignFormAssignee(request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Submits a new form assignee with its field assignments.
    /// </summary>
    /// <param name="request">The CreateFormAssigneeRequest object containing form and field assignment data.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("assignees")]
    [AnyPermission("CanAssignPackagingMaterialTest", "CanAssignProductTest", "CanAssignRawMaterialTest", "CanAssignRoutineTest", "CanCreateRoutineTest", "CanStartPackagingMaterialTest", "CanStartProductTest", "CanStartRawMaterialTest")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> SubmitFormAssignee([FromBody] CreateFormAssigneeRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.SubmitFormAssignee(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves detailed information about a specific Form Assignee by its unique identifier.
    /// </summary>
    /// <param name="formAssigneeId">The unique identifier of the Form Assignee.</param>
    /// <returns>Returns the FormAssigneeDto if found, or a failure result.</returns>
    [HttpGet("assignees/{formAssigneeId:guid}")]
    [AnyPermission("CanAssignPackagingMaterialTest", "CanAssignProductTest", "CanAssignRawMaterialTest", "CanAssignRoutineTest", "CanCreateRoutineTest", "CanExecuteFormulaResponse", "CanPerformRoutineTest", "CanReassignPackagingMaterialTest", "CanReassignProductTest", "CanReassignRawMaterialTest", "CanStartPackagingMaterialTest", "CanStartProductTest", "CanStartRawMaterialTest")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetFormAssignee(Guid formAssigneeId)
    {
        var result = await repository.GetFormAssignee(formAssigneeId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a list of unique users assigned to a specific form.
    /// </summary>
    /// <param name="formId">The id of the form.</param>
    /// <returns>Returns a list of users assigned to the form.</returns>
    [HttpGet("assignees/{formId:guid}/users")]
    [AnyPermission("CanAssignPackagingMaterialTest", "CanAssignProductTest", "CanAssignRawMaterialTest", "CanAssignRoutineTest", "CanCreateRoutineTest", "CanReassignPackagingMaterialTest", "CanReassignProductTest", "CanReassignRawMaterialTest", "CanStartPackagingMaterialTest", "CanStartProductTest", "CanStartRawMaterialTest")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetFormAssigneeUsers(Guid formId)
    {
        var result = await repository.GetFormAssigneeUsers(formId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a Form Assignee associated with a specific Material Batch.
    /// </summary>
    /// <param name="materialBatchId">The unique identifier of the Material Batch.</param>
    /// <param name="stage">Optional test stage to disambiguate assignments when a batch has more than one.</param>
    /// <param name="productionActivityStepId">Optional production activity step to disambiguate assignments.</param>
    /// <returns>Returns the FormAssigneeDto if found, or a failure result.</returns>
    [HttpGet("assignees/material-batch/{materialBatchId:guid}")]
    [AnyPermission("CanCheckPackagingMaterialTestResult", "CanCheckRawMaterialTestResult", "CanReassignPackagingMaterialTest", "CanReassignRawMaterialTest", "CanStartPackagingMaterialTest", "CanStartRawMaterialTest", "CanViewPackagingMaterialGoodsReceiptNotes", "CanViewRawMaterialGoodsReceiptNotes", "CanViewResponseReworkTaskDetail", "CanViewResponseReworkTasks")]
    [Authorize]
    [ProducesResponseType(typeof(FormAssigneeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetFormAssigneeByBatch(
        Guid materialBatchId,
        [FromQuery] TestStage? stage,
        [FromQuery] Guid? productionActivityStepId
    )
    {
        var result = await repository.GetFormAssigneeByBatch(
            materialBatchId,
            stage,
            productionActivityStepId
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a Form Assignee associated with a specific Batch Manufacturing Record (BMR).
    /// </summary>
    /// <param name="bmrId">The unique identifier of the Batch Manufacturing Record.</param>
    /// <param name="stage">Optional test stage to disambiguate assignments when a BMR has more than one.</param>
    /// <param name="productionActivityStepId">Optional production activity step to disambiguate assignments.</param>
    /// <returns>Returns the FormAssigneeDto if found, or a failure result.</returns>
    [HttpGet("assignees/bmr/{bmrId:guid}")]
    [AnyPermission("CanAssignProductTest", "CanCheckProductTest", "CanReassignProductTest", "CanStartProductTest", "CanViewAnalyticalTestRequests", "CanViewAnalyticalTestRequestsForSampling", "CanViewProductAnalyticalTestRequests", "CanViewProductAnalyticalTestRequestsForAnalysing", "CanViewResponseReworkTaskDetail", "CanViewResponseReworkTasks")]
    [Authorize]
    [ProducesResponseType(typeof(FormAssigneeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetFormAssigneeByBmr(
        Guid bmrId,
        [FromQuery] TestStage? stage,
        [FromQuery] Guid? productionActivityStepId
    )
    {
        var result = await repository.GetFormAssigneeByBmr(bmrId, stage, productionActivityStepId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Returns a response ID given the form fields.
    /// </summary>
    /// <param name="request">The body of the response</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpGet("response")]
    [AnyPermission("CanAssignProductTest", "CanCheckProductTest", "CanReassignProductTest", "CanStartProductTest", "CanViewAnalyticalTestRequests", "CanViewAnalyticalTestRequestsForSampling", "CanViewProductAnalyticalTestRequests", "CanViewProductAnalyticalTestRequestsForAnalysing", "CanViewRoutineSetup", "CanViewRoutineTests", "CanViewWaterQuality")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid?))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetResponseId([FromQuery] GetResponseIdRequest request)
    {
        var result = await repository.GetResponseId(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Returns a form assignee ID given the form fields.
    /// </summary>
    /// <param name="request">The body of the response</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpGet("form-assignee")]
    [AnyPermission("CanViewRoutineSetup", "CanViewRoutineTests", "CanViewWaterQuality")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid?))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetFormAssigneeId([FromQuery] GetResponseIdRequest request)
    {
        var result = await repository.GetFormAssigneeId(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

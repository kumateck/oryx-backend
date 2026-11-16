using API.Authorization;
using APP.Extensions;
using APP.Services.Reviews;
using APP.Utils;
using DOMAIN.Entities.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Document review: the "Checked by" step between preparing a record and its approvals. Each action also checks,
/// on the record itself, the key for its stored kind; the attribute only refuses callers who hold none of them.
/// </summary>
[Route("api/v{version:apiVersion}/document-review")]
[ApiController]
public class DocumentReviewController(IDocumentReviewService reviews) : ControllerBase
{
    private Guid? Actor => Guid.TryParse(HttpContext.Items["Sub"] as string, out var id) ? id : null;

    /// <summary>The document types that can have a review step.</summary>
    [HttpGet("types")]
    [Authorize(PermissionKeys.CanViewDocumentReviewSettings)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<ReviewDocumentTypeDto>))]
    public IResult GetTypes() => TypedResults.Ok(reviews.Types());

    /// <summary>Who reviews each configured document type.</summary>
    [HttpGet("settings")]
    [Authorize(PermissionKeys.CanViewDocumentReviewSettings)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ReviewConfigurationDto>))]
    public async Task<IResult> GetSettings()
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await reviews.GetConfigurations(actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Creates or replaces the reviewers of a document type.</summary>
    [HttpPut("settings")]
    [Authorize(PermissionKeys.CanEditDocumentReviewSettings)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> SaveSettings([FromBody] SaveReviewConfigurationRequest request)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await reviews.SaveConfiguration(request, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>Removes a document type's review step. Records already in review stay in review.</summary>
    [HttpDelete("settings/{itemType}")]
    [Authorize(PermissionKeys.CanDeleteDocumentReviewSettings)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteSettings([FromRoute] string itemType)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await reviews.DeleteConfiguration(itemType, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>Records waiting for the caller's review, and records returned to the caller to correct.</summary>
    [HttpGet("pending")]
    [Authorize(PermissionKeys.CanViewPendingReviews)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<PendingReviewDto>>))]
    public async Task<IResult> GetPending([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await reviews.GetPending(actor, page, pageSize);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>A record's review, its history, its signatures and what the caller may do.</summary>
    [HttpGet("{modelType}/{modelId:guid}")]
    [AnyPermission(PermissionKeys.CanViewForeignPurchaseOrder, PermissionKeys.CanViewLocalPurchaseOrder,
        PermissionKeys.CanViewRawMaterialRequisitionsForProduction, PermissionKeys.CanViewPackagingMaterialRequisitionsForProduction,
        PermissionKeys.CanViewBillingSheet, PermissionKeys.CanViewStaffRequisition, PermissionKeys.CanViewOvertimeManagement, PermissionKeys.CanViewLeaveRequests,
        PermissionKeys.CanViewJobRequests, PermissionKeys.CanViewPackingList,
        PermissionKeys.CanViewRawMaterialRequisitions, PermissionKeys.CanViewPackagingMaterialRequisitions)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DocumentReviewStateDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetState([FromRoute] string modelType, [FromRoute] Guid modelId)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await reviews.GetState(modelType, modelId, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Confirms the review, which starts the record's approvals.</summary>
    [HttpPost("{modelType}/{modelId:guid}/confirm")]
    [AnyPermission(PermissionKeys.CanCheckForeignPurchaseOrder, PermissionKeys.CanCheckLocalPurchaseOrder,
        PermissionKeys.CanReviewRawMaterialPurchaseRequisition, PermissionKeys.CanReviewPackageMaterialPurchaseRequisition,
        PermissionKeys.CanReviewRawMaterialStockRequisition, PermissionKeys.CanReviewPackageMaterialStockRequisition,
        PermissionKeys.CanReviewBillingSheet, PermissionKeys.CanReviewStaffRequisition, PermissionKeys.CanReviewOvertimeRequest, PermissionKeys.CanReviewLeaveRequest,
        PermissionKeys.CanReviewJobRequest, PermissionKeys.CanReviewPackingList,
        PermissionKeys.CanCheckRawMaterialIssue, PermissionKeys.CanCheckPackagingMaterialIssue)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Confirm([FromRoute] string modelType, [FromRoute] Guid modelId, [FromBody] DocumentReviewActionRequest request)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await reviews.Confirm(modelType, modelId, request, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>Returns the record to its preparer for correction. A comment is required.</summary>
    [HttpPost("{modelType}/{modelId:guid}/return")]
    [AnyPermission(PermissionKeys.CanReturnForeignPurchaseOrderCheck, PermissionKeys.CanReturnLocalPurchaseOrderCheck,
        PermissionKeys.CanReturnRawMaterialPurchaseRequisition, PermissionKeys.CanReturnPackageMaterialPurchaseRequisition,
        PermissionKeys.CanReturnRawMaterialStockRequisition, PermissionKeys.CanReturnPackageMaterialStockRequisition,
        PermissionKeys.CanReturnBillingSheet, PermissionKeys.CanReturnStaffRequisition, PermissionKeys.CanReturnOvertimeRequest, PermissionKeys.CanReturnLeaveRequest,
        PermissionKeys.CanReturnJobRequest, PermissionKeys.CanReturnPackingList,
        PermissionKeys.CanReturnRawMaterialIssue, PermissionKeys.CanReturnPackagingMaterialIssue)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Return([FromRoute] string modelType, [FromRoute] Guid modelId, [FromBody] DocumentReviewActionRequest request)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await reviews.Return(modelType, modelId, request, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>Sends a corrected record back for review.</summary>
    [HttpPost("{modelType}/{modelId:guid}/resubmit")]
    [AnyPermission(PermissionKeys.CanCreateForeignPurchaseOrder, PermissionKeys.CanCreateLocalPurchaseOrder,
        PermissionKeys.CanEditRawMaterialPurchaseRequisition, PermissionKeys.CanEditPackageMaterialPurchaseRequisition,
        PermissionKeys.CanUpdateProductionStep,
        PermissionKeys.CanEditBillingSheet, PermissionKeys.CanEditStaffRequisition, PermissionKeys.CanEditOvertimeManagement, PermissionKeys.CanEditLeaveRequest,
        PermissionKeys.CanEditJobRequest, PermissionKeys.CanGeneratePackingList,
        PermissionKeys.CanRepairIssuedBom, PermissionKeys.CanRepairIssuedPackaging)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Resubmit([FromRoute] string modelType, [FromRoute] Guid modelId, [FromBody] DocumentReviewActionRequest request)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await reviews.Resubmit(modelType, modelId, request, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

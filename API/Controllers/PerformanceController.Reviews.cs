using APP.Extensions;
using APP.Utils;
using DOMAIN.Entities.Performance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public partial class PerformanceController
{
    [HttpPost("reviews")]
    [Authorize(PermissionKeys.CanManagePerformanceCycles)]
    public async Task<IResult> CreateReview([FromBody] CreatePerformanceReviewRequest request)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();

        var result = await repository.CreateReview(request, userId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("reviews")]
    [Authorize(PermissionKeys.CanViewPerformanceReviews)]
    public async Task<IResult> GetReviews(
        [FromQuery] Guid? employeeId, [FromQuery] Guid? cycleId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await repository.GetReviews(employeeId, cycleId, page, pageSize);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("reviews/{id:guid}")]
    [Authorize(PermissionKeys.CanViewPerformanceReviews)]
    public async Task<IResult> GetReview([FromRoute] Guid id)
    {
        var result = await repository.GetReview(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("reviews/{id:guid}/self-assessment")]
    [Authorize(PermissionKeys.CanSubmitSelfAssessment)]
    public async Task<IResult> SubmitSelfAssessment([FromRoute] Guid id, [FromBody] SubmitSelfAssessmentRequest request)
    {
        var result = await repository.SubmitSelfAssessment(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Submits the manager's assessment and moves the review to pending approval.
    /// </summary>
    [HttpPost("reviews/{id:guid}/manager-review")]
    [Authorize(PermissionKeys.CanSubmitManagerReview)]
    public async Task<IResult> SubmitManagerReview([FromRoute] Guid id, [FromBody] SubmitManagerReviewRequest request)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();

        var result = await repository.SubmitManagerReview(id, request, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

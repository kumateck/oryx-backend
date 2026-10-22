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

    /// <summary>The reviews the signed-in employee is reviewed in or reviews.</summary>
    [HttpGet("reviews/mine")]
    public async Task<IResult> GetMyReviews([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        if (!await Has(PermissionKeys.CanSubmitSelfAssessment) && !await Has(PermissionKeys.CanSubmitManagerReview)) return TypedResults.Forbid();
        if (await repository.EmployeeOf(userId) is not { } own) return TypedResults.Forbid();
        var result = await repository.GetReviewsFor(own, page, pageSize);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>A review, for HR reviewers or for the employee and reviewer taking part in it.</summary>
    [HttpGet("reviews/{id:guid}")]
    public async Task<IResult> GetReview([FromRoute] Guid id)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        if (await repository.ReviewParticipants(id) is not { } participants) return TypedResults.NotFound();
        if (!await Has(PermissionKeys.CanViewPerformanceReviews))
        {
            var own = await repository.EmployeeOf(userId);
            if (own == null || (own != participants.EmployeeId && own != participants.ReviewerId)) return TypedResults.Forbid();
        }
        var result = await repository.GetReview(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("reviews/{id:guid}/self-assessment")]
    [Authorize(PermissionKeys.CanSubmitSelfAssessment)]
    public async Task<IResult> SubmitSelfAssessment([FromRoute] Guid id, [FromBody] SubmitSelfAssessmentRequest request)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        if (await repository.EmployeeOf(userId) is not { } own)
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Employee.NotLinked", detail: "Your account is not linked to an employee record.");
        var result = await repository.SubmitSelfAssessment(id, request, own);
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

        if (await repository.EmployeeOf(userId) is not { } own)
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Employee.NotLinked", detail: "Your account is not linked to an employee record.");
        var result = await repository.SubmitManagerReview(id, request, userId, own);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

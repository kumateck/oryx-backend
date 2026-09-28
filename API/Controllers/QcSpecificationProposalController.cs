using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Specification proposals from worksheet import (build brief 08): the import screen stores
/// one set per saved file, and a reviewer turns Pending sets into a <b>Draft</b>
/// Specification or dismisses them.
/// <para>
/// Nothing here approves anything. Apply goes through the ordinary Specification
/// create/update, so the M2 lifecycle stays the only path to Effective.
/// </para>
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/qc/specification-proposals")]
[Authorize]
public class QcSpecificationProposalController(ISpecificationProposalRepository repository) : ControllerBase
{
    /// <summary>
    /// Stores one file's proposals. The save step of the import action itself, so it sits
    /// behind the import key rather than a key of its own.
    /// </summary>
    [HttpPost]
    [Authorize(QcWorksheetPermissionKeys.CanImportQcWorksheetTemplates)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SpecificationProposalSetDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> CreateProposalSet([FromBody] CreateSpecificationProposalSetRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.CreateProposalSet(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Lists proposal sets, newest first. Unpaginated: the review page groups the Pending ones by family.</summary>
    [HttpGet]
    [Authorize(QcWorksheetPermissionKeys.CanViewQcSpecificationProposals)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<SpecificationProposalSetSummaryDto>))]
    public async Task<IResult> GetProposalSets(
        [FromQuery] SpecificationProposalStatus? status = null,
        [FromQuery] ArdFamily? family = null)
    {
        var result = await repository.GetProposalSets(status, family);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Retrieves one proposal set, including its stored proposals.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(QcWorksheetPermissionKeys.CanViewQcSpecificationProposals)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SpecificationProposalSetDetailDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProposalSet([FromRoute] Guid id)
    {
        var result = await repository.GetProposalSet(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Builds the reviewable Specification draft plan for Pending sets of one family. Read-only.
    /// </summary>
    [HttpPost("draft")]
    [Authorize(QcWorksheetPermissionKeys.CanApplyQcSpecificationProposals)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SpecificationDraftPlan))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> BuildDraftPlan([FromBody] SpecificationProposalDraftRequest request)
    {
        var result = await repository.BuildDraftPlan(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Applies the reviewer-edited plan in one transaction and returns the Draft
    /// Specification's id.
    /// </summary>
    [HttpPost("apply")]
    [Authorize(QcWorksheetPermissionKeys.CanApplyQcSpecificationProposals)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SpecificationProposalApplyResult))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Apply([FromBody] SpecificationProposalApplyRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.Apply(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Dismisses a Pending set. A reason is required.</summary>
    [HttpPost("{id:guid}/dismiss")]
    [Authorize(QcWorksheetPermissionKeys.CanDismissQcSpecificationProposals)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SpecificationProposalSetDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Dismiss(
        [FromRoute] Guid id, [FromBody] SpecificationProposalDismissRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.Dismiss(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

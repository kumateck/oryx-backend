using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SHARED;

namespace API.Controllers;

/// <summary>
/// The formal OOS/OOT investigation workflow.
/// <para>
/// Three permission keys, three authorities. Investigating is a QC Officer's job, authorizing a
/// retest a QC Manager's, and signing the disposition that rejects or releases a real batch a QA
/// Manager's — so they are three separate keys rather than one, following the project's rule
/// that every action gets its own.
/// </para>
/// <para>
/// Reading is a fourth, <see cref="QcWorksheetPermissionKeys.CanViewQcOosCases"/>, and it has to
/// be: all three authorities above act on a case, and two of them — the retest authorizer and
/// the QA signer — held no key that let them load it. Gating the queue and the detail on
/// <see cref="QcWorksheetPermissionKeys.CanInvestigateQcOosCase"/> meant a QA Manager could sign
/// a disposition only by first being granted an investigation permission they have no business
/// holding. Same split, and the same reasoning, as the SamplingPointGroup view/manage pair.
/// </para>
/// <para>
/// This controller is entirely additive. The live <c>qa/oos-investigations</c> route and its
/// controller are untouched and keep working unchanged alongside it.
/// </para>
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/qc/worksheets/oos-cases")]
[Authorize]
public class QcOosCaseController(IOosCaseRepository repository) : ControllerBase
{
    /// <summary>The OOS queue, filterable by status.</summary>
    [HttpGet]
    [Authorize(QcWorksheetPermissionKeys.CanViewQcOosCases)]
    [ProducesResponseType(StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<OosCaseSummaryDto>>))]
    public async Task<IResult> GetOosCases(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null,
        [FromQuery] OosCaseStatus? status = null)
    {
        var result = await repository.GetOosCases(page, pageSize, searchQuery, status);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// The full case: the submitted value against the pinned Specification limit that triggered
    /// it, the investigation, and — once a retest exists — both worksheets side by side.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(QcWorksheetPermissionKeys.CanViewQcOosCases)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OosCaseDetailDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetOosCase([FromRoute] Guid id)
    {
        var result = await repository.GetOosCase(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Opens Phase 1 — and this is where the batch is actually quarantined, not at the case's
    /// automatic creation. Locking a batch out of use across Warehouse and Production is a
    /// decision someone makes, so it waits for this call.
    /// </summary>
    [HttpPost("{id:guid}/start-investigation")]
    [Authorize(QcWorksheetPermissionKeys.CanInvestigateQcOosCase)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OosCaseDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> StartInvestigation([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.StartInvestigation(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Records the Phase 1 findings. Editable only while the investigation is in progress.</summary>
    [HttpPut("{id:guid}/investigation")]
    [Authorize(QcWorksheetPermissionKeys.CanInvestigateQcOosCase)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OosCaseDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> UpdateInvestigation(
        [FromRoute] Guid id, [FromBody] UpdateOosInvestigationRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.UpdateInvestigation(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Authorizes a retest, creating a new linked worksheet. The original result is never
    /// overwritten — both stay visible, and the Specification's retest policy decides whether
    /// the retest runs on the same sample or a fresh one.
    /// </summary>
    [HttpPost("{id:guid}/authorize-retest")]
    [Authorize(QcWorksheetPermissionKeys.CanAuthorizeQcOosRetest)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OosCaseDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> AuthorizeRetest(
        [FromRoute] Guid id, [FromBody] AuthorizeOosRetestRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.AuthorizeRetest(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// The alternative to a retest: no lab error was found, so the case goes straight to QA. The
    /// disposition round opens immediately, since there is no retest completion to wait for.
    /// </summary>
    [HttpPost("{id:guid}/escalate")]
    [Authorize(QcWorksheetPermissionKeys.CanAuthorizeQcOosRetest)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OosCaseDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> Escalate(
        [FromRoute] Guid id, [FromBody] EscalateOosCaseRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.Escalate(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// The QA disposition: an electronic signature requiring the caller's own password in
    /// addition to a valid session, recorded in the same centralized QcApproval table as every
    /// other QC approval. Rejects or releases the real batch once every required stage has
    /// signed.
    /// </summary>
    [HttpPost("{id:guid}/disposition")]
    [Authorize(QcWorksheetPermissionKeys.CanDispositionQcOosCase)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OosCaseDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> Dispose(
        [FromRoute] Guid id, [FromBody] OosDispositionRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        var roleIds = (List<Guid>)HttpContext.Items["Roles"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.RecordDisposition(id, request, Guid.Parse(userId), roleIds ?? []);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

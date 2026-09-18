using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SHARED;

namespace API.Controllers;

/// <summary>
/// Certificates of Analysis and Environmental Monitoring Reports — one engine, two shapes.
/// <para>
/// <b>There is no create endpoint and no edit endpoint, deliberately.</b> A certificate is never
/// authored: the system generates a Draft the moment the round it certifies satisfies the
/// strict-hold rule — every required worksheet for every subject Reviewed, and no OOS case still
/// open. The only user actions are issuing a draft and revising an issued certificate, and each
/// gets its own permission key.
/// </para>
/// <para>
/// This controller is entirely additive, and its route sits under the rebuilt module's own
/// <c>qc/worksheets/</c> prefix. The live Material/Product/Packaging certificate path — the
/// <c>CommercialCertificate</c>/<c>RoutineCertificate</c> entities and their
/// <c>qc/commercial-certificates</c> and <c>qc/routines/certificates</c> routes — is neither
/// read, written, referenced nor routed over here, and keeps working unchanged alongside it.
/// </para>
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/qc/worksheets/certificates")]
[Authorize]
public class QcCoaController(ICoaRepository repository) : ControllerBase
{
    /// <summary>The certificate register, filterable by status, shape and issue-date range.</summary>
    [HttpGet]
    [Authorize(QcWorksheetPermissionKeys.CanViewQcCertificate)]
    [ProducesResponseType(StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<CoaSummaryDto>>))]
    public async Task<IResult> GetCertificates(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null,
        [FromQuery] CoaStatus? status = null,
        [FromQuery] CoaCertificateShape? shape = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var result = await repository.GetCoas(page, pageSize, searchQuery, status, shape, from, to);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// The full certificate: the shape's own header block — a Monitoring Report genuinely has no
    /// manufacturing or expiry date field, rather than blank ones — and the rows grouped by
    /// subject and section, in print order.
    /// <para>
    /// Every value returned was snapshotted when the certificate was generated. Nothing is
    /// re-derived from the Specification or the worksheets, so an issued certificate reads
    /// identically however long afterwards it is opened.
    /// </para>
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(QcWorksheetPermissionKeys.CanViewQcCertificate)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CoaDetailDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetCertificate([FromRoute] Guid id)
    {
        var result = await repository.GetCoa(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Issues a drafted certificate, which also locks the worksheets it draws on and releases the
    /// round.
    /// <para>
    /// No re-authentication wrapper, unlike the QC actions that carry one. Issuance is not itself
    /// an Approval-chain action: every worksheet review that gated this certificate's generation
    /// already went through the full re-authenticated Approval flow.
    /// </para>
    /// </summary>
    [HttpPost("{id:guid}/issue")]
    [Authorize(QcWorksheetPermissionKeys.CanIssueQcCertificate)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CoaDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> Issue([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.Issue(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Reissues a certificate with its rows recomputed from current data, under a mandatory
    /// reason.
    /// <para>
    /// An electronic signature: requires the caller's own password in addition to a valid session,
    /// recorded in the same centralized QcApproval table as every other QC signature. Issuance is
    /// the deliberate exception to that rule; this is not. Withdrawing a certificate that is
    /// already in circulation is a fresh decision by a named person, and it is signed for as one.
    /// </para>
    /// <para>
    /// The original is never overwritten or deleted. It stays fully retrievable, and only moves to
    /// Superseded once this replacement is itself issued — and a refused signature leaves it
    /// untouched.
    /// </para>
    /// </summary>
    [HttpPost("{id:guid}/revise")]
    [Authorize(QcWorksheetPermissionKeys.CanReviseQcCertificate)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CoaDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> Revise([FromRoute] Guid id, [FromBody] ReviseCoaRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.Revise(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

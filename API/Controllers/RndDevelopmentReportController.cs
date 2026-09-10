using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/rnd/projects/{rndProjectId:guid}/development-report")]
[Authorize]
public class RndDevelopmentReportController(IRndDevelopmentReportRepository repository) : ControllerBase
{
    /// <summary>
    /// Generates the R&amp;D development report PDF for a project, rolling up its
    /// formulation history, trial batches, analytical methods, stability data,
    /// and technology transfer status (structured along CTD Module 3.2.P.2 lines).
    /// </summary>
    [HttpGet]
    [Authorize(PermissionKeys.CanViewRndProjects)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GenerateDevelopmentReportPdf([FromRoute] Guid rndProjectId)
    {
        var result = await repository.GenerateDevelopmentReportPdf(rndProjectId);
        if (result.IsFailure) return result.ToProblemDetails();

        return TypedResults.File(result.Value, "application/pdf", $"rnd-development-report-{rndProjectId}.pdf");
    }
}

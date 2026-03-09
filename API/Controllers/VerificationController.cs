using APP.Extensions;
using APP.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SHARED.Requests;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/verification")]
[ApiController]
[Authorize]
public class VerificationController(IVerificationRepository repository) : ControllerBase
{
    /// <summary>
    /// Verifies an entity (Product, Specification, or ARD).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> VerifyEntity([FromBody] VerifyRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.VerifyEntity(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

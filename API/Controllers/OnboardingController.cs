using APP.Extensions;
using APP.IRepository;
using DOMAIN.Entities.Employees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// The public onboarding form. Every call carries the invitation token from the emailed link; nothing here works without a
/// current, unused invitation, and the token travels in the body so it is not written to request logs.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/onboarding")]
[AllowAnonymous]
public class OnboardingController(IEmployeeRepository repository) : ControllerBase
{
    /// <summary>Who the invitation is for, if the token is still valid.</summary>
    [HttpPost("invitation")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OnboardingInvitationDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetInvitation([FromBody] OnboardingTokenRequest request)
    {
        var result = await repository.GetOnboardingInvitation(request.Token);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Submits the onboarding form once; the email and employee type are the invitation's.</summary>
    [HttpPost("submit")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> Submit([FromBody] CompleteOnboardingRequest request)
    {
        var result = await repository.CompleteOnboarding(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Adds the passport photograph shortly after the form was submitted.</summary>
    [HttpPost("photo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> UploadPhoto([FromBody] OnboardingAvatarRequest request)
    {
        var result = await repository.UploadOnboardingAvatar(request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

using APP.Extensions;
using APP.IRepository;
using DOMAIN.Entities.JobRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/job-requests")]
[Authorize]
public class JobRequestController(IJobRequestRepository repository) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateJobRequest([FromBody] CreateJobRequest request)
    {
        var result = await repository.CreateJobRequest(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    /// <summary>
    /// Retrieves a list of job requests.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<JobRequest>))]
    public async Task<IResult> GetJobRequests()
    {
        var result = await repository.GetJobRequests();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
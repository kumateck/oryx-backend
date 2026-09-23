using APP.Extensions;
using APP.IRepository;
using DOMAIN.Entities.QualityRoutines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/qc/routines")]
[Authorize]
public class RoutineQcController(IRoutineQcRepository repository) : ControllerBase
{
    private Guid? ActorId => Guid.TryParse(HttpContext.Items["Sub"] as string, out var id) ? id : null;

    [HttpPost("ards")]
    public async Task<IResult> CreateArd([FromBody] CreateRoutineArdRequest request)
    {
        if (ActorId is not Guid actor) return TypedResults.Unauthorized();
        var result = await repository.CreateArd(request, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("ards")]
    public async Task<IResult> ListArds()
    {
        var result = await repository.ListArds();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("definitions")]
    public async Task<IResult> ListDefinitions()
    {
        var result = await repository.ListDefinitions();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("definitions")]
    public async Task<IResult> CreateDefinition([FromBody] CreateRoutineDefinitionRequest request)
    {
        if (ActorId is not Guid actor) return TypedResults.Unauthorized();
        var result = await repository.CreateDefinition(request, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    public async Task<IResult> CreateExecution([FromBody] CreateRoutineExecutionRequest request)
    {
        if (ActorId is not Guid actor) return TypedResults.Unauthorized();
        var result = await repository.CreateExecution(request, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet]
    public async Task<IResult> List()
    {
        var result = await repository.ListExecutions();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{id:guid}")]
    public async Task<IResult> Detail([FromRoute] Guid id)
    {
        var result = await repository.GetExecution(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/samples")]
    public async Task<IResult> AddSample([FromRoute] Guid id,
        [FromBody] CreateRoutineSampleRequest request)
    {
        if (ActorId is not Guid actor) return TypedResults.Unauthorized();
        var result = await repository.AddSample(id, request, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("tracks/{id:guid}")]
    public async Task<IResult> Track([FromRoute] Guid id)
    {
        var result = await repository.GetTrack(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("certificates/{id:guid}")]
    public async Task<IResult> Certificate([FromRoute] Guid id)
    {
        var result = await repository.GetCertificate(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("tracks/{trackId:guid}/submit-approval")]
    public async Task<IResult> SubmitTrackForApproval([FromRoute] Guid trackId)
    {
        if (ActorId is not Guid actor) return TypedResults.Unauthorized();
        var result = await repository.SubmitTrackForApproval(trackId, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpPost("samples/{sampleId:guid}/certificate")]
    public async Task<IResult> GenerateCertificate([FromRoute] Guid sampleId)
    {
        if (ActorId is not Guid actor) return TypedResults.Unauthorized();
        var result = await repository.GenerateCertificate(sampleId, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

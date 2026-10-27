using APP.Extensions;
using APP.Services.QcWorksheets;
using APP.IRepository;
using AutoMapper;
using DOMAIN.Entities.Forms;
using INFRASTRUCTURE.Context;
using APP.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SHARED;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/qc/rework-tasks")]
public sealed class QcResponseReworkController(IResponseReworkService rework,
    ApplicationDbContext context, IFormRepository forms, IMapper mapper) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ResponseReworkTaskDto>))]
    [Authorize(QcWorksheetPermissionKeys.CanViewResponseReworkTasks)]
    public async Task<IResult> List()
    {
        var result = await rework.ListAsync();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseReworkDetailDto))]
    [Authorize(QcWorksheetPermissionKeys.CanViewResponseReworkTaskDetail)]
    public async Task<IResult> Get(Guid id)
    {
        var result = await rework.GetAsync(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/start")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [Authorize(QcWorksheetPermissionKeys.CanStartResponseReworkTask)]
    public async Task<IResult> Start(Guid id)
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"] as string, out var actorId))
            return TypedResults.Unauthorized();
        var result = await rework.StartAsync(id, actorId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{id:guid}/form")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FormDto))]
    [Authorize(QcWorksheetPermissionKeys.CanViewResponseReworkTaskDetail)]
    public async Task<IResult> Form(Guid id)
    {
        var result = await ResponseReworkForm.Read(context, forms, mapper, id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

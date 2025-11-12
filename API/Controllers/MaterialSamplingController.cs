using APP.Extensions;
using APP.IRepository;
using DOMAIN.Entities.Checklists;
using DOMAIN.Entities.MaterialSampling;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/material-samplings")]
[Authorize]
public class MaterialSamplingController(IMaterialSamplingRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a sampling material
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateMaterialSampling([FromBody] CreateMaterialSamplingRequest request)
    {
        var result = await repository.CreateMaterialSampling(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Adds an issue number to sampling material
    /// </summary>
    [HttpPut("{materialSamplingId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> AddIssueNumberToMaterialSampling([FromRoute] Guid materialSamplingId, 
        [FromQuery] string issueNumber)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await 
            repository.AddIssueNumberToMaterialSample(materialSamplingId, issueNumber, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the details of a sampling material by its ID.
    /// </summary>
    [HttpGet("{grnId:guid}/{batchId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MaterialSamplingDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMaterialSamplingByMaterialId([FromRoute] Guid grnId, [FromRoute] Guid batchId)
    {
        var result = await repository.GetMaterialSamplingByGrnAndBatch(grnId, batchId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Creates a new pre-sample checklist.
    /// </summary>
    [HttpPost("pre-sample")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreatePreSampleChecklist([FromBody] CreatePreSampleChecklistRequest request)
    {
        var result = await repository.CreatePreSampleChecklist(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a pre-sample checklist by GRN and Batch.
    /// </summary>
    [HttpGet("pre-sample/{grnId:guid}/{batchId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PreSampleChecklistDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPreSampleChecklistByGrnAndBatch([FromRoute] Guid grnId, [FromRoute] Guid batchId)
    {
        var result = await repository.GetPreSampleChecklistByGrnAndBatch(grnId, batchId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
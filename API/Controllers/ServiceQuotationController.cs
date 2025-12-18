using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.JobRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/service-quotations")]
[Authorize]
public class ServiceQuotationController(IServiceQuotationRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new service quotation
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateServiceQuotation([FromBody] CreateServiceQuotationRequest request)
    {
        var result = await repository.CreateServiceQuotation(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of service quotations
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ServiceQuotationDto>>))]
    public async Task<IResult> GetServiceQuotations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] QuotationStatus? status = null,
        [FromQuery] Guid? jobOrderId = null,
        [FromQuery] Guid? serviceProviderId = null)
    {
        var result = await repository.GetServiceQuotations(page, pageSize, status, jobOrderId, serviceProviderId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a service quotation by its unique identifier
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ServiceQuotationDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetServiceQuotation([FromRoute] Guid id)
    {
        var result = await repository.GetServiceQuotation(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates an existing service quotation
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateServiceQuotation([FromRoute] Guid id, [FromBody] UpdateServiceQuotationRequest request)
    {
        var result = await repository.UpdateServiceQuotation(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Negotiates quotation pricing
    /// </summary>
    [HttpPost("negotiate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> NegotiateQuotation([FromBody] NegotiateQuotationRequest request)
    {
        var result = await repository.NegotiateQuotation(request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Compares quotations for a job order side-by-side
    /// </summary>
    [HttpPost("compare")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ServiceQuotationDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CompareQuotations([FromBody] CompareQuotationsRequest request)
    {
        var result = await repository.CompareQuotations(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}


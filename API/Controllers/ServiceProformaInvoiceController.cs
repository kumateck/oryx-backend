using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.JobRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/service-proforma-invoices")]
[Authorize]
public class ServiceProformaInvoiceController(IServiceProformaInvoiceRepository repository) : ControllerBase
{
    /// <summary>
    /// Requests a proforma invoice from the selected contractor
    /// </summary>
    [HttpPost("request")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> RequestServiceProformaInvoice([FromBody] RequestServiceProformaInvoiceRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.RequestServiceProformaInvoice(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Contractor responds with proforma invoice
    /// </summary>
    [HttpPost("respond")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> RespondServiceProformaInvoice([FromBody] RespondServiceProformaInvoiceRequest request)
    {
        var result = await repository.RespondServiceProformaInvoice(request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Approves a proforma invoice response
    /// </summary>
    [HttpPost("approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ApproveServiceProformaInvoice([FromBody] ApproveServiceProformaInvoiceRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        request.ApprovedById = Guid.Parse(userId);
        var result = await repository.ApproveServiceProformaInvoice(request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of service proforma invoices
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ServiceProformaInvoiceDto>>))]
    public async Task<IResult> GetServiceProformaInvoices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] ServiceProformaInvoiceStatus? status = null,
        [FromQuery] Guid? jobOrderId = null,
        [FromQuery] Guid? serviceProviderId = null)
    {
        var result = await repository.GetServiceProformaInvoices(page, pageSize, status, jobOrderId, serviceProviderId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a service proforma invoice by its unique identifier
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ServiceProformaInvoiceDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetServiceProformaInvoice([FromRoute] Guid id)
    {
        var result = await repository.GetServiceProformaInvoice(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}


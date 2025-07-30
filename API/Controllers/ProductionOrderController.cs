using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Invoices;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.ProformaInvoices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{Version:apiVersion}/production-orders")]
[Authorize]
public class ProductionOrderController(IProductionOrderRepository repository) : ControllerBase
{
    // ----------------------------
    // Production Order Endpoints
    // ----------------------------

    /// <summary>
    /// Creates a production order
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateProductionOrder([FromBody] CreateProductionOrderRequest request)
    {
        var result = await repository.CreateProductionOrder(request);
        return result.IsSuccess ? TypedResults.Ok(result) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of production orders.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ProductionOrderDto>>))]
    public async Task<IResult> GetProductionOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetProductionOrders(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a production order by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductionOrderDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProductionOrder([FromRoute] Guid id)
    {
        var result = await repository.GetProductionOrder(id);
        return result.IsSuccess ? TypedResults.Ok(result) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Updates a production order by its ID.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(ProductionOrderDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateProductionOrder([FromRoute] Guid id, [FromBody] CreateProductionOrderRequest request)
    {
        var result = await repository.UpdateProductionOrder(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Deletes a production order by its ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteProductionOrder([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteProductionOrder(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result) : result.ToProblemDetails();
    }

    // ----------------------------
    // Proforma Invoice Endpoints
    // ----------------------------

    /// <summary>
    /// Creates a proforma invoice.
    /// </summary>
    [HttpPost("proforma-invoices")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateProformaInvoice([FromBody] CreateProformaInvoice request)
    {
        var result = await repository.CreateProformaInvoice(request);
        return result.IsSuccess ? TypedResults.Ok(result) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of proforma invoices.
    /// </summary>
    [HttpGet("proforma-invoices")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ProformaInvoiceDto>>))]
    public async Task<IResult> GetProformaInvoices([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetProformaInvoices(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a proforma invoice by its ID.
    /// </summary>
    [HttpGet("proforma-invoices/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProformaInvoiceDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProformaInvoice([FromRoute] Guid id)
    {
        var result = await repository.GetProformaInvoice(id);
        return result.IsSuccess ? TypedResults.Ok(result) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a proforma invoice by its ID.
    /// </summary>
    [HttpPut("proforma-invoices/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateProformaInvoice([FromRoute] Guid id, [FromBody] CreateProformaInvoice request)
    {
        var result = await repository.UpdateProformaInvoice(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a proforma invoice by its ID.
    /// </summary>
    [HttpDelete("proforma-invoices/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteProformaInvoice([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteProformaInvoice(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Creates an invoice.
    /// </summary>
    [HttpPost("invoices")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateInvoice([FromBody] CreateInvoice request)
    {
        var result = await repository.CreateInvoice(request);
        return result.IsSuccess ? TypedResults.Ok(result) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets a paginated list of invoices.
    /// </summary>
    [HttpGet("invoices")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<InvoiceDto>>))]
    public async Task<IResult> GetInvoices([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetInvoices(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets an invoice by ID.
    /// </summary>
    [HttpGet("invoices/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(InvoiceDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetInvoice([FromRoute] Guid id)
    {
        var result = await repository.GetInvoice(id);
        return result.IsSuccess ? TypedResults.Ok(result) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates an invoice.
    /// </summary>
    [HttpPut("invoices/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateInvoice([FromRoute] Guid id, [FromBody] CreateInvoice request)
    {
        var result = await repository.UpdateInvoice(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes an invoice.
    /// </summary>
    [HttpDelete("invoices/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteInvoice([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteInvoice(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result) : result.ToProblemDetails();
    }
}
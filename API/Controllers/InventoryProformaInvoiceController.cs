// using APP.Extensions;
// using APP.IRepository;
// using APP.Utils;
// using DOMAIN.Entities.Invoices;
// using DOMAIN.Entities.ProformaInvoices;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
//
// namespace API.Controllers;
//
// [ApiController]
// [Route("api/v{version:apiVersion}/inventory-proforma-invoice")]
// [Authorize]
// public class InventoryProformaInvoiceController(IInventoryProformaInvoiceRepository repository) : ControllerBase
// {
//
//     /// <summary>
//     /// Creates a proforma invoice.
//     /// </summary>
//     [HttpPost("proforma-invoice")]
//     [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
//     [ProducesResponseType(StatusCodes.Status400BadRequest)]
//     public async Task<IResult> CreateProformaInvoice([FromBody] CreateInventoryProformaInvoice request)
//     {
//         var result = await repository.CreateProformaInvoice(request);
//         return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
//     }
//
//     /// <summary>
//     /// Sends the proforma invoice to the customer
//     /// </summary>
//     [HttpPost("proforma-invoice/{proformaInvoiceId:guid}/customer")]
//     [ProducesResponseType(StatusCodes.Status204NoContent)]
//     [ProducesResponseType(StatusCodes.Status400BadRequest)]
//     public async Task<IResult> SendProformaInvoiceToCustomer([FromRoute] Guid proformaInvoiceId)
//     {
//         var userId = (string)HttpContext.Items["Sub"];
//         if (userId == null) return TypedResults.Unauthorized();
//
//         var result = await repository.SendProformaInvoiceToCustomer(proformaInvoiceId, Guid.Parse(userId));
//         return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
//     }
//
//     /// <summary>
//     /// Retrieves a paginated list of proforma invoices.
//     /// </summary>
//     [HttpGet("proforma-invoice")]
//     [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ProformaInvoiceDto>>))]
//     public async Task<IResult> GetProformaInvoices(
//         [FromQuery] ProformaInvoiceStatus? proformaInvoiceStatus,
//         [FromQuery] int page = 1,
//         [FromQuery] int pageSize = 10,
//         [FromQuery] string searchQuery = null,
//         [FromQuery] bool? approved = null)
//     {
//         var result = await repository.GetProformaInvoices(page,
//             pageSize,
//             searchQuery,
//             proformaInvoiceStatus,
//             approved);
//         return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
//     }
//
//     /// <summary>
//     /// Retrieves a proforma invoice by its ID.
//     /// </summary>
//     [HttpGet("proforma-invoice/{proformaInvoiceId:guid}")]
//     [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProformaInvoiceDto))]
//     [ProducesResponseType(StatusCodes.Status404NotFound)]
//     public async Task<IResult> GetProformaInvoice([FromRoute] Guid proformaInvoiceId)
//     {
//         var result = await repository.GetProformaInvoice(proformaInvoiceId);
//         return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
//     }
//
//     /// <summary>
//     /// Updates a proforma invoice by its ID.
//     /// </summary>
//     [HttpPut("proforma-invoice/{id:guid}")]
//     [ProducesResponseType(StatusCodes.Status204NoContent)]
//     [ProducesResponseType(StatusCodes.Status400BadRequest)]
//     [ProducesResponseType(StatusCodes.Status404NotFound)]
//     public async Task<IResult> UpdateProformaInvoice([FromRoute] Guid id, [FromBody] CreateProformaInvoice request)
//     {
//         var result = await repository.UpdateProformaInvoice(id, request);
//         return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
//     }
//
//     /// <summary>
//     /// Deletes a proforma invoice by its ID.
//     /// </summary>
//     [HttpDelete("proforma-invoice/{id:guid}")]
//     [ProducesResponseType(StatusCodes.Status204NoContent)]
//     [ProducesResponseType(StatusCodes.Status404NotFound)]
//     public async Task<IResult> DeleteProformaInvoice([FromRoute] Guid id)
//     {
//         var userId = (string)HttpContext.Items["Sub"];
//         if (userId == null) return TypedResults.Unauthorized();
//
//         var result = await repository.DeleteProformaInvoice(id, Guid.Parse(userId));
//         return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
//     }
//
//     // ----------------------------
//     // Invoice Endpoints
//     // ----------------------------
//
//     /// <summary>
//     /// Creates an invoice.
//     /// </summary>
//     [HttpPost("invoice")]
//     [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
//     [ProducesResponseType(StatusCodes.Status400BadRequest)]
//     public async Task<IResult> CreateInvoice([FromBody] CreateInvoice request)
//     {
//         var result = await repository.CreateInvoice(request);
//         return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
//     }
//
//     /// <summary>
//     /// Gets a paginated list of invoices.
//     /// </summary>
//     [HttpGet("invoice")]
//     [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<InvoiceDto>>))]
//     public async Task<IResult> GetInvoices([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
//     {
//         var result = await repository.GetInvoices(page, pageSize, searchQuery);
//         return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
//     }
//
//     /// <summary>
//     /// Gets an invoice by ID.
//     /// </summary>
//     [HttpGet("invoice/{id:guid}")]
//     [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(InvoiceDto))]
//     [ProducesResponseType(StatusCodes.Status404NotFound)]
//     public async Task<IResult> GetInvoice([FromRoute] Guid id)
//     {
//         var result = await repository.GetInvoice(id);
//         return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
//     }
//
//     /// <summary>
//     /// Updates an invoice.
//     /// </summary>
//     [HttpPut("invoice/{id:guid}")]
//     [ProducesResponseType(StatusCodes.Status204NoContent)]
//     [ProducesResponseType(StatusCodes.Status400BadRequest)]
//     [ProducesResponseType(StatusCodes.Status404NotFound)]
//     public async Task<IResult> UpdateInvoice([FromRoute] Guid id, [FromBody] CreateInvoice request)
//     {
//         var result = await repository.UpdateInvoice(id, request);
//         return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
//     }
//
//     /// <summary>
//     /// Deletes an invoice.
//     /// </summary>
//     [HttpDelete("invoice/{id:guid}")]
//     [ProducesResponseType(StatusCodes.Status204NoContent)]
//     [ProducesResponseType(StatusCodes.Status404NotFound)]
//     public async Task<IResult> DeleteInvoice([FromRoute] Guid id)
//     {
//         var userId = (string)HttpContext.Items["Sub"];
//         if (userId == null) return TypedResults.Unauthorized();
//
//         var result = await repository.DeleteInvoice(id, Guid.Parse(userId));
//         return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
//     }
// }
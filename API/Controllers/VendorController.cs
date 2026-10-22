using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Vendors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using APP.Services.Stores;

namespace API.Controllers;
[ApiController]
[Route("api/v{version:apiVersion}/vendors")]
[Authorize]
public class VendorController(IVendorRepository repository, INFRASTRUCTURE.Context.ApplicationDbContext context) : ControllerBase
{
    /// <summary>
    /// Creates a vendor
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateVendor([FromBody] CreateVendorRequest request)
    {
        if (!await ServiceWorkflowAccess.All(context, Actor(), "CanViewVendors", "CanCreateVendor")) return TypedResults.Forbid();

        var result = await repository.CreateVendor(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of vendors 
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<VendorDto>>))]
    public async Task<IResult> GetVendors([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null)
    {
        if (!await ServiceWorkflowAccess.Any(context, Actor(), "CanViewVendors", "CanViewPurchaseRequisitionsSourcing")) return TypedResults.Forbid();

        var result = await repository.GetVendors(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a vendor by its unique identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VendorDto))]
    public async Task<IResult> GetVendor([FromRoute] Guid id)
    {
        if (!await ServiceWorkflowAccess.Any(context, Actor(), "CanViewVendors", "CanViewPurchaseRequisitionsSourcing", "CanViewQuotationRequestsForTrustedVendors", "CanViewQuotationResponsesForTrustedVendors")) return TypedResults.Forbid();

        var result = await repository.GetVendor(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates an existing vendor.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(VendorDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateVendor([FromRoute] Guid id, [FromBody] CreateVendorRequest request)
    {
        if (!await ServiceWorkflowAccess.All(context, Actor(), "CanViewVendors", "CanCreateVendor")) return TypedResults.Forbid();

        var result = await repository.UpdateVendor(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a vendor using the specified supplier ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteVendor([FromRoute] Guid id)
    {
        if (!await ServiceWorkflowAccess.All(context, Actor(), "CanViewVendors", "CanDeleteVendor")) return TypedResults.Forbid();

        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteVendor(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a list of vendors based on an item
    /// </summary>
    [HttpGet("item/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<VendorDto>))]
    public async Task<IResult> GetItems([FromRoute] Guid id)
    {
        if (!await ServiceWorkflowAccess.Any(context, Actor(), "CanViewVendors", "CanViewPurchaseRequisitionsSourcing")) return TypedResults.Forbid();

        var result = await repository.GetVendorsByItem(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private Guid? Actor() => Guid.TryParse(HttpContext.Items["Sub"] as string, out var id) ? id : null;
}

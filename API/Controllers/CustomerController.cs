using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{Version:apiVersion}/customers")]
[Authorize]
public class CustomerController(ICustomerRepository repository) : ControllerBase
{

    /// <summary>
    /// Creates a customer
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateCustomer([FromBody] CreateCustomerRequest request)
    {
        var result = await repository.CreateCustomer(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of customers
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK,  Type = typeof(Paginateable<IEnumerable<CustomerDto>>))]
    public async Task<IResult> GetCustomers([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetCustomers(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the details of a customer by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK,  Type = typeof(CustomerDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetCustomer([FromRoute] Guid id)
    {
        var result = await repository.GetCustomer(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Updates the details of a customer by its ID.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(CustomerDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateCustomer([FromRoute] Guid id, [FromBody] CreateCustomerRequest request)
    {
        var result = await repository.UpdateCustomer(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Deletes the details of a customer by its ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteCustomer([FromRoute] Guid id)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteCustomer(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
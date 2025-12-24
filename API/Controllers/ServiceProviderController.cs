using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.ServiceProviders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Service Provider Management API
/// </summary>
/// <remarks>
/// Manages service providers/contractors that can be used for external job orders. Service providers 
/// are external companies or individuals that can perform services. They can be associated with 
/// multiple services and can submit quotations for job orders.
/// </remarks>
[ApiController]
[Route("api/v{version:apiVersion}/service-providers")]
[Authorize]
[Tags("Service Providers")]
public class ServiceProviderController(IServiceProviderRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new service provider
    /// </summary>
    /// <remarks>
    /// Creates a service provider (contractor) that can be used when sending job orders externally. 
    /// Service providers can be associated with multiple services and will receive RFQs for job orders.
    /// </remarks>
    /// <param name="request">Service provider creation details</param>
    /// <returns>Returns the ID of the created service provider</returns>
    /// <response code="200">Service provider created successfully</response>
    /// <response code="400">Invalid request data or provider already exists</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateServiceProvider([FromBody] CreateServiceProviderRequest request)
    {
        var result = await repository.CreateServiceProvider(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of service providers
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ServiceProviderDto>>))]
    public async Task<IResult> GetServiceProviders([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetServiceProviders(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a service provider by its unique identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ServiceProviderDto))]
    public async Task<IResult> GetServiceProvider([FromRoute] Guid id)
    {
        var result = await repository.GetServiceProvider(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates an existing service provider.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(ServiceProviderDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateServiceProvider([FromRoute] Guid id, [FromBody] CreateServiceProviderRequest request)
    {
        var result = await repository.UpdateServiceProvider(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a service provider using the specified service ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteServiceProvider([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteServiceProvider(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
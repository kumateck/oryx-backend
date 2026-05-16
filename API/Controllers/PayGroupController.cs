using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.PayGroups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/payroll/pay-groups")]
[ApiController]
[Authorize]
public class PayGroupController(IPayGroupRepository repository) : ControllerBase
{
    
    /// <summary>
    /// Creates a new pay group.
    /// </summary>
    /// <param name="request">The CreatePayGroup object.</param>
    /// <returns>Returns the ID of the created record.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreatePayGroup([FromBody] CreatePayGroupRequest request)
    {
        var result = await repository.CreatePayGroup(request);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific pay group by ID.
    /// </summary>
    /// <param name="payGroupId">The ID of the record.</param>
    /// <returns>Returns the entity details.</returns>
    [HttpGet("{payGroupId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PayGroupDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPayGroup(
        [FromRoute] Guid payGroupId)
    {
        var result = await repository.GetPayGroup(payGroupId);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of pay groups.
    /// </summary>
    /// <param name="page">Current page number.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="searchQuery">Optional search query.</param>
    /// <returns>Returns paginated results.</returns>
    [HttpGet]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<PayGroupDto>>)
    )]
    public async Task<IResult> GetPayGroups(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetPayGroups(
            page,
            pageSize,
            searchQuery);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific PayGroup.
    /// </summary>
    /// <param name="request">The update request object.</param>
    /// <param name="payGroupId">The ID of the entity.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("{payGroupId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdatePayGroup(
        [FromBody] CreatePayGroupRequest request,
        [FromRoute] Guid payGroupId)
    {
        var result = await repository.UpdatePayGroup(
            payGroupId,
            request);

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific PayGroup.
    /// </summary>
    /// <param name="payGroupId">The ID of the entity to delete.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpDelete("{payGroupId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeletePayGroup(
        [FromRoute] Guid payGroupId)
    {
        var userId = (string)HttpContext.Items["Sub"];

        if (userId is null)
            return TypedResults.Unauthorized();

        var result = await repository.DeletePayGroup(
            payGroupId,
            Guid.Parse(userId));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.ToProblemDetails();
    }
}
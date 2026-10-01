using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/role")]
[ApiController]
[Authorize]
public class RoleController(IRoleRepository repo) : ControllerBase
{
    [HttpGet]
    [Authorize(PermissionKeys.CanViewRoles)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<RoleDto>))]
    public async Task<IResult> GetRoles()
    {
        var response = await repo.GetRoles();
        return response.IsSuccess ? TypedResults.Ok(response.Value) : response.ToProblemDetails();
    }

    [HttpGet("with-permissions")]
    [Authorize(PermissionKeys.CanViewRoles)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<RolePermissionDto>>))]
    public async Task<IResult> GetRolesWithPermissions([FromQuery(Name = "page")] int page = 1,
        [FromQuery(Name = "pageSize")] int pageSize = 5,
        [FromQuery(Name = "searchQuery")] string searchQuery = null)
    {
        var response = await repo.GetRolesWithPermissions(page, pageSize, searchQuery);
        return response.IsSuccess ? TypedResults.Ok(response.Value) : response.ToProblemDetails();
    }

    //[Authorize]
    [HttpGet("{id}")]
    [Authorize(PermissionKeys.CanViewRoles)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RolePermissionDto))]
    public async Task<IResult> GetRole(Guid id)
    {
        try
        {
            var response = await repo.GetRole(id);
            return response.IsSuccess ? TypedResults.Ok(response.Value) : response.ToProblemDetails();
        }
        catch (Exception e)
        {
            return TypedResults.NotFound(e.Message);
        }
    }

    //[Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> CreateRole(CreateRoleRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var response = await repo.CreateRole(request, Guid.Parse(userId));
        return response.IsSuccess ? TypedResults.NoContent() : response.ToProblemDetails();
    }

    //[Authorize]
    [HttpPut("{id}")]
    [Authorize(PermissionKeys.CanEditRole)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> UpdateRole(UpdateRoleRequest request, Guid id)
    {
        try
        {
            var userId = (string)HttpContext.Items["Sub"];
            if (userId == null) return TypedResults.Unauthorized();

            var response = await repo.UpdateRole(request, id, Guid.Parse(userId));
            return response.IsSuccess ? TypedResults.NoContent() : response.ToProblemDetails();
        }
        catch (Exception e)
        {
            return TypedResults.NotFound(e.Message);
        }
    }

    //[Authorize]
    [HttpGet("check/{id}")]
    [Authorize(PermissionKeys.CanDeleteRole)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RoleUsageDto))]
    public async Task<IResult> CheckRole(Guid id)
    {
        var response = await repo.CheckRole(id);
        return response.IsSuccess ? TypedResults.Ok(response.Value) : response.ToProblemDetails();
    }

    //[Authorize]
    [HttpDelete("{id}")]
    [Authorize(PermissionKeys.CanDeleteRole)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> DeleteRole(Guid id)
    {
        try
        {
            var userId = (string)HttpContext.Items["Sub"];
            if (userId == null) return TypedResults.Unauthorized();

            var response = await repo.DeleteRole(id, Guid.Parse(userId));
            return response.IsSuccess ? TypedResults.NoContent() : response.ToProblemDetails();
        }
        catch (Exception e)
        {
            return TypedResults.NotFound(e.Message);
        }
    }
}

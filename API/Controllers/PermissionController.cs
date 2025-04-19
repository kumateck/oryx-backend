using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/permission")]
[ApiController]
public class PermissionController(IPermissionRepository repo) : ControllerBase
{
    /// <summary>
    /// Retrieves all permission modules in the system.
    /// </summary>
    [HttpGet("modules")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PermissionModuleDto>))]
    public IResult GetAllPermissionModules()
    {
        var result = repo.GetAllPermissionInSystem();
        return TypedResults.Ok(result);
    }

    /// <summary>
    /// Retrieves all permissions in the system.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PermissionDto>))]
    public IResult GetAllPermissions()
    {
        var result = repo.GetAllPermissions();
        return TypedResults.Ok(result);
    }

    /// <summary>
    /// Retrieves all permission modules assigned to a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    [HttpGet("user/{userId?}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<PermissionModuleDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetPermissionsForUser(Guid? userId)
    {
        var id = (string)HttpContext.Items["Sub"];
        if (id == null) return TypedResults.Unauthorized();
        
        var result = await repo.GetAllPermissionForUser(userId ?? Guid.Parse(id));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves all permission modules assigned to a role.
    /// </summary>
    /// <param name="roleId">The role ID.</param>
    [HttpGet("role/{roleId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<PermissionModuleDto>))]
    public async Task<IResult> GetPermissionsByRole(Guid roleId)
    {
        var result = await repo.GetPermissionByRole(roleId);
        return TypedResults.Ok(result);
    }

    /// <summary>
    /// Updates permissions assigned to a role.
    /// </summary>
    /// <param name="roleId">The role ID.</param>
    /// <param name="permissions">List of permission identifiers.</param>
    [HttpPut("role/{roleId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> UpdateRolePermissions(Guid roleId, [FromBody] List<PermissionModuleDto> permissions)
    {
        var result = await repo.UpdateRolePermissions(permissions, roleId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Gets filtered menu based on user role
    /// </summary>
    [HttpGet("menu")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof (List<MenuItem>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetFilteredMenu()
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repo.GetFilteredMenu(Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

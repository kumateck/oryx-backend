using API.Authorization;
using APP.Services.Authorization;
using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/permission")]
[ApiController]
public class PermissionController(IPermissionRepository repo, INFRASTRUCTURE.Context.ApplicationDbContext context) : ControllerBase
{
    /// <summary>
    /// Retrieves all permission modules in the system.
    /// </summary>
    [HttpGet("modules")]
    [AnyPermission("CanViewPermissions", "CanViewRoles", "CanCreateRole", "CanEditRole")]
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
    [AnyPermission("CanViewPermissions", "CanViewRoles", "CanCreateRole", "CanEditRole")]
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
    [SignedInUser("Returns the caller's own permissions, which every signed-in page needs.")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<PermissionModuleDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetPermissionsForUser(Guid? userId)
    {
        var id = (string)HttpContext.Items["Sub"];
        if (id == null) return TypedResults.Unauthorized();

        // Your own permissions are yours to read; anyone else's need CanViewPermissions.
        if (userId is { } other && other != Guid.Parse(id)
            && !(await CurrentPermissionSet.Load(context, Guid.Parse(id))).Contains(PermissionKeys.CanViewPermissions))
            return TypedResults.Forbid();
        var result = await repo.GetAllPermissionForUser(userId ?? Guid.Parse(id));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves all permission modules assigned to a role.
    /// </summary>
    /// <param name="roleId">The role ID.</param>
    [HttpGet("role/{roleId}")]
    [Authorize(PermissionKeys.CanViewPermissions)]
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
    [Authorize(PermissionKeys.CanUpdateExistingPermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> UpdateRolePermissions(Guid roleId, [FromBody] List<PermissionModuleDto> permissions)
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"] as string, out var actor)) return TypedResults.Unauthorized();
        var result = await repo.UpdateRolePermissions(permissions, roleId, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets filtered menu based on user role
    /// </summary>
    [HttpGet("menu")]
    [SignedInUser("Returns the caller's own menu.")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<MenuItem>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetFilteredMenu()
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repo.GetFilteredMenu(Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

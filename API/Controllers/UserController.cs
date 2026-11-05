using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Users.Request;
using SHARED;
using SHARED.Requests;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/user")]
[ApiController]
public class UserController(IUserRepository repo) : ControllerBase
{
    [Authorize(PermissionKeys.CanCreateUser)]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IResult> CreateUser([FromBody] CreateUserRequest request)
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"] as string, out var actor)) return TypedResults.Unauthorized();
        var response = await repo.CreateUser(request, actor);
        return response.IsSuccess ? TypedResults.Ok(response) : response.ToProblemDetails();
    }

    /// <summary>
    /// Self sign-up is switched off: accounts are created by someone with CanCreateUser through POST user, and
    /// roles are given separately. The route stays so older clients get a clear refusal.
    /// </summary>
    [Authorize(PermissionKeys.CanCreateUser)]
    [HttpPost("sign-up")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IResult CreateNewUser([FromBody] CreateClientRequest request) =>
        Result.Failure(Error.Forbidden("User.SignUpDisabled", "Self sign-up is disabled. Create the account from user management.")).ToProblemDetails();

    [Authorize]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<UserWithRoleDto>>))]
    public async Task<IResult> GetUsers([FromQuery(Name = "isActive")] bool? isDisabled, [FromQuery(Name = "page")] int page = 1,
        [FromQuery(Name = "pageSize")] int pageSize = 5,
        [FromQuery(Name = "searchQuery")] string searchQuery = null,
        [FromQuery(Name = "roleId")] Guid? roleId = null,
        [FromQuery(Name = "departmentId")] Guid? departmentId = null)
    {
        var response = await repo.GetUsers(page, pageSize, searchQuery, isDisabled, roleId, departmentId);
        return response.IsSuccess ? TypedResults.Ok(response.Value) : response.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves users who hold at least one of the given permission keys, e.g. to
    /// restrict a picker to users who are actually allowed to perform an action
    /// (only QC-permissioned users can be assigned to run a test).
    /// </summary>
    [Authorize]
    [HttpGet("by-permission")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<UserWithRoleDto>>))]
    public async Task<IResult> GetUsersByPermissions(
        [FromQuery(Name = "permissionKey")] List<string> permissionKey,
        [FromQuery(Name = "page")] int page = 1,
        [FromQuery(Name = "pageSize")] int pageSize = 5,
        [FromQuery(Name = "searchQuery")] string searchQuery = null)
    {
        // Clients may send this as repeated keys (?permissionKey=A&permissionKey=B)
        // or as one comma-joined value (?permissionKey=A,B) - model binding only
        // splits the former, so split any comma-joined entries here too.
        var keys = permissionKey
            .SelectMany(k => k.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToList();
        var response = await repo.GetUsersByPermissions(keys, page, pageSize, searchQuery);
        return response.IsSuccess ? TypedResults.Ok(response.Value) : response.ToProblemDetails();
    }

    [Authorize]
    [HttpGet("authenticated")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserWithRoleDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetUser()
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var response = await repo.GetUser(Guid.Parse(userId));
        return response.IsSuccess ? TypedResults.Ok(response.Value) : response.ToProblemDetails();
    }

    [Authorize]
    [HttpGet("role/{roleId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UserWithRoleDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetUserByRole(Guid roleId, [FromQuery(Name = "departmentId")] Guid? departmentId = null)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var response = await repo.GetUsersByRoleId(roleId, departmentId);
        return response.IsSuccess ? TypedResults.Ok(response.Value) : response.ToProblemDetails();
    }


    [Authorize(PermissionKeys.CanEditUser)]
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateUser([FromBody] UpdateUserRequest request, Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var response = await repo.UpdateUser(request, id, Guid.Parse(userId));
        return response.IsSuccess ? TypedResults.NoContent() : response.ToProblemDetails();
    }

    [Authorize(PermissionKeys.CanAssignUserRole)]
    [HttpPut("role/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateUserRoles([FromBody] UpdateUserRoleRequest request, Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var response = await repo.UpdateRolesOfUser(request, id, Guid.Parse(userId));
        return response.IsSuccess ? TypedResults.NoContent() : response.ToProblemDetails();
    }

    [Authorize(PermissionKeys.CanDeleteUser)]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteUser(Guid id)
    {
        try
        {
            var userId = (string)HttpContext.Items["Sub"];
            if (userId == null) return TypedResults.Unauthorized();

            var response = await repo.DeleteUser(id, Guid.Parse(userId));
            return response.IsSuccess ? TypedResults.NoContent() : response.ToProblemDetails();
        }
        catch (Exception e)
        {
            return TypedResults.NotFound(e.Message);
        }
    }

    /// <summary>Sets the signed-in user's own avatar; another user's is refused.</summary>
    [Authorize]
    [HttpPost("avatar/{id?}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UploadAnImage([Required][FromBody] UploadFileRequest request, Guid? id = null)
    {
        try
        {
            var userId = (string)HttpContext.Items["Sub"];
            if (userId == null) return TypedResults.Unauthorized();

            if (id is { } other && other != Guid.Parse(userId)) return TypedResults.Forbid();
            await repo.UploadAvatar(request, Guid.Parse(userId));
            return TypedResults.NoContent();
        }
        catch (Exception)
        {
            return TypedResults.NoContent();
        }
    }

    /// <summary>
    /// Sets the signed-in user's own electronic signature. Nobody can set another user's signature, so a
    /// signature always comes from the person it belongs to.
    /// </summary>
    [Authorize(PermissionKeys.CanCreateSignatureSettings)]
    [HttpPost("signature/{id?}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UploadSignature([Required][FromBody] UploadFileRequest request, Guid? id = null)
    {
        try
        {
            var userId = (string)HttpContext.Items["Sub"];
            if (userId == null) return TypedResults.Unauthorized();

            if (id is { } other && other != Guid.Parse(userId)) return TypedResults.Forbid();
            await repo.UploadSignature(request, Guid.Parse(userId));
            return TypedResults.NoContent();
        }
        catch (Exception)
        {
            return TypedResults.NoContent();
        }
    }

    /// <summary>
    /// Sets or removes the signed-in user's department. Send an explicit null departmentId to
    /// remove the assignment. Requires the CanSwitchDepartment permission.
    /// </summary>
    [Authorize(PermissionKeys.CanSwitchDepartment)]
    [HttpPost("switch-department")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> SetDepartment([FromBody] SwitchDepartmentRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var token = HttpContext.Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();
        if (string.IsNullOrEmpty(token)) return TypedResults.Unauthorized();

        var response = await repo.SwitchDepartment(request.DepartmentId, Guid.Parse(userId), token);
        return response.IsSuccess ? TypedResults.NoContent() : response.ToProblemDetails();
    }

    /// <summary>
    /// Legacy URL for assigning a department. Requires the CanSwitchDepartment permission
    /// (IT Support &gt; Manage User Directory); role names no longer grant it.
    /// </summary>
    [Authorize(PermissionKeys.CanSwitchDepartment)]
    [HttpPost("switch-department/{departmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> SwitchDepartment(Guid departmentId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var token = HttpContext.Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();
        if (string.IsNullOrEmpty(token)) return TypedResults.Unauthorized();

        var response = await repo.SwitchDepartment(departmentId, Guid.Parse(userId), token);
        return response.IsSuccess ? TypedResults.Ok() : response.ToProblemDetails();
    }

    /// <summary>Blocks an active user (CanBlockUser) or unblocks a blocked one (CanUnblockUser); never the caller.</summary>
    [Authorize]
    [HttpPut("toggle-disable/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DisableUser([FromRoute] Guid id)
    {
        try
        {
            var userId = (string)HttpContext.Items["Sub"];
            if (userId == null) return TypedResults.Unauthorized();

            var response = await repo.ToggleDisableUser(id, Guid.Parse(userId));
            return response.IsSuccess ? TypedResults.NoContent() : response.ToProblemDetails();
        }
        catch (Exception e)
        {
            return TypedResults.NotFound(e.Message);
        }
    }
}

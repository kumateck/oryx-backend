using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using APP.Extensions;
using APP.IRepository;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using SHARED.Requests;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/user")]
[ApiController]
//[ValidateModelState]
public class UserController(IUserRepository repo) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    [ApiVersion(2)]
    public async Task<IResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var response = await repo.CreateUser(request);
        return response.IsSuccess ? TypedResults.Ok(response) : response.ToProblemDetails();
    }
    
    [AllowAnonymous]
    [HttpPost("sign-up")]
    public async Task<IActionResult> CreateNewUser([FromBody] CreateClientRequest request)
    {
        return Created(string.Empty, await repo.CreateNewUser(request));
    }
    
    //[Authorize("permission.user." + PermissionUtils.PermSuffixRead)]
    [HttpGet]
    public async Task<IResult> GetUsers([FromQuery(Name = "page")] int page = 1,
        [FromQuery(Name = "pageSize")] int pageSize = 5,
        [FromQuery(Name = "roleNames")] string roleNames = null,
        [FromQuery(Name = "searchQuery")] string searchQuery = null,
        [FromQuery(Name = "with-disabled")] bool withDisabled = false)
    {
        var response = await repo.GetUsers(page, pageSize, searchQuery, roleNames, withDisabled);
        return response.IsSuccess ? TypedResults.Ok(response.Value) : response.ToProblemDetails();
    }
    
    //[Authorize("permission.user." + PermissionUtils.PermSuffixUpdate)]
    [HttpPut("{id}")]
    public async Task<IResult> UpdateUser([FromBody] UpdateUserRequest request, Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var response = await repo.UpdateUser(request, id, Guid.Parse(userId));
        return response.IsSuccess ? TypedResults.NoContent() : response.ToProblemDetails();
    }
    
    //[Authorize("permission.user." + PermissionUtils.PermSuffixUpdate)]
    [HttpPut("role/{id}")]
    public async Task<IResult> UpdateUserRoles([FromBody] UpdateUserRoleRequest request, Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var response = await repo.UpdateRolesOfUser(request, id, Guid.Parse(userId));
        return response.IsSuccess ? TypedResults.NoContent() : response.ToProblemDetails();
    }

    //[Authorize("permission.user." + PermissionUtils.PermSuffixDelete)]
    [HttpDelete("{id}")]
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
    
    [AllowAnonymous]
    [HttpPost("avatar/{id?}")]
    public async Task<IResult> UploadAnImage([Required][FromBody] UploadFileRequest request, Guid? id = null)
    {
        try
        {
            var userId = (string)HttpContext.Items["Sub"];
            if (userId == null) return TypedResults.Unauthorized();
        
            await repo.UploadAvatar(request, id ?? Guid.Parse(userId));
            return  TypedResults.NoContent();
        }
        catch (Exception)
        {
            return TypedResults.NoContent();
        }
    }
    
    //[Authorize("permission.user." + PermissionUtils.PermSuffixDelete)]
    [HttpGet("toggle-disable/{id}")]
    public async Task<IResult> DisableUser(Guid id)
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
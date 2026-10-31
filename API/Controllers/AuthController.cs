using APP.Extensions;
using APP.IRepository;
using DOMAIN.Entities.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using ForgotPasswordRequest = DOMAIN.Entities.Auth.ForgotPasswordRequest;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/auth")]
[ApiController]
public class AuthController(IAuthRepository repo) : ControllerBase
{
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponse))]
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IResult> Login([FromBody] LoginRequest request)
    {
        var response = await repo.Login(request);
        return response.IsSuccess ? TypedResults.Ok(response.Value) : response.ToProblemDetails();
    }

    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponse))]
    [AllowAnonymous]
    [HttpPost("login-with-refresh-token")]
    public async Task<IResult> Login([FromBody] LoginWithRefreshToken request)
    {
        var response = await repo.LoginWithRefreshToken(request);
        return response.IsSuccess ? TypedResults.Ok(response.Value) : response.ToProblemDetails();
    }

    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PasswordChangeResponse))]
    [AllowAnonymous]
    [HttpPost("set-password")]
    public async Task<IResult> SetPassword([FromBody] SetPasswordRequest request)
    {
        var response = await repo.SetPassword(request);
        return response.IsSuccess ? TypedResults.Ok(response.Value) : response.ToProblemDetails();
    }

    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PasswordChangeResponse))]
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IResult> ResetPassword([FromBody] ChangePasswordRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];

        var response = await repo.ResetPassword(request, Guid.Parse(userId));
        return response.IsSuccess ? TypedResults.Ok(response.Value) : response.ToProblemDetails();
    }

    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var response = await repo.ForgotPassword(request);
        return response.IsSuccess ? TypedResults.NoContent() : response.ToProblemDetails();
    }

    /// <summary>
    /// Password change for logged-in users.
    /// </summary>
    [HttpPost("user-reset-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PasswordChangeResponse))]
    public async Task<IResult> UserChangePassword([FromBody] UserPasswordChangeRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repo.ChangePassword(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    /// <summary>
    /// Sets a new password for another user. Only dkadusei@kumateck.com may call this; everyone
    /// else gets 403. The route sits under /auth so the activity-log middleware never stores the
    /// request body (it contains the new password); the repository writes its own audit entry.
    /// </summary>
    [HttpPost("admin/user-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> AdminResetUserPassword([FromBody] AdminResetUserPasswordRequest request,
        [FromServices] IUserPasswordAdminRepository passwordAdmin)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await passwordAdmin.ResetUserPassword(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
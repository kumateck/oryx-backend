using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace SHARED.Services.Identity;

public class CurrentUserService : ICurrentUserService
{
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdString, out var userId))
            UserId = userId;

        var departmentIdString = httpContextAccessor.HttpContext?.User.FindFirstValue("department");
        if (Guid.TryParse(departmentIdString, out var departmentId))
            DepartmentId = departmentId;
    }

    public Guid? UserId { get; }

    public Guid? DepartmentId { get; }
}
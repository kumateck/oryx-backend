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
    }

    public Guid? UserId { get; }
    //public List<KeyValuePair<string, string>> Claims { get; set; }
}
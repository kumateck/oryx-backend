using APP.IRepository;
using APP.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/performance")]
[ApiController]
[Authorize]
public partial class PerformanceController(IPerformanceRepository repository, INFRASTRUCTURE.Context.ApplicationDbContext context) : ControllerBase
{
    private bool TryGetUserId(out Guid userId)
        => Guid.TryParse(HttpContext.Items["Sub"] as string, out userId);

    private async Task<bool> Has(string key)
    {
        if (!TryGetUserId(out var userId)) return false;
        return (await APP.Services.Authorization.CurrentPermissionSet.Load(context, userId)).Contains(key);
    }

    /// <summary>
    /// Goals of other employees need team access; without it a user works only on the goals of the employee record
    /// linked to their account.
    /// </summary>
    private async Task<bool> MayActForEmployee(Guid employeeId)
    {
        if (await Has(PermissionKeys.CanViewTeamGoals)) return true;
        return TryGetUserId(out var userId) && await repository.EmployeeOf(userId) == employeeId;
    }
}

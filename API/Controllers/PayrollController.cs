using APP.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/payroll")]
[ApiController]
[Authorize]
public partial class PayrollController(IPayrollRepository repository) : ControllerBase
{
    private bool TryGetUserId(out Guid userId)
        => Guid.TryParse(HttpContext.Items["Sub"] as string, out userId);
}

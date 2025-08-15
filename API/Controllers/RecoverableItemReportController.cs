using APP.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/recoverable-item-reports")]
[Authorize]
public class RecoverableItemReportController(IRecoverableItemReportRepository repository) : ControllerBase
{
    
}
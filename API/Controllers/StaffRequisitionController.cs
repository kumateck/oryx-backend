using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/staff-requisitions")]
[Authorize]
public class StaffRequisitionController : ControllerBase
{
    
}
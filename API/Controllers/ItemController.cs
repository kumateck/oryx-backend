using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/item")]
[ApiController]
public class ItemController : ControllerBase
{
}
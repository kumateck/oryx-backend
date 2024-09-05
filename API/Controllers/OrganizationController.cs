using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/organization")]
[ApiController]
[Authorize]
public class OrganizationController
{
}
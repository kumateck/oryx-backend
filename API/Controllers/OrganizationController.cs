using APP.Attribute;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/organization")]
[ApiController]
[Authorize]
[ValidateModelState]
public class OrganizationController
{
}
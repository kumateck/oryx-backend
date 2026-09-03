using APP.IRepository;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/suppliers")]
[ApiController]
public partial class SupplierController(ISupplierRelationshipRepository repository) : ControllerBase
{
    private bool TryGetUserId(out Guid userId)
        => Guid.TryParse(HttpContext.Items["Sub"] as string, out userId);
}

using APP.Services.Authorization;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.StpRevisions;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization;

/// <summary>
/// Requires the specification's view key (reading links) or edit key (changing one), in the stored specification's kind:
/// the route's <c>materialSpecificationId</c> (raw or packaging from its material) or <c>productSpecificationId</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class StpTestLinkPermissionAttribute(bool write) : Attribute, IAsyncActionFilter
{
    public bool Write { get; } = write;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (!Guid.TryParse(http.Items["Sub"] as string, out var actor)) { context.Result = new UnauthorizedResult(); return; }
        var db = http.RequestServices.GetRequiredService<ApplicationDbContext>();
        StpRevisionKind? kind = null;
        if (context.ActionArguments.TryGetValue("productSpecificationId", out var product) && product is Guid productId)
            kind = await db.ProductSpecifications.AsNoTracking().AnyAsync(item => item.Id == productId) ? StpRevisionKind.Product : null;
        else if (context.ActionArguments.TryGetValue("materialSpecificationId", out var material) && material is Guid materialId)
            kind = await db.MaterialSpecifications.AsNoTracking().Where(item => item.Id == materialId)
                .Select(item => (StpRevisionKind?)(item.Material.Kind == MaterialKind.Package ? StpRevisionKind.PackagingMaterial : StpRevisionKind.RawMaterial))
                .FirstOrDefaultAsync();
        // Nothing names a specification that exists: the action answers not found.
        if (kind is null) { await next(); return; }
        var key = StpTestLinkPermissions.Key(kind.Value, Write);
        if ((await CurrentPermissionSet.Load(db, actor)).Contains(key)) { await next(); return; }
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden, Title = "Forbidden", Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3",
            Extensions = { ["errors"] = new[] { new { Code = "StpTestLink.Permission", Description = $"You need {key} for this." } } },
        }) { StatusCode = StatusCodes.Status403Forbidden };
    }
}

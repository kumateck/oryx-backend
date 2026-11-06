using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Requisitions.Request;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization;

/// <summary>
/// Raising a requisition needs the key for what is raised: a purchase requisition needs the create key for each
/// material's kind (read from the stored materials); a stock requisition is raised from a production step and needs the
/// batch-board step key; a trial requisition needs the R&amp;D trial batch key. Grants are read from current data.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RequisitionCreatePermissionAttribute : Attribute, IAsyncActionFilter
{
    public static readonly string[] AllKeys =
    [
        PermissionKeys.CanCreateRawMaterialRequisitions, PermissionKeys.CanCreatePackageMaterialRequisitions,
        PermissionKeys.CanUpdateProductionStep, PermissionKeys.CanCreateRndTrialBatch,
    ];

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (!Guid.TryParse(http.Items["Sub"] as string, out var actor)) { context.Result = new UnauthorizedResult(); return; }
        if (context.ActionArguments.Values.OfType<CreateRequisitionRequest>().FirstOrDefault() is not { } request) { await next(); return; }
        var db = http.RequestServices.GetRequiredService<ApplicationDbContext>();
        var needed = await Keys(db, request);
        var grants = await CurrentPermissionSet.Load(db, actor);
        var missing = needed.Where(key => !grants.Contains(key)).ToList();
        if (missing.Count == 0) { await next(); return; }
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden, Title = "Forbidden", Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3",
            Extensions = { ["errors"] = new[] { new { Code = "Requisition.Permission", Description = $"You need {string.Join(" and ", missing)} for this." } } },
        }) { StatusCode = StatusCodes.Status403Forbidden };
    }

    public static async Task<List<string>> Keys(ApplicationDbContext db, CreateRequisitionRequest request)
    {
        switch (request.RequisitionType)
        {
            case RequisitionType.Stock: return [PermissionKeys.CanUpdateProductionStep];
            case RequisitionType.Trial: return [PermissionKeys.CanCreateRndTrialBatch];
        }
        var materials = request.Items.Select(item => item.MaterialId).ToList();
        var kinds = await db.Materials.IgnoreQueryFilters().AsNoTracking().Where(material => materials.Contains(material.Id))
            .Select(material => material.Kind).Distinct().ToListAsync();
        return kinds.Select(kind => kind == MaterialKind.Raw ? PermissionKeys.CanCreateRawMaterialRequisitions : PermissionKeys.CanCreatePackageMaterialRequisitions)
            .Distinct().ToList();
    }
}

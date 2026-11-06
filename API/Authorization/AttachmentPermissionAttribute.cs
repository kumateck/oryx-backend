using APP.Services.Authorization;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization;

/// <summary>
/// Adding or removing a file on a record follows the page that works on that record: it needs one of that record
/// type's create or edit keys. The type is the route <c>modelType</c> for an upload, or the stored attachment's type for
/// a removal (route <c>modelId</c>, optionally <c>reference</c>). Types with no page are refused unless the caller is the
/// system administrator. Reading a file needs no extra key: pages get the file's link inside the record they may read.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AttachmentPermissionAttribute : Attribute, IAsyncActionFilter
{
    private static string[] Stores(string format) => new[] { "It", "General", "Equipment", "Reagent" }.Select(store => string.Format(format, store)).ToArray();

    /// <summary>Each record type's keys, any one of which lets the caller add or remove its files.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> Keys = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["BillingSheet"] = ["CanCreateBillingSheet", "CanEditBillingSheet"],
        ["Department"] = ["CanCreateNewDepartment", "CanEditDepartment"],
        ["Employee"] = ["CanRegisterEmployee", "CanUpdateEmployeeInfo"],
        ["GrnNumber"] = ["CanCreateGrnForRawMaterials", "CanCreateGrnForPackagingMaterials"],
        ["Item"] = [.. Stores("CanCreateNewItemIn{0}Store"), .. Stores("CanEditItemIn{0}Store")],
        ["LeaveRequest"] = ["CanCreateLeaveRequest", "CanEditLeaveRequest"],
        ["MaterialAnalyticalRawData"] = ["CanCreateRawMaterialArd", "CanEditRawMaterialArd", "CanCreatePackagingMaterialArd", "CanEditPackagingMaterialArd"],
        ["ProductAnalyticalRawData"] = ["CanCreateProductArd", "CanEditProductArd"],
        ["Payment"] = ["CanRecordPayment"],
        ["PayrollRun"] = ["CanCreatePayrollRun"],
        ["PerformanceReview"] = ["CanSubmitSelfAssessment", "CanSubmitManagerReview"],
        ["ProductBatchNumber"] = ["CanIssueBmr"],
        ["ProductSTPNumber"] = ["CanCreateProductStp", "CanEditProductStp"],
        ["ProformaInvoice"] = ["CanGenerateProformaInvoice"],
        ["PurchaseOrder"] = ["CanCreateForeignPurchaseOrder", "CanCreateLocalPurchaseOrder", "CanSendForeignProformaRequest", "CanSendLocalProformaRequest"],
        ["Service"] = ["CanCreateService", "CanEditService"],
        ["ShipmentDocument"] = ["CanCreateShipmentDocument", "CanEditShipmentDocument"],
        ["Signature"] = ["CanCreateSignatureSettings"],
    };

    public static IEnumerable<string> AllKeys => Keys.Values.SelectMany(keys => keys).Distinct();

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (!Guid.TryParse(http.Items["Sub"] as string, out var actor)) { context.Result = new UnauthorizedResult(); return; }
        var db = http.RequestServices.GetRequiredService<ApplicationDbContext>();
        var types = await Types(db, context.ActionArguments);
        // Nothing to remove: the action itself answers not found.
        if (types.Count == 0) { await next(); return; }
        var grants = await CurrentPermissionSet.Load(db, actor);
        var refused = types.Where(type => !(Keys.TryGetValue(type, out var keys) && keys.Any(grants.Contains))).ToList();
        if (refused.Count == 0 || await AccessGuard.IsSystemAdministrator(db, actor)) { await next(); return; }
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden, Title = "Forbidden", Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3",
            Extensions = { ["errors"] = new[] { new { Code = "Attachment.Permission", Description = $"You cannot add or remove files on {string.Join(", ", refused)} records." } } },
        }) { StatusCode = StatusCodes.Status403Forbidden };
    }

    public static async Task<HashSet<string>> Types(ApplicationDbContext db, IDictionary<string, object?> arguments)
    {
        if (arguments.TryGetValue("modelType", out var type) && type is string modelType)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { modelType };
        if (!arguments.TryGetValue("modelId", out var id) || id is not Guid modelId) return [];
        var reference = arguments.TryGetValue("reference", out var value) ? value as string : null;
        var stored = await db.Attachments.AsNoTracking().Where(item => item.ModelId == modelId && (reference == null || item.Reference == reference))
            .Select(item => item.ModelType).Distinct().ToListAsync();
        return new HashSet<string>(stored.Where(item => !string.IsNullOrEmpty(item)), StringComparer.OrdinalIgnoreCase);
    }
}

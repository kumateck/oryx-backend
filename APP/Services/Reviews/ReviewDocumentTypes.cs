using APP.Utils;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Reviews;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.Reviews;

/// <summary>
/// A record as the review step sees it, with the keys for each task chosen from its stored kind. Every
/// key in a list is required.
/// </summary>
public sealed record ReviewTarget(
    string ModelType,
    string ConfigurationType,
    Guid ModelId,
    string Code,
    Guid? PreparedById,
    IReadOnlyList<string> ViewKeys,
    IReadOnlyList<string> ConfirmKeys,
    IReadOnlyList<string> ReturnKeys,
    IReadOnlyList<string> ResubmitKeys,
    bool AlwaysReviewed,
    int? Variant = null);

/// <summary>
/// The document types that have a review step. A type is listed only once its corrections during review
/// and its desktop Review panel exist; only listed types can be configured.
/// </summary>
public static class ReviewDocumentTypes
{
    public const string PurchaseOrder = "PurchaseOrder";
    public const string PurchaseRequisition = "PurchaseRequisition";
    public const string StockRequisition = "StockRequisition";
    public const string RawStockRequisition = "RawStockRequisition";
    public const string PackageStockRequisition = "PackageStockRequisition";

    public static readonly IReadOnlyList<ReviewDocumentTypeDto> All =
    [
        // A purchase order has always had to be checked before approval; its check is this review.
        new(PurchaseOrder, "Purchase order", AlwaysReviewed: true),
        new(PurchaseRequisition, "Purchase requisition", AlwaysReviewed: false),
        new(StockRequisition, "Stock requisition", AlwaysReviewed: false),
    ];

    public static bool IsConfigurable(string itemType) => All.Any(type => type.ItemType == itemType);

    public static string Label(string configurationType) =>
        All.FirstOrDefault(type => type.ItemType == configurationType)?.Label ?? configurationType;

    public static string ConfigurationTypeFor(string modelType) => modelType switch
    {
        RawStockRequisition or PackageStockRequisition => StockRequisition,
        _ => modelType,
    };

    /// <summary>
    /// Types whose review is opened when their approvals would start. A purchase order's review opens when
    /// it is finalised or revised instead, because its check already sits before its approvals.
    /// </summary>
    public static bool HeldAtApprovalStart(string modelType) =>
        modelType is PurchaseRequisition or RawStockRequisition or PackageStockRequisition;

    public static bool IsReviewModelType(string modelType) =>
        modelType is PurchaseOrder or PurchaseRequisition or RawStockRequisition or PackageStockRequisition;

    /// <summary>
    /// Loads the record and its keys. The kind always comes from what is stored: a model type in a request
    /// that does not match the stored record finds nothing.
    /// </summary>
    public static async Task<ReviewTarget> LoadAsync(ApplicationDbContext context, string modelType, Guid id,
        CancellationToken cancellationToken = default)
    {
        switch (modelType)
        {
            case PurchaseOrder:
            {
                var order = await context.PurchaseOrders.AsNoTracking().Where(item => item.Id == id)
                    .Select(item => new { item.Code, item.CreatedById, item.Supplier.Type }).FirstOrDefaultAsync(cancellationToken);
                if (order is null) return null;
                var foreign = order.Type == SupplierType.Foreign;
                return new ReviewTarget(PurchaseOrder, PurchaseOrder, id, order.Code, order.CreatedById,
                    [foreign ? PermissionKeys.CanViewForeignPurchaseOrder : PermissionKeys.CanViewLocalPurchaseOrder],
                    [foreign ? PermissionKeys.CanCheckForeignPurchaseOrder : PermissionKeys.CanCheckLocalPurchaseOrder],
                    [foreign ? PermissionKeys.CanReturnForeignPurchaseOrderCheck : PermissionKeys.CanReturnLocalPurchaseOrderCheck],
                    [foreign ? PermissionKeys.CanCreateForeignPurchaseOrder : PermissionKeys.CanCreateLocalPurchaseOrder],
                    AlwaysReviewed: true, Variant: (int)order.Type);
            }
            case PurchaseRequisition or RawStockRequisition or PackageStockRequisition:
            {
                var requisition = await context.Requisitions.AsNoTracking().Where(item => item.Id == id)
                    .Select(item => new { item.Code, item.RequestedById, item.CreatedById, item.RequisitionType }).FirstOrDefaultAsync(cancellationToken);
                if (requisition is null) return null;
                var kinds = await context.RequisitionItems.AsNoTracking()
                    .Where(item => item.RequisitionId == id && item.DeletedAt == null)
                    .Select(item => item.Material.Kind).Distinct().ToListAsync(cancellationToken);
                if (kinds.Count == 0) return null;
                var preparer = requisition.CreatedById ?? requisition.RequestedById;
                if (requisition.RequisitionType == RequisitionType.Purchase)
                {
                    if (modelType != PurchaseRequisition) return null;
                    return new ReviewTarget(PurchaseRequisition, PurchaseRequisition, id, requisition.Code, preparer,
                        Keys(kinds, PermissionKeys.CanViewRawMaterialRequisitionsForProduction, PermissionKeys.CanViewPackagingMaterialRequisitionsForProduction),
                        Keys(kinds, PermissionKeys.CanReviewRawMaterialPurchaseRequisition, PermissionKeys.CanReviewPackageMaterialPurchaseRequisition),
                        Keys(kinds, PermissionKeys.CanReturnRawMaterialPurchaseRequisition, PermissionKeys.CanReturnPackageMaterialPurchaseRequisition),
                        Keys(kinds, PermissionKeys.CanEditRawMaterialPurchaseRequisition, PermissionKeys.CanEditPackageMaterialPurchaseRequisition),
                        AlwaysReviewed: false);
                }
                if (requisition.RequisitionType != RequisitionType.Stock || kinds.Count != 1) return null;
                var raw = kinds[0] == MaterialKind.Raw;
                if (modelType != (raw ? RawStockRequisition : PackageStockRequisition)) return null;
                return new ReviewTarget(modelType, StockRequisition, id, requisition.Code, preparer,
                    [raw ? PermissionKeys.CanViewRawMaterialRequisitionsForProduction : PermissionKeys.CanViewPackagingMaterialRequisitionsForProduction],
                    [raw ? PermissionKeys.CanReviewRawMaterialStockRequisition : PermissionKeys.CanReviewPackageMaterialStockRequisition],
                    [raw ? PermissionKeys.CanReturnRawMaterialStockRequisition : PermissionKeys.CanReturnPackageMaterialStockRequisition],
                    // A stock requisition is corrected through its batch (Repair) and sent back by production.
                    [PermissionKeys.CanUpdateProductionStep],
                    AlwaysReviewed: false);
            }
            default:
                return null;
        }
    }

    private static List<string> Keys(IEnumerable<MaterialKind> kinds, string raw, string package) =>
        kinds.Select(kind => kind == MaterialKind.Raw ? raw : package).Distinct().ToList();

    /// <summary>The person who gave the record's final approval, when a person did.</summary>
    public static async Task<(Guid? UserId, DateTime? At)> FinalApproverAsync(ApplicationDbContext context, string modelType, Guid id,
        CancellationToken cancellationToken = default)
    {
        if (modelType == PurchaseOrder)
        {
            if (!await context.PurchaseOrders.AnyAsync(item => item.Id == id && item.Approved, cancellationToken)) return (null, null);
            var stage = await context.PurchaseOrderApprovals.AsNoTracking()
                .Where(item => item.PurchaseOrderId == id && item.Status == ApprovalStatus.Approved && item.ApprovedById != null)
                .OrderByDescending(item => item.Order).ThenByDescending(item => item.ApprovalTime)
                .Select(item => new { item.ApprovedById, item.ApprovalTime }).FirstOrDefaultAsync(cancellationToken);
            return (stage?.ApprovedById, stage?.ApprovalTime);
        }
        if (modelType is PurchaseRequisition or RawStockRequisition or PackageStockRequisition)
        {
            var requisition = await context.Requisitions.AsNoTracking().Where(item => item.Id == id && item.Approved)
                .Select(item => new { item.ContentRevision }).FirstOrDefaultAsync(cancellationToken);
            if (requisition is null) return (null, null);
            var stage = await context.RequisitionApprovals.AsNoTracking()
                .Where(item => item.RequisitionId == id && item.ContentRevision == requisition.ContentRevision
                    && item.Status == ApprovalStatus.Approved && item.ApprovedById != null)
                .OrderByDescending(item => item.Order).ThenByDescending(item => item.ApprovalTime)
                .Select(item => new { item.ApprovedById, item.ApprovalTime }).FirstOrDefaultAsync(cancellationToken);
            return (stage?.ApprovedById, stage?.ApprovalTime);
        }
        return (null, null);
    }
}

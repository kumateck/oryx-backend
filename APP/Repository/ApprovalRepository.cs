using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;
public class ApprovalRepository(ApplicationDbContext context, IMapper mapper) : IApprovalRepository 
{ 
    public async Task<Result<Guid>> CreateApproval(CreateApprovalRequest request, Guid userId) 
    {
        if (await context.Approvals.FirstOrDefaultAsync(a => a.ItemType == request.ItemType) is not null)
        {
            return Error.Validation("Approval", "Approval for this type already exists");
        }
        var approval = mapper.Map<Approval>(request); 
        approval.CreatedById = userId; 
        await context.Approvals.AddAsync(approval); 
        await context.SaveChangesAsync();
        
        return approval.Id;
    }
    
    public async Task<Result<ApprovalDto>> GetApproval(Guid approvalId) 
    { 
        var approval = await context.Approvals
            .Include(a => a.ApprovalStages)
            .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == approvalId);

        return approval is null ? Error.NotFound("Approval.NotFound", "Approval was not found") : mapper.Map<ApprovalDto>(approval);
    }
    
    public async Task<Result<Paginateable<IEnumerable<ApprovalDto>>>> GetApprovals(int page, int pageSize, string searchQuery) 
    { 
        var query = context.Approvals
            .Include(a => a.ApprovalStages)
            .AsQueryable();
        
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, a => a.ItemType.ToString());
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<ApprovalDto>
        );
    }
    public async Task<Result> UpdateApproval(CreateApprovalRequest request, Guid approvalId, Guid userId) 
    { 
        var existingApproval = await context.Approvals.FirstOrDefaultAsync(a => a.Id == approvalId); 
        if (existingApproval is null)
        {
            return Error.NotFound("Approval.NotFound", "Approval was not found");
        }

        mapper.Map(request, existingApproval);
        existingApproval.LastUpdatedById = userId;

        context.Approvals.Update(existingApproval);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    public async Task<Result> DeleteApproval(Guid approvalId, Guid userId) 
    { 
        var approval = await context.Approvals.FirstOrDefaultAsync(a => a.Id == approvalId); 
        if (approval is null)
        {
            return Error.NotFound("Approval.NotFound", "Approval was not found");
        }

        approval.DeletedAt = DateTime.UtcNow;
        approval.LastDeletedById = userId;
        context.Approvals.Update(approval);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ApproveItem(string modelType, Guid modelId, Guid userId, List<Guid> roleIds, string comments = null)
    {
        if (modelType is "PurchaseRequisition" or "StockRequisition")
        {
            var requisition = await context.Requisitions
                .Include(r => r.Approvals)
                .FirstOrDefaultAsync(r => r.Id == modelId);

            if (requisition is null)
                return RequisitionErrors.NotFound(modelId);

            var expectedType = modelType == "PurchaseRequisition"
                ? RequisitionType.Purchase
                : RequisitionType.Stock;

            if (requisition.RequisitionType != expectedType)
            {
                return Error.Validation("Approval.TypeMismatch",
                    $"Requisition type mismatch. Expected {expectedType} but got {requisition.RequisitionType}.");
            }

            var approvalStages = requisition.Approvals.Select(item => new ResponsibleApprovalStage
            {
                RoleId = item.RoleId,
                UserId = item.UserId,
                Order = item.Order,
                Approved = item.Approved,
                Required = item.Required,
                ApprovalTime = item.ApprovalTime,
                Comments = item.Comments
            }).ToList();

            var currentApprovals = GetCurrentApprovalStage(approvalStages);

            var approvableStage = currentApprovals.FirstOrDefault(stage =>
                stage.UserId == userId || (stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value)));

            if (approvableStage == null)
            {
                return Error.Validation("Approval.Unauthorized",
                    "You are not authorized to approve this resource at this time.");
            }

            // Approve the stage in the actual tracked list (not the mapped one)
            var stageToApprove = requisition.Approvals.First(stage =>
                (stage.UserId == approvableStage.UserId && stage.UserId == userId) ||
                (stage.RoleId == approvableStage.RoleId && approvableStage.RoleId.HasValue && roleIds.Contains(approvableStage.RoleId.Value)));

            stageToApprove.Approved = true;
            stageToApprove.ApprovalTime = DateTime.UtcNow;
            stageToApprove.Comments = comments;
            stageToApprove.ApprovedById = userId;
            context.RequisitionApprovals.Update(stageToApprove);

            // Optionally mark requisition as fully approved if all required stages are approved
            var allRequiredApproved = requisition.Approvals
                .Where(s => s.Required)
                .All(s => s.Approved);

            if (allRequiredApproved)
            {
                requisition.Approved = true;
            }
            context.Requisitions.Update(requisition);
            await context.SaveChangesAsync();
            return Result.Success();
        }

        // Handle other models
        switch (modelType)
        {
            case nameof(PurchaseOrder):
                var purchaseOrder = await context.PurchaseOrders
                    .Include(po => po.Approvals)
                    .FirstOrDefaultAsync(po => po.Id == modelId);

                if (purchaseOrder is null)
                    return Error.Validation("PurchaseOrder.NotFound", $"Purchase Order {modelId} not found.");

                var purchaseOrderApprovalStages = purchaseOrder.Approvals.Select(item => new ResponsibleApprovalStage
                {
                    RoleId = item.RoleId,
                    UserId = item.UserId,
                    Order = item.Order,
                    Approved = item.Approved,
                    Required = item.Required,
                    ApprovalTime = item.ApprovalTime,
                    Comments = item.Comments
                }).ToList();

                var purchaseOrderCurrentApprovals = GetCurrentApprovalStage(purchaseOrderApprovalStages);

                var purchaseOrderApprovingStage = purchaseOrderCurrentApprovals.FirstOrDefault(stage =>
                    stage.UserId == userId || (stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value)));

                if (purchaseOrderApprovingStage == null)
                {
                    return Error.Validation("Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time.");
                }

                // Approve the purchase order stage in the actual tracked list
                var stageToApprovePo = purchaseOrder.Approvals.First(stage =>
                    (stage.UserId == purchaseOrderApprovingStage.UserId && stage.UserId == userId) ||
                    (stage.RoleId == purchaseOrderApprovingStage.RoleId && purchaseOrderApprovingStage.RoleId.HasValue && roleIds.Contains(purchaseOrderApprovingStage.RoleId.Value)));

                stageToApprovePo.Approved = true;
                stageToApprovePo.ApprovalTime = DateTime.UtcNow;
                stageToApprovePo.Comments = comments;

                // Optionally mark purchase order as fully approved
                var allRequiredPoApproved = purchaseOrder.Approvals
                    .Where(s => s.Required)
                    .All(s => s.Approved);

                if (allRequiredPoApproved)
                {
                    purchaseOrder.Approved = true;
                    //purchaseOrder.Status = RequestStatus.Approved;
                }

                await context.SaveChangesAsync();
                break;

        case nameof(BillingSheet):
            var billingSheet = await context.BillingSheets
                .Include(bs => bs.Approvals)
                .FirstOrDefaultAsync(bs => bs.Id == modelId);

            if (billingSheet is null)
                return Error.Validation("BillingSheet.NotFound", $"Billing Sheet {modelId} not found.");

            var billingSheetApprovalStages = billingSheet.Approvals.Select(item => new ResponsibleApprovalStage
            {
                RoleId = item.RoleId,
                UserId = item.UserId,
                Order = item.Order,
                Approved = item.Approved,
                Required = item.Required,
                ApprovalTime = item.ApprovalTime,
                Comments = item.Comments
            }).ToList();

            var billingSheetCurrentApprovals = GetCurrentApprovalStage(billingSheetApprovalStages);

            var billingSheetApprovingStage = billingSheetCurrentApprovals.FirstOrDefault(stage =>
                stage.UserId == userId || (stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value)));

            if (billingSheetApprovingStage == null)
            {
                return Error.Validation("Approval.Unauthorized",
                    "You are not authorized to approve this resource at this time.");
            }

            // Approve the billing sheet stage in the actual tracked list
            var stageToApproveBs = billingSheet.Approvals.First(stage =>
                (stage.UserId == billingSheetApprovingStage.UserId && stage.UserId == userId) ||
                (stage.RoleId == billingSheetApprovingStage.RoleId && billingSheetApprovingStage.RoleId.HasValue && roleIds.Contains(billingSheetApprovingStage.RoleId.Value)));

            stageToApproveBs.Approved = true;
            stageToApproveBs.ApprovalTime = DateTime.UtcNow;
            stageToApproveBs.Comments = comments;

            // Optionally mark billing sheet as fully approved
            var allRequiredBsApproved = billingSheet.Approvals
                .Where(s => s.Required)
                .All(s => s.Approved);

            if (allRequiredBsApproved)
            {
                billingSheet.Approved = true;
                //billingSheet.Status = RequestStatus.Approved;
            }

            await context.SaveChangesAsync();
            break;

            default:
                return Error.Validation("Approval.InvalidType",
                    $"Unsupported model type: {modelType}");
        }

        return Result.Success();
    }
    
    public async Task<List<ApprovalEntity>> GetEntitiesRequiringApproval(Guid userId, List<Guid> roleIds)
    {
        var entitiesRequiringApproval = new List<ApprovalEntity>();

        // 1. Get Purchase Orders requiring approval
        var purchaseOrders = await context.PurchaseOrders
            .AsSplitQuery()
            .Include(po => po.Approvals).ThenInclude(responsibleApprovalStage => responsibleApprovalStage.ApprovedBy)
            .Include(po => po.CreatedBy)
            .ThenInclude(po => po.Department)
            .Where(po => po.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value))) && !a.Approved))
            .ToListAsync();

        foreach (var po in purchaseOrders)
        {
            entitiesRequiringApproval.Add(new ApprovalEntity
            {
                ModelType = nameof(PurchaseOrder),
                Id = po.Id,
                CreatedAt = po.CreatedAt,
                Department = mapper.Map<DepartmentDto>(po.CreatedBy?.Department),
                Code = po.Code,
                ApprovalLogs = GetApprovalLogs(po.Approvals.Select(p => new ResponsibleApprovalStage
                {
                    Approved = p.Approved,
                    Order = p.Order,
                    Comments = p.Comments,
                    ApprovedBy = p.ApprovedBy,
                    ApprovalTime = p.ApprovalTime
                }).ToList())
            });
        }

        // 2. Get Requisitions requiring approval
        var requisitions = await context.Requisitions
            .Include(r => r.Approvals)
            .Include(po => po.CreatedBy)
            .ThenInclude(po => po.Department)
            .Where(r => r.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value))) && !a.Approved))
            .ToListAsync();

        foreach (var r in requisitions)
        {
            if (r.RequisitionType == RequisitionType.Stock)
            {
                entitiesRequiringApproval.Add(new ApprovalEntity
                {
                    ModelType = "StockRequisition",
                    Id = r.Id,
                    CreatedAt = r.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(r.CreatedBy?.Department),
                    Code = r.Code,
                    ApprovalLogs = GetApprovalLogs(r.Approvals.Select(p => new ResponsibleApprovalStage
                    {
                        Approved = p.Approved,
                        Order = p.Order,
                        Comments = p.Comments,
                        ApprovedBy = p.ApprovedBy,
                        ApprovalTime = p.ApprovalTime
                    }).ToList())
                });
            }
            else
            {
                entitiesRequiringApproval.Add(new ApprovalEntity
                {
                    ModelType = "PurchaseRequisition",
                    Id = r.Id,
                    CreatedAt = r.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(r.CreatedBy?.Department),
                    Code = r.Code
                });
            }
        }

        // 3. Get Billing Sheets requiring approval
        var billingSheets = await context.BillingSheets
            .Include(bs => bs.Approvals)
            .Include(po => po.CreatedBy)
            .ThenInclude(po => po.Department)
            .Where(bs => bs.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value))) && !a.Approved))
            .ToListAsync();

        foreach (var bs in billingSheets)
        {
            entitiesRequiringApproval.Add(new ApprovalEntity
            {
                ModelType = nameof(BillingSheet),
                Id = bs.Id,
                Code = bs.Code,
                Department = mapper.Map<DepartmentDto>(bs.CreatedBy?.Department),
                CreatedAt = bs.CreatedAt,
                ApprovalLogs = GetApprovalLogs(bs.Approvals.Select(p => new ResponsibleApprovalStage
                {
                    Approved = p.Approved,
                    Order = p.Order,
                    Comments = p.Comments,
                    ApprovedBy = p.ApprovedBy,
                    ApprovalTime = p.ApprovalTime
                }).ToList())
            });
        }

        return entitiesRequiringApproval;
    }



    public List<CurrentApprovalStage> GetCurrentApprovalStage(List<ResponsibleApprovalStage> stages)
    {
        var result = new List<CurrentApprovalStage>();

        // Sort by order first
        var sortedStages = stages.OrderBy(s => s.Order).ToList();

        // Find the next required unapproved stage
        var nextRequired = sortedStages.FirstOrDefault(s => s.Required && !s.Approved);

        if (nextRequired == null)
        {
            // If there's no more required stages, return any unapproved unrequired ones
            result.AddRange(
                sortedStages
                    .Where(s => !s.Required && !s.Approved)
            );
            return result;
        }

        // Get all unrequired unapproved stages that come *before* the required one
        var priorUnrequired = sortedStages
            .Where(s => !s.Required && !s.Approved && s.Order < nextRequired.Order)
            .ToList();

        result.AddRange(priorUnrequired);
        result.Add(nextRequired);

        return result;
    }
    
    
    public async Task CreateInitialApprovalsAsync(string modelType, Guid modelId)
    {
        var approvalStages = await context.Approvals
            .Where(s => s.ItemType == modelType)
            .SelectMany(s => s.ApprovalStages)
            .OrderBy(s => s.Order)
            .ToListAsync();

        if (!approvalStages.Any())
            return;

        switch (modelType)
        {
            case "RawStockRequisition" or "PackageStockRequisition" or "PurchaseRequisition" or "Requisition": 
                await CreateRequisitionApprovals(modelId, approvalStages);
                break;

            case "BillingSheet":
                await CreateBillingSheetApprovals(modelId, approvalStages);
                break;

            case "PurchaseOrder":
                await CreatePurchaseOrderApprovals(modelId, approvalStages);
                break;

            default:
                throw new NotSupportedException($"Approval creation not supported for model type '{modelType}'");
        }
    }

    private async Task CreateRequisitionApprovals(Guid requisitionId,  List<ApprovalStage> stages)
    {
        var approvals = stages.Select(stage => new RequisitionApproval
        {
            Required = stage.Required,
            Order = stage.Order,
            RequisitionId = requisitionId
        }).ToList();

        await context.RequisitionApprovals.AddRangeAsync(approvals);
        await context.SaveChangesAsync();
    }

    private async Task CreateBillingSheetApprovals(Guid sheetId, List<ApprovalStage> stages)
    {
        var approvals = stages.Select(stage => new BillingSheetApproval
        {
            Required = stage.Required,
            Order = stage.Order,
            BillingSheetId = sheetId
        }).ToList();

        await context.BillingSheetApprovals.AddRangeAsync(approvals);
        await context.SaveChangesAsync();
    }

    private async Task CreatePurchaseOrderApprovals(Guid orderId, List<ApprovalStage> stages)
    {
        var approvals = stages.Select(stage => new PurchaseOrderApproval
        {
            Required = stage.Required,
            Order = stage.Order,
            PurchaseOrderId = orderId
        }).ToList();

        await context.PurchaseOrderApprovals.AddRangeAsync(approvals);
        await context.SaveChangesAsync();
    }
    
    private Result CheckAuthorization(
        List<ResponsibleApprovalStage> approvalStages,
        Guid userId,
        List<Guid> roleIds)
    {
        var currentApprovals = GetCurrentApprovalStage(approvalStages);

        var isUserAuthorized = currentApprovals.Any(c => c.UserId == userId) ||
                               currentApprovals.Any(c => c.RoleId.HasValue && roleIds.Contains(c.RoleId.Value));

        if (!isUserAuthorized)
        {
            return Error.Validation("Approval.Unauthorized", "You are not authorized to approve this resource at this time");
        }

        return Result.Success();
    }
    
    public List<ApprovalLog> GetApprovalLogs(List<ResponsibleApprovalStage> stages)
    {
        return stages
            .Where(s => s.Approved)
            .OrderBy(s => s.Order)
            .Select(s => new ApprovalLog
            {
                User = mapper.Map<CollectionItemDto>(s.ApprovedBy),
                ApprovedAt = s.ApprovalTime,
                Comments = s.Comments
            })
            .ToList();
    }

}

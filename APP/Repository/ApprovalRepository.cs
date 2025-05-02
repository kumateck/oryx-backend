using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.LeaveRequests;
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
        
        if(string.IsNullOrEmpty(request.ItemType))
            return Error.Validation("Approval", "Approval item type is required");
        
        var approval = mapper.Map<Approval>(request); 
        approval.CreatedById = userId; 
        await context.Approvals.AddAsync(approval); 
        await context.SaveChangesAsync();
        
        return approval.Id;
    }
    
    public async Task<Result<ApprovalDto>> GetApproval(Guid approvalId) 
    { 
        var approval = await context.Approvals
            .AsSplitQuery()
            .Include(a => a.ApprovalStages)
            .ThenInclude(a => a.User)
            .Include(a => a.ApprovalStages)
            .ThenInclude(s => s.Role)
            .FirstOrDefaultAsync(a => a.Id == approvalId);

        return approval is null ? Error.NotFound("Approval.NotFound", "Approval was not found") : mapper.Map<ApprovalDto>(approval);
    }
    
    public async Task<Result<Paginateable<IEnumerable<ApprovalDto>>>> GetApprovals(int page, int pageSize, string searchQuery) 
    { 
        var query = context.Approvals
            .AsSplitQuery()
            .Include(a => a.ApprovalStages)
            .ThenInclude(s => s.User)
            .Include(a => a.ApprovalStages)
            .ThenInclude(s => s.Role)
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
                Status = item.Status,
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

            stageToApprove.Status = ApprovalStatus.Approved;
            stageToApprove.ApprovalTime = DateTime.UtcNow;
            stageToApprove.Comments = comments;
            stageToApprove.ApprovedById = userId;
            context.RequisitionApprovals.Update(stageToApprove);

            // Optionally mark requisition as fully approved if all required stages are approved
            var allRequiredApproved = requisition.Approvals
                .Where(s => s.Required)
                .All(s => s.Status == ApprovalStatus.Approved);

            if (allRequiredApproved)
            {
                requisition.Approved = true;
            }
            context.Requisitions.Update(requisition);
            await context.SaveChangesAsync();
            
            //activate next pending stages
            var nextPendingStages = requisition.Approvals
                .Where(s => s.Status == ApprovalStatus.Pending && s.ActivatedAt == null)
                .OrderBy(s => s.Order)
                .ToList();

            if (nextPendingStages.Any())
            {
                // Get the current approval stages after the approval
                var updatedApprovalStages = requisition.Approvals.Select(item => new ResponsibleApprovalStage
                {
                    RoleId = item.RoleId,
                    UserId = item.UserId,
                    Order = item.Order,
                    Status = item.Status,
                    Required = item.Required,
                    ApprovalTime = item.ApprovalTime,
                    Comments = item.Comments
                }).ToList();

                var newlyActiveStages = GetCurrentApprovalStage(updatedApprovalStages)
                    .Where(s => !requisition.Approvals.First(ra =>
                        (ra.UserId == s.UserId && s.UserId.HasValue) || (ra.RoleId == s.RoleId && s.RoleId.HasValue)).ActivatedAt.HasValue)
                    .ToList();

                foreach (var stageToActivate in newlyActiveStages)
                {
                    var actualStage = requisition.Approvals.First(ra =>
                        (ra.UserId == stageToActivate.UserId && stageToActivate.UserId.HasValue) || (ra.RoleId == stageToActivate.RoleId && stageToActivate.RoleId.HasValue));
                    actualStage.ActivatedAt = DateTime.UtcNow;
                    context.RequisitionApprovals.Update(actualStage);
                }
            }
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
                    Status = item.Status,
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

                stageToApprovePo.Status = ApprovalStatus.Approved;
                stageToApprovePo.ApprovalTime = DateTime.UtcNow;
                stageToApprovePo.Comments = comments;

                // Optionally mark purchase order as fully approved
                var allRequiredPoApproved = purchaseOrder.Approvals
                    .Where(s => s.Required)
                    .All(s => s.Status == ApprovalStatus.Approved);

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
                    Status = item.Status,
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

                stageToApproveBs.Status = ApprovalStatus.Approved;
                stageToApproveBs.ApprovalTime = DateTime.UtcNow;
                stageToApproveBs.Comments = comments;

                // Optionally mark billing sheet as fully approved
                var allRequiredBsApproved = billingSheet.Approvals
                    .Where(s => s.Required)
                    .All(s => s.Status == ApprovalStatus.Approved);

                if (allRequiredBsApproved)
                {
                    billingSheet.Approved = true;
                    //billingSheet.Status = RequestStatus.Approved;
                }

                await context.SaveChangesAsync();
                break; 
            
            
            case nameof(LeaveRequest):
                var leaveRequest = await context.LeaveRequests
                    .Include(lr => lr.Approvals)
                    .FirstOrDefaultAsync(lr => lr.Id == modelId);
                
                if (leaveRequest is null)
                    return Error.Validation("LeaveRequest.NotFound", $"Leave Request {modelId} not found.");
                
                var leaveRequestApprovalStages = leaveRequest.Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments
                        
                    }).ToList();
                
                var leaveRequestCurrentApprovals = GetCurrentApprovalStage(leaveRequestApprovalStages);
                
                var leaveRequestApprovingStage = leaveRequestCurrentApprovals.FirstOrDefault(stage =>
                    stage.UserId == userId || (stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value)));
                
                if (leaveRequestApprovingStage == null)
                {
                    return Error.Validation("Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time.");
                }
                
                // Approve the leave request stage in the actual tracked list
                var stageToApproveLr = leaveRequest.Approvals.First(
                    stage => (stage.UserId == leaveRequestApprovingStage.UserId && stage.UserId == userId) ||
                    (stage.RoleId == leaveRequestApprovingStage.RoleId && leaveRequestApprovingStage.RoleId.HasValue && roleIds.Contains(leaveRequestApprovingStage.RoleId.Value)));

                stageToApproveLr.Status = ApprovalStatus.Approved;
                stageToApproveLr.ApprovalTime = DateTime.UtcNow;
                stageToApproveLr.Comments = comments;
                
                // Optionally mark leave request as fully approved
                var allRequiredLrApproved = leaveRequest.Approvals
                    .Where(s => s.Required)
                    .All(s => s.Status == ApprovalStatus.Approved);
                if (allRequiredLrApproved)
                {
                    leaveRequest.Approved = true;
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
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value))) && a.Status != ApprovalStatus.Approved))
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
                RequestedBy = mapper.Map<CollectionItemDto>(po.CreatedBy),
                ApprovalLogs = GetApprovalLogs(po.Approvals.Select(p => new ResponsibleApprovalStage
                {
                    Status = p.Status,
                    Order = p.Order,
                    Comments = p.Comments,
                    ApprovedBy = p.ApprovedBy,
                    ApprovalTime = p.ApprovalTime
                }).ToList())
            });
        }

        // 2. Get Requisitions requiring approval
        var requisitions = await context.Requisitions
            .AsSplitQuery()
            .Include(p => p.Department)
            .Include(p => p.CreatedBy)
            .Include(po => po.Approvals).ThenInclude(responsibleApprovalStage => responsibleApprovalStage.ApprovedBy)
            .Include(po => po.CreatedBy)
            .ThenInclude(po => po.Department)
            .Where(po => po.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value))) && a.Status != ApprovalStatus.Approved))
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
                    RequestedBy = mapper.Map<CollectionItemDto>(r.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(r.Approvals.Select(p => new ResponsibleApprovalStage
                    {
                        Status = p.Status,
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
                    Code = r.Code,
                    RequestedBy = mapper.Map<CollectionItemDto>(r.CreatedBy),
                });
            }
        }

        // 3. Get Billing Sheets requiring approval
        var billingSheets = await context.BillingSheets
            .Include(bs => bs.Approvals)
            .Include(po => po.CreatedBy)
            .ThenInclude(po => po.Department)
            .Where(bs => bs.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value))) && a.Status != ApprovalStatus.Approved))
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
                RequestedBy = mapper.Map<CollectionItemDto>(bs.CreatedBy),
                ApprovalLogs = GetApprovalLogs(bs.Approvals.Select(p => new ResponsibleApprovalStage
                {
                    Status = p.Status,
                    Order = p.Order,
                    Comments = p.Comments,
                    ApprovedBy = p.ApprovedBy,
                    ApprovalTime = p.ApprovalTime
                }).ToList())
            });
        }
        
        var leaveRequests = await context.LeaveRequests
            .Include(bs => bs.Approvals)
            .Include(po => po.CreatedBy)
            .ThenInclude(po => po.Department)
            .Where(bs => bs.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value))) && a.Status != ApprovalStatus.Approved))
            .ToListAsync();

        foreach (var bs in leaveRequests)
        {
            entitiesRequiringApproval.Add(new ApprovalEntity
            {
                ModelType = nameof(LeaveRequest),
                Id = bs.Id,
                Code = "",
                Department = mapper.Map<DepartmentDto>(bs.CreatedBy?.Department),
                CreatedAt = bs.CreatedAt,
                RequestedBy = mapper.Map<CollectionItemDto>(bs.CreatedBy),
                ApprovalLogs = GetApprovalLogs(bs.Approvals.Select(p => new ResponsibleApprovalStage
                {
                    Status = p.Status,
                    Order = p.Order,
                    Comments = p.Comments,
                    ApprovedBy = p.ApprovedBy,
                    ApprovalTime = p.ApprovalTime
                }).ToList())
            });
        }

        return entitiesRequiringApproval;
    }

    public async Task<Result<ApprovalEntity>> GetEntityRequiringApproval(string modelType,
        Guid modelId)
    {

        switch (modelType)
        {
            case "PurchaseRequisition" or "StockRequisition":
                var requisition = await context.Requisitions
                    .AsSplitQuery()
                    .Include(r => r.CreatedBy).ThenInclude(r => r.Department)
                    .Include(r => r.Approvals).ThenInclude(a => a.ApprovedBy)
                    .FirstOrDefaultAsync(r => r.Id == modelId);
                return new ApprovalEntity
                {
                    ModelType = modelType,
                    Id = modelId,
                    Code = requisition.Code,
                    CreatedAt = requisition.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(requisition.CreatedBy?.Department),
                    RequestedBy = mapper.Map<CollectionItemDto>(requisition.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(requisition.Approvals.Select(p => new ResponsibleApprovalStage
                    {
                        Status = p.Status,
                        Order = p.Order,
                        Comments = p.Comments,
                        ApprovedBy = p.ApprovedBy,
                        ApprovalTime = p.ApprovalTime
                    }).ToList())
                };

            case nameof(BillingSheet):
                var billingSheet = await context.BillingSheets
                    .AsSplitQuery()
                    .Include(b => b.CreatedBy).ThenInclude(u => u.Department)
                    .Include(b => b.Approvals).ThenInclude(a => a.ApprovedBy)
                    .FirstOrDefaultAsync(b => b.Id == modelId);
                return new ApprovalEntity
                {
                    ModelType = modelType,
                    Id = modelId,
                    Code = billingSheet.Code,
                    CreatedAt = billingSheet.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(billingSheet.CreatedBy?.Department),
                    RequestedBy = mapper.Map<CollectionItemDto>(billingSheet.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(billingSheet.Approvals.Select(p => new ResponsibleApprovalStage
                    {
                        Status = p.Status,
                        Order = p.Order,
                        Comments = p.Comments,
                        ApprovedBy = p.ApprovedBy,
                        ApprovalTime = p.ApprovalTime
                    }).ToList())
                };

            case nameof(PurchaseOrder):
                var purchaseOrder = await context.PurchaseOrders
                    .AsSplitQuery()
                    .Include(p => p.CreatedBy).ThenInclude(u => u.Department)
                    .Include(p => p.Approvals).ThenInclude(a => a.ApprovedBy)
                    .FirstOrDefaultAsync(p => p.Id == modelId);
                return new ApprovalEntity
                {
                    ModelType = modelType,
                    Id = modelId,
                    Code = purchaseOrder.Code,
                    CreatedAt = purchaseOrder.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(purchaseOrder.CreatedBy?.Department),
                    RequestedBy = mapper.Map<CollectionItemDto>(purchaseOrder.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(purchaseOrder.Approvals.Select(p => new ResponsibleApprovalStage
                    {
                        Status = p.Status,
                        Order = p.Order,
                        Comments = p.Comments,
                        ApprovedBy = p.ApprovedBy,
                        ApprovalTime = p.ApprovalTime
                    }).ToList())
                };

            case nameof(LeaveRequest):
                var leaveRequest = await context.LeaveRequests
                    .AsSplitQuery()
                    .Include(l => l.CreatedBy).ThenInclude(u => u.Department)
                    .Include(l => l.Approvals).ThenInclude(a => a.ApprovedBy)
                    .FirstOrDefaultAsync(l => l.Id == modelId);
                return new ApprovalEntity
                {
                    ModelType = modelType,
                    Id = modelId,
                    Code = "",
                    CreatedAt = leaveRequest.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(leaveRequest.CreatedBy?.Department),
                    RequestedBy = mapper.Map<CollectionItemDto>(leaveRequest.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(leaveRequest.Approvals.Select(p => new ResponsibleApprovalStage
                    {
                        Status = p.Status,
                        Order = p.Order,
                        Comments = p.Comments,
                        ApprovedBy = p.ApprovedBy,
                        ApprovalTime = p.ApprovalTime
                    }).ToList())
                };

            default:
                throw new NotImplementedException($"Approval handling not implemented for model type: {modelType}");
        }
    }
    
    public List<ResponsibleApprovalStage> GetCurrentApprovalStage(List<ResponsibleApprovalStage> stages)
    {
        var result = new List<ResponsibleApprovalStage>();

        // Sort by order first
        var sortedStages = stages.OrderBy(s => s.Order).ToList();

        // Find the next required unapproved stage
        var nextRequired = sortedStages.FirstOrDefault(s => s.Required && s.Status != ApprovalStatus.Approved);

        if (nextRequired == null)
        {
            // If there's no more required stages, return any unapproved unrequired ones
            result.AddRange(
                sortedStages
                    .Where(s => !s.Required && s.Status != ApprovalStatus.Approved)
            );
            return result;
        }

        // Get all unrequired unapproved stages that come *before* the required one
        var priorUnrequired = sortedStages
            .Where(s => !s.Required && s.Status != ApprovalStatus.Approved && s.Order < nextRequired.Order)
            .ToList();

        result.AddRange(priorUnrequired);
        result.Add(nextRequired);

        return result;
    }
    
    
    public async Task CreateInitialApprovalsAsync(string modelType, Guid modelId)
    {
        var approval = await context.Approvals.FirstOrDefaultAsync(a => a.ItemType == modelType);
        if (approval == null) return;
        
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
                await CreateRequisitionApprovals(modelId, approvalStages, approval);
                break;

            case nameof(BillingSheet):
                await CreateBillingSheetApprovals(modelId, approvalStages, approval);
                break;

            case nameof(PurchaseOrder):
                await CreatePurchaseOrderApprovals(modelId, approvalStages, approval);
                break;
            
            case nameof(LeaveRequest):
                await CreateLeaveRequestApprovals(modelId, approvalStages, approval);
                break;

            default:
                throw new NotSupportedException($"Approval creation not supported for model type '{modelType}'");
        }
    }

    private async Task CreateRequisitionApprovals(Guid requisitionId,  List<ApprovalStage> stages, Approval approval)
    {
        var approvals = stages.Select(stage => new RequisitionApproval
        {
            Required = stage.Required,
            Order = stage.Order,
            RequisitionId = requisitionId,
            CreatedAt = DateTime.UtcNow,
            ApprovalId = approval.Id,
            UserId = stage.UserId,
            RoleId = stage.RoleId,
            ActivatedAt = stage.Order == 1 ? DateTime.UtcNow : null 
        }).ToList();

        await context.RequisitionApprovals.AddRangeAsync(approvals);
        await context.SaveChangesAsync();
    }

    private async Task CreateBillingSheetApprovals(Guid sheetId, List<ApprovalStage> stages, Approval approval)
    {
        var approvals = stages.Select(stage => new BillingSheetApproval
        {
            Required = stage.Required,
            Order = stage.Order,
            BillingSheetId = sheetId,
            CreatedAt = DateTime.UtcNow,
            ApprovalId = approval.Id,
            UserId = stage.UserId,
            RoleId = stage.RoleId,
            ActivatedAt = stage.Order == 1 ? DateTime.UtcNow : null 
        }).ToList();

        await context.BillingSheetApprovals.AddRangeAsync(approvals);
        await context.SaveChangesAsync();
    }

    private async Task CreatePurchaseOrderApprovals(Guid orderId, List<ApprovalStage> stages, Approval approval)
    {
        var approvals = stages.Select(stage => new PurchaseOrderApproval
        {
            Required = stage.Required,
            Order = stage.Order,
            PurchaseOrderId = orderId,
            CreatedAt = DateTime.UtcNow,
            ApprovalId = approval.Id,
            UserId = stage.UserId,
            RoleId = stage.RoleId,
            ActivatedAt = stage.Order == 1 ? DateTime.UtcNow : null 
        }).ToList();

        await context.PurchaseOrderApprovals.AddRangeAsync(approvals);
        await context.SaveChangesAsync();
    }
    
    private async Task CreateLeaveRequestApprovals(Guid leaveRequestId, List<ApprovalStage> stages, Approval approval)
    {
        var approvals = stages.Select(stage => new LeaveRequestApproval
        {
            Required = stage.Required,
            Order = stage.Order,
            LeaveRequestId = leaveRequestId,
            CreatedAt = DateTime.UtcNow,
            ApprovalId = approval.Id,
            UserId = stage.UserId,
            RoleId = stage.RoleId,
            ActivatedAt = stage.Order == 1 ? DateTime.UtcNow : null 
        }).ToList();

        await context.LeaveRequestApprovals.AddRangeAsync(approvals);
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
            //.Where(s => s.Status != ApprovalStatus.Approved)
            .OrderBy(s => s.Order)
            .Select(s => new ApprovalLog
            {
                User = mapper.Map<CollectionItemDto>(s.ApprovedBy),
                ApprovedAt = s.ApprovalTime,
                Comments = s.Comments,
                Status = s.Status
            })
            .ToList();
    }
    
    public async Task ProcessApprovalEscalations()
    {
        var approvalsWithEscalation = await context.Approvals
            .ToListAsync();

        // Get entities that are not fully approved
        var unapprovedRequisitions = await context.Requisitions
            .Where(r => !r.Approved)
            .Include(requisition => requisition.Approvals)
            .ToListAsync();

        var unapprovedBillingSheets = await context.BillingSheets
            .Where(r => !r.Approved)
            .Include(requisition => requisition.Approvals)
            .ToListAsync();

        var unapprovedPurchaseOrders = await context.PurchaseOrders
            .Where(r => !r.Approved)
            .Include(requisition => requisition.Approvals)
            .ToListAsync();

        foreach (var approval in approvalsWithEscalation)
        {
            switch (approval.ItemType)
            {
                case "PurchaseRequisition":
                case "StockRequisition":
                    var requisition = unapprovedRequisitions.FirstOrDefault(r => r.Approvals.Any(a => a.Approval.Id == approval.Id));
                    if (requisition != null)
                    {
                        await ProcessRequisitionEscalations(requisition.Id, approval.EscalationDuration);
                    }
                    break;
                case "BillingSheet":
                    var billingSheet = unapprovedBillingSheets.FirstOrDefault(bs => bs.Approvals.Any(a => a.Approval.Id == approval.Id));
                    if (billingSheet != null)
                    {
                        await ProcessBillingSheetEscalations(billingSheet.Id, approval.EscalationDuration);
                    }
                    break;
                case "PurchaseOrder":
                    var purchaseOrder = unapprovedPurchaseOrders.FirstOrDefault(po => po.Approvals.Any(a => a.Approval.Id == approval.Id));
                    if (purchaseOrder != null)
                    {
                        await ProcessPurchaseOrderEscalations(purchaseOrder.Id, approval.EscalationDuration);
                    }
                    break;
            }
        }
    }
    
    private async Task ProcessRequisitionEscalations(Guid approvalId, TimeSpan escalationDuration)
    {
        var requisition = await context.Requisitions
            .Include(r => r.Approvals)
            .FirstOrDefaultAsync(r => r.Approvals.Any(a => a.Approval.Id == approvalId));

        if (requisition == null || !requisition.Approvals.Any()) return;

        var currentApprovalStages = GetCurrentApprovalStage(requisition.Approvals.Select(a => new ResponsibleApprovalStage
        {
            Status = a.Status,
            Required = a.Required,
            Order = a.Order,
            CreatedAt = a.CreatedAt,
            ActivatedAt = a.ActivatedAt // Include ActivatedAt
        }).ToList());

        if (currentApprovalStages.Count <= 1) return;

        var stagesToAutoApprove = currentApprovalStages
            .Where(s => s.Status == ApprovalStatus.Pending && s.ActivatedAt.HasValue && (DateTime.UtcNow - s.ActivatedAt.Value) > escalationDuration)
            .ToList();

        if (stagesToAutoApprove.Any())
        {
            foreach (var stage in stagesToAutoApprove)
            {
                //var actualStage = requisition.Approvals.First(a => a.Id == stage.Id);
                stage.Status = ApprovalStatus.Approved;
                stage.ApprovalTime = DateTime.UtcNow;
                stage.Comments = "Approval stage exceeded escalation duration.";
                // You might want to record who auto-approved it (e.g., a system user)
            }

            // Check if the requisition is now fully approved
            var allRequiredApproved = requisition.Approvals
                .Where(s => s.Required)
                .All(s => s.Status == ApprovalStatus.Approved);

            if (allRequiredApproved)
            {
                requisition.Approved = true;
            }

            context.Requisitions.Update(requisition);
            await context.SaveChangesAsync();
        }
    }
    private async Task ProcessBillingSheetEscalations(Guid billingSheetId, TimeSpan escalationDuration)
    {
        var billingSheet = await context.BillingSheets
            .Include(bs => bs.Approvals)
            .FirstOrDefaultAsync(bs => bs.Id == billingSheetId); // Use billingSheetId directly

        if (billingSheet == null || !billingSheet.Approvals.Any()) return;

        var currentApprovalStages = GetCurrentApprovalStage(billingSheet.Approvals.Select(a => new ResponsibleApprovalStage
        {
            Status = a.Status,
            Required = a.Required,
            Order = a.Order,
            CreatedAt = a.CreatedAt,
            ActivatedAt = a.ActivatedAt
        }).ToList());

        if (currentApprovalStages.Count <= 1) return;

        var stagesToAutoApprove = currentApprovalStages
            .Where(s => s.Status == ApprovalStatus.Pending && s.ActivatedAt.HasValue && (DateTime.UtcNow - s.ActivatedAt.Value) > escalationDuration)
            .ToList();

        if (stagesToAutoApprove.Any())
        {
            foreach (var stage in stagesToAutoApprove)
            {
                //var actualStage = billingSheet.Approvals.First(a => a.Id == stage.Id);
                stage.Status = ApprovalStatus.Approved;
                stage.ApprovalTime = DateTime.UtcNow;
                stage.Comments = "Approval stage exceeded escalation duration.";
            }

            var allRequiredApproved = billingSheet.Approvals
                .Where(s => s.Required)
                .All(s => s.Status == ApprovalStatus.Approved);

            if (allRequiredApproved)
            {
                billingSheet.Approved = true;
            }

            context.BillingSheets.Update(billingSheet);
            await context.SaveChangesAsync();
        }
    }

    private async Task ProcessPurchaseOrderEscalations(Guid purchaseOrderId, TimeSpan escalationDuration)
    {
        var purchaseOrder = await context.PurchaseOrders
            .Include(po => po.Approvals)
            .FirstOrDefaultAsync(po => po.Id == purchaseOrderId); // Use purchaseOrderId directly

        if (purchaseOrder == null || !purchaseOrder.Approvals.Any()) return;

        var currentApprovalStages = GetCurrentApprovalStage(purchaseOrder.Approvals.Select(a =>
            new ResponsibleApprovalStage
            {
                Status = a.Status,
                Required = a.Required,
                Order = a.Order,
                CreatedAt = a.CreatedAt,
                ActivatedAt = a.ActivatedAt
            }).ToList());

        if (currentApprovalStages.Count <= 1) return;

        var stagesToAutoApprove = currentApprovalStages
            .Where(s => s.Status == ApprovalStatus.Pending && s.ActivatedAt.HasValue &&
                        (DateTime.UtcNow - s.ActivatedAt.Value) > escalationDuration)
            .ToList();

        if (stagesToAutoApprove.Any())
        {
            foreach (var stage in stagesToAutoApprove)
            {
                //var actualStage = purchaseOrder.Approvals.First(a => a.Id == stage.Id);
                stage.Status = ApprovalStatus.Approved;
                stage.ApprovalTime = DateTime.UtcNow;
                stage.Comments = "Approval stage exceeded escalation duration.";
            }

            var allRequiredApproved = purchaseOrder.Approvals
                .Where(s => s.Required)
                .All(s => s.Status == ApprovalStatus.Approved);

            if (allRequiredApproved)
            {
                purchaseOrder.Approved = true;
            }

            await context.SaveChangesAsync();
        }
    }
}

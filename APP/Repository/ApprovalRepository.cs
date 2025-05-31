using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.LeaveRequests;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.StaffRequisitions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SHARED;

namespace APP.Repository;
public class ApprovalRepository(ApplicationDbContext context, IMapper mapper, IMemoryCache cache) : IApprovalRepository 
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
                .AsSplitQuery()
                .Include(r => r.Approvals).Include(requisition => requisition.Items)
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
                requisition.Status = RequestStatus.Pending;
                foreach (var item in requisition.Items)
                {
                    item.Status = RequestStatus.Pending;
                }
                context.Requisitions.Update(requisition);
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
            await AddApprovalLogs(new CreateApprovalLog
            {
                UserId = userId,
                Comments = comments,
                Status = ApprovalStatus.Approved,
                ModelId = requisition.Id,
            });
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
                    purchaseOrder.Status = PurchaseOrderStatus.Pending;
                    context.PurchaseOrders.Update(purchaseOrder);
                }
                await context.SaveChangesAsync();
                
                //activate next pending stages
                var nextPendingStages = purchaseOrder.Approvals
                    .Where(s => s.Status == ApprovalStatus.Pending && s.ActivatedAt == null)
                    .OrderBy(s => s.Order)
                    .ToList();

                if (nextPendingStages.Any())
                {
                    // Get the current approval stages after the approval
                    var updatedApprovalStages = purchaseOrder.Approvals.Select(item => new ResponsibleApprovalStage
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
                        .Where(s => !purchaseOrder.Approvals.First(ra =>
                            (ra.UserId == s.UserId && s.UserId.HasValue) || (ra.RoleId == s.RoleId && s.RoleId.HasValue)).ActivatedAt.HasValue)
                        .ToList();

                    foreach (var stageToActivate in newlyActiveStages)
                    {
                        var actualStage = purchaseOrder.Approvals.First(ra =>
                            (ra.UserId == stageToActivate.UserId && stageToActivate.UserId.HasValue) || (ra.RoleId == stageToActivate.RoleId && stageToActivate.RoleId.HasValue));
                        actualStage.ActivatedAt = DateTime.UtcNow;
                        context.PurchaseOrderApprovals.Update(actualStage);
                    }
                }
                await context.SaveChangesAsync();
                await AddApprovalLogs(new CreateApprovalLog
                {
                    UserId = userId,
                    Comments = comments,
                    Status = ApprovalStatus.Approved,
                    ModelId = purchaseOrder.Id,
                });
                return Result.Success();

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
                    billingSheet.Status = BillingSheetStatus.Pending;
                    context.BillingSheets.Update(billingSheet);
                }
                await context.SaveChangesAsync();
                
                //activate next pending stages
                var nextBillingStages = billingSheet.Approvals
                    .Where(s => s.Status == ApprovalStatus.Pending && s.ActivatedAt == null)
                    .OrderBy(s => s.Order)
                    .ToList();

                if (nextBillingStages.Any())
                {
                    // Get the current approval stages after the approval
                    var updatedApprovalStages = billingSheet.Approvals.Select(item => new ResponsibleApprovalStage
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
                        .Where(s => !billingSheet.Approvals.First(ra =>
                            (ra.UserId == s.UserId && s.UserId.HasValue) || (ra.RoleId == s.RoleId && s.RoleId.HasValue)).ActivatedAt.HasValue)
                        .ToList();

                    foreach (var stageToActivate in newlyActiveStages)
                    {
                        var actualStage = billingSheet.Approvals.First(ra =>
                            (ra.UserId == stageToActivate.UserId && stageToActivate.UserId.HasValue) || (ra.RoleId == stageToActivate.RoleId && stageToActivate.RoleId.HasValue));
                        actualStage.ActivatedAt = DateTime.UtcNow;
                        context.BillingSheetApprovals.Update(actualStage);
                    }
                }
                await context.SaveChangesAsync();
                await AddApprovalLogs(new CreateApprovalLog
                {
                    UserId = userId,
                    Comments = comments,
                    Status = ApprovalStatus.Approved,
                    ModelId = billingSheet.Id,
                });
                return Result.Success();
            
            
            case nameof(StaffRequisition):
                var staffRequisition = await context.StaffRequisitions
                    .Include(lr => lr.Approvals)
                    .FirstOrDefaultAsync(lr => lr.Id == modelId);
                
                if (staffRequisition is null)
                    return Error.Validation("StaffRequisition.NotFound", $"Staff Requisition {modelId} not found.");
                
                var staffRequisitionApprovalStages = staffRequisition.Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments
                        
                    }).ToList();
                
                var staffRequisitionCurrentApprovals = GetCurrentApprovalStage(staffRequisitionApprovalStages);
                
                var staffRequisitionApprovingStage = staffRequisitionCurrentApprovals.FirstOrDefault(stage =>
                    stage.UserId == userId || (stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value)));
                
                if (staffRequisitionApprovingStage == null)
                {
                    return Error.Validation("Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time.");
                }
                
                // Approve the leave request stage in the actual tracked list
                var stageToApproveSr = staffRequisition.Approvals.First(
                    stage => (stage.UserId == staffRequisitionApprovingStage.UserId && stage.UserId == userId) ||
                    (stage.RoleId == staffRequisitionApprovingStage.RoleId && staffRequisitionApprovingStage.RoleId.HasValue && roleIds.Contains(staffRequisitionApprovingStage.RoleId.Value)));

                stageToApproveSr.Status = ApprovalStatus.Approved;
                stageToApproveSr.ApprovalTime = DateTime.UtcNow;
                stageToApproveSr.Comments = comments;
                
                // Optionally mark staff requisition as fully approved
                var allRequiredSrApproved = staffRequisition.Approvals
                    .Where(s => s.Required)
                    .All(s => s.Status == ApprovalStatus.Approved);
                if (allRequiredSrApproved)
                {
                    staffRequisition.Approved = true;
                    staffRequisition.StaffRequisitionStatus = StaffRequisitionStatus.Approved;
                    context.StaffRequisitions.Update(staffRequisition);
                }
                await context.SaveChangesAsync();
                
                //activate next pending stages
                var nextStaffRequisitionStage = staffRequisition.Approvals
                    .Where(s => s.Status == ApprovalStatus.Pending && s.ActivatedAt == null)
                    .OrderBy(s => s.Order)
                    .ToList();

                if (nextStaffRequisitionStage.Any())
                {
                    // Get the current approval stages after the approval
                    var updatedApprovalStages = staffRequisition.Approvals.Select(item => new ResponsibleApprovalStage
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
                        .Where(s => !staffRequisition.Approvals.First(ra =>
                            (ra.UserId == s.UserId && s.UserId.HasValue) || (ra.RoleId == s.RoleId && s.RoleId.HasValue)).ActivatedAt.HasValue)
                        .ToList();

                    foreach (var stageToActivate in newlyActiveStages)
                    {
                        var actualStage = staffRequisition.Approvals.First(ra =>
                            (ra.UserId == stageToActivate.UserId && stageToActivate.UserId.HasValue) || (ra.RoleId == stageToActivate.RoleId && stageToActivate.RoleId.HasValue));
                        actualStage.ActivatedAt = DateTime.UtcNow;
                        context.StaffRequisitionApprovals.Update(actualStage);
                    }
                }
                await context.SaveChangesAsync();
                await AddApprovalLogs(new CreateApprovalLog
                {
                    UserId = userId,
                    Comments = comments,
                    Status = ApprovalStatus.Approved,
                    ModelId = staffRequisition.Id,
                });
                return Result.Success();
            
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
                
                // Optionally mark a leave request as fully approved
                var allRequiredLrApproved = leaveRequest.Approvals
                    .Where(s => s.Required)
                    .All(s => s.Status == ApprovalStatus.Approved);
                if (allRequiredLrApproved)
                {
                    leaveRequest.Approved = true;
                    leaveRequest.LeaveStatus = LeaveStatus.Approved;
                    context.LeaveRequests.Update(leaveRequest);
                }
                await context.SaveChangesAsync();
                
                //activate next pending stages
                var nextLeaveStage = leaveRequest.Approvals
                    .Where(s => s.Status == ApprovalStatus.Pending && s.ActivatedAt == null)
                    .OrderBy(s => s.Order)
                    .ToList();

                if (nextLeaveStage.Any())
                {
                    // Get the current approval stages after the approval
                    var updatedApprovalStages = leaveRequest.Approvals.Select(item => new ResponsibleApprovalStage
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
                        .Where(s => !leaveRequest.Approvals.First(ra =>
                            (ra.UserId == s.UserId && s.UserId.HasValue) || (ra.RoleId == s.RoleId && s.RoleId.HasValue)).ActivatedAt.HasValue)
                        .ToList();

                    foreach (var stageToActivate in newlyActiveStages)
                    {
                        var actualStage = leaveRequest.Approvals.First(ra =>
                            (ra.UserId == stageToActivate.UserId && stageToActivate.UserId.HasValue) || (ra.RoleId == stageToActivate.RoleId && stageToActivate.RoleId.HasValue));
                        actualStage.ActivatedAt = DateTime.UtcNow;
                        context.LeaveRequestApprovals.Update(actualStage);
                    }
                }
                await context.SaveChangesAsync();
                await AddApprovalLogs(new CreateApprovalLog
                {
                    UserId = userId,
                    Comments = comments,
                    Status = ApprovalStatus.Approved,
                    ModelId = leaveRequest.Id,
                });
                return Result.Success();
            
            default:
                return Error.Validation("Approval.InvalidType",
                    $"Unsupported model type: {modelType}");
        }
    }
    
    public async Task<Result> RejectItem(string modelType, Guid modelId, Guid userId, List<Guid> roleIds, string comments = null)
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

            stageToApprove.Status = ApprovalStatus.Rejected;
            stageToApprove.Comments = comments;
            stageToApprove.ApprovedById = userId;
            context.RequisitionApprovals.Update(stageToApprove);
            await context.SaveChangesAsync();
            await AddApprovalLogs(new CreateApprovalLog
            {
                UserId = userId,
                Comments = comments,
                Status = ApprovalStatus.Rejected,
                ModelId = requisition.Id,
            });
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

                stageToApprovePo.Status = ApprovalStatus.Rejected;
                stageToApprovePo.Comments = comments;
                stageToApprovePo.ApprovedById = userId;
                await context.SaveChangesAsync();
                await AddApprovalLogs(new CreateApprovalLog
                {
                    UserId = userId,
                    Comments = comments,
                    Status = ApprovalStatus.Rejected,
                    ModelId = purchaseOrder.Id,
                });
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
                await context.SaveChangesAsync();
                await AddApprovalLogs(new CreateApprovalLog
                {
                    UserId = userId,
                    Comments = comments,
                    Status = ApprovalStatus.Approved,
                    ModelId = billingSheet.Id,
                });
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
                await AddApprovalLogs(new CreateApprovalLog
                {
                    UserId = userId,
                    Comments = comments,
                    Status = ApprovalStatus.Approved,
                    ModelId = leaveRequest.Id,
                });
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
                ApprovalLogs = GetApprovalLogs(po.Id)
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
                    ApprovalLogs = GetApprovalLogs(r.Id)
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
                ApprovalLogs = GetApprovalLogs(bs.Id)
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
                ApprovalLogs = GetApprovalLogs(bs.Id)
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
                    ApprovalLogs = GetApprovalLogs(modelId)
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
                    ApprovalLogs = GetApprovalLogs(modelId)
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
                    ApprovalLogs = GetApprovalLogs(modelId)
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
                    ApprovalLogs = GetApprovalLogs(modelId)
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

        if (approvalStages.Count == 0)
        {
            switch (modelType)
            {
                case "RawStockRequisition" or "PackageStockRequisition" or "PurchaseRequisition" or "Requisition":
                    var requisition = await context.Requisitions.FirstOrDefaultAsync(r => r.Id == modelId);
                    if (requisition != null)
                    {
                        requisition.Status = RequestStatus.Pending;
                        context.Requisitions.Update(requisition);
                        await context.SaveChangesAsync();
                    }

                    break;

                case nameof(BillingSheet):
                    var billingSheet = await context.BillingSheets.FirstOrDefaultAsync(r => r.Id == modelId);
                    if (billingSheet != null)
                    {
                        billingSheet.Status = BillingSheetStatus.Pending;
                        context.BillingSheets.Update(billingSheet);
                        await context.SaveChangesAsync();
                    }

                    break;

                case nameof(PurchaseOrder):
                    var purchaseOrder = await context.PurchaseOrders.FirstOrDefaultAsync(r => r.Id == modelId);
                    if (purchaseOrder != null)
                    {
                        purchaseOrder.Status = PurchaseOrderStatus.Pending;
                        context.PurchaseOrders.Update(purchaseOrder);
                        await context.SaveChangesAsync();
                    }

                    break;

                case nameof(LeaveRequest):
                    var leaveRequest = await context.LeaveRequests.FirstOrDefaultAsync(r => r.Id == modelId);
                    if (leaveRequest != null)
                    {
                        leaveRequest.LeaveStatus = LeaveStatus.Pending;
                        context.LeaveRequests.Update(leaveRequest);
                        await context.SaveChangesAsync();
                    }
                    break;
            }
            return;
        }

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

    private async Task AddApprovalLogs(CreateApprovalLog log)
    {
        await context.ApprovalActionLogs.AddAsync(new ApprovalActionLog
        {
            UserId = log.UserId,
            ModelId = log.ModelId,
            Status = log.Status,
            CreatedAt = DateTime.UtcNow,
            Comments = log.Comments,
        });
        await context.SaveChangesAsync();
    }
    
    public List<ApprovalLog> GetApprovalLogs(Guid modelId)
    {
        return context.ApprovalActionLogs
            .AsSplitQuery()
            .Include(log => log.User)
            .Where(log => log.ModelId == modelId)
            .OrderBy(log => log.CreatedAt)
            .Select(log => new ApprovalLog
            {
                User = mapper.Map<CollectionItemDto>(log.User),
                ApprovedAt = log.CreatedAt,
                Comments = log.Comments,
                Status = log.Status
            })
            .ToList();
    }
    
    public async Task ProcessApprovalEscalations()
    {
        var cacheKey = "ApprovalsWithEscalation";
        if (!cache.TryGetValue(cacheKey, out List<Approval> approvalsWithEscalation))
        {
            approvalsWithEscalation = await context.Approvals.ToListAsync();

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(10)); // Adjust expiration as needed

            cache.Set(cacheKey, approvalsWithEscalation, cacheEntryOptions);
        }

        // Get entities that are not fully approved
        var unapprovedRequisitions = await context.Requisitions
            .AsSplitQuery()
            .Where(r => !r.Approved)
            .Include(requisition => requisition.Approvals)
            .ToListAsync();

        var unapprovedBillingSheets = await context.BillingSheets
            .AsSplitQuery()
            .Where(r => !r.Approved)
            .Include(requisition => requisition.Approvals)
            .ToListAsync();

        var unapprovedPurchaseOrders = await context.PurchaseOrders
            .AsSplitQuery()
            .Where(r => !r.Approved)
            .Include(requisition => requisition.Approvals)
            .ToListAsync();
        
        var unapprovedLeaveRequests = await context.LeaveRequests
            .AsSplitQuery()
            .Where(r => !r.Approved)
            .Include(requisition => requisition.Approvals)
            .ToListAsync();

        foreach (var approval in approvalsWithEscalation)
        {
            switch (approval.ItemType)
            {
                case "PurchaseRequisition":
                case "StockRequisition":
                    var requisition = unapprovedRequisitions.FirstOrDefault(r => r.Approvals.Any(a => a.ApprovalId == approval.Id));
                    if (requisition != null)
                    {
                        await ProcessRequisitionEscalations(requisition, approval.EscalationDuration);
                    }
                    break;
                case "BillingSheet":
                    var billingSheet = unapprovedBillingSheets.FirstOrDefault(bs => bs.Approvals.Any(a => a.ApprovalId == approval.Id));
                    if (billingSheet != null)
                    {
                        await ProcessBillingSheetEscalations(billingSheet, approval.EscalationDuration);
                    }
                    break;
                case "PurchaseOrder":
                    var purchaseOrder = unapprovedPurchaseOrders.FirstOrDefault(po => po.Approvals.Any(a => a.ApprovalId == approval.Id));
                    if (purchaseOrder != null)
                    {
                        await ProcessPurchaseOrderEscalations(purchaseOrder, approval.EscalationDuration);
                    }
                    break;
                case nameof(LeaveRequest):
                    var leaveRequest = unapprovedLeaveRequests.FirstOrDefault(po => po.Approvals.Any(a => a.ApprovalId == approval.Id));
                    if (leaveRequest != null)
                    {
                        await ProcessLeaveRequestEscalations(leaveRequest, approval.EscalationDuration);
                    }
                    break;
            }
        }
    }
    
    private async Task ProcessRequisitionEscalations(Requisition requisition, TimeSpan escalationDuration)
    {
        if (requisition is null || requisition.Approvals.Count == 0) return;

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
    private async Task ProcessBillingSheetEscalations(BillingSheet billingSheet, TimeSpan escalationDuration)
    {
        if (billingSheet is null || billingSheet.Approvals.Count == 0) return;

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

    private async Task ProcessPurchaseOrderEscalations(PurchaseOrder purchaseOrder, TimeSpan escalationDuration)
    {
        if (purchaseOrder is null || purchaseOrder.Approvals.Count == 0) return;

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
    
    private async Task ProcessLeaveRequestEscalations(LeaveRequest leaveRequest, TimeSpan escalationDuration)
    {
        if (leaveRequest is null || leaveRequest.Approvals.Count == 0) return;

        var currentApprovalStages = GetCurrentApprovalStage(leaveRequest.Approvals.Select(a =>
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

            var allRequiredApproved = leaveRequest.Approvals
                .Where(s => s.Required)
                .All(s => s.Status == ApprovalStatus.Approved);

            if (allRequiredApproved)
            {
                leaveRequest.Approved = true;
            }

            await context.SaveChangesAsync();
        }
    }
}

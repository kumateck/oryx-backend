using APP.Extensions;
using APP.IRepository;
using APP.Repository.QcWorksheets;
using APP.Services.ProductionActivityStepEventPublisher;
using APP.Services.QcWorksheets;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.JobRequests;
using DOMAIN.Entities.LeaveRequests;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.OvertimeRequests;
using DOMAIN.Entities.Payments;
using DOMAIN.Entities.Payroll;
using DOMAIN.Entities.Performance;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProformaInvoices;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.RndProjects;
using DOMAIN.Entities.RndFormulations;
using DOMAIN.Entities.RndTechnologyTransfers;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.StaffRequisitions;
using DOMAIN.Entities.StockAdjustments;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SHARED;

namespace APP.Repository;

public class ApprovalRepository(
    ApplicationDbContext context,
    IMapper mapper,
    UserManager<User> userManager,
    IMemoryCache cache,
    ILogger<ApprovalRepository> logger,
    IServiceProvider serviceProvider,
    IProductionActivityStepEventPublisher stepEventPublisher
) : IApprovalRepository
{
    public async Task<Result<Guid>> CreateApproval(CreateApprovalRequest request, Guid userId)
    {
        var validation = ApprovalConfigurationValidator.Validate(request);
        if (!validation.IsSuccess)
            return Result.Failure<Guid>(validation.Errors);

        if (
            await context.Approvals.FirstOrDefaultAsync(a => a.ItemType == request.ItemType)
            is not null
        )
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
        var approval = await context
            .Approvals.AsSplitQuery()
            .Include(a => a.ApprovalStages)
                .ThenInclude(a => a.User)
            .Include(a => a.ApprovalStages)
                .ThenInclude(s => s.Role)
            .FirstOrDefaultAsync(a => a.Id == approvalId);

        return approval is null
            ? Error.NotFound("Approval.NotFound", "Approval was not found")
            : mapper.Map<ApprovalDto>(approval);
    }

    public async Task<Result<Paginateable<IEnumerable<ApprovalDto>>>> GetApprovals(
        int page,
        int pageSize,
        string searchQuery
    )
    {
        var query = context
            .Approvals.AsSplitQuery()
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

    public async Task<Result> UpdateApproval(
        CreateApprovalRequest request,
        Guid approvalId,
        Guid userId
    )
    {
        var validation = ApprovalConfigurationValidator.Validate(request);
        if (!validation.IsSuccess)
            return validation;

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

    public async Task<Result> ApproveItem(
        string modelType,
        Guid modelId,
        Guid userId,
        List<Guid> roleIds,
        string comments = null
    )
    {
        if (modelType == nameof(AllocateProductionOrder))
            return await AllocateProductionOrderApprovalHandler.ApproveAsync(
                context, modelId, userId, roleIds, comments);

        if (modelType == nameof(CustomerQuotation))
            return await CustomerQuotationApprovalHandler.ApproveAsync(
                context, modelId, userId, roleIds, comments);

        if (modelType == nameof(RndProject))
            return await RndProjectApprovalHandler.ApproveAsync(
                context, modelId, userId, roleIds, comments);
        if (modelType == nameof(RndFormulation))
            return await RndFormulationApprovalHandler.ApproveAsync(
                context, modelId, userId, roleIds, comments);
        if (modelType == nameof(RndTechnologyTransfer))
            return await RndTechnologyTransferApprovalHandler.ApproveAsync(
                context, modelId, userId, roleIds, comments);

        // Rebuilt QC module. The handler refuses unless the caller re-authenticated during
        // this request, so reaching this through the generic approval endpoint cannot sign
        // a QC document — see IQcReauthContext.
        if (QcWorksheetModelTypes.IsQcWorksheetModelType(modelType))
            return await QcApprovalHandler.ApproveAsync(
                context,
                serviceProvider.GetService<IQcReauthContext>(),
                modelType, modelId, userId, roleIds, comments);

        if (modelType is "PurchaseRequisition" or "StockRequisition" or "TrialRequisition")
        {
            var requisition = await context
                .Requisitions.AsSplitQuery()
                .Include(r => r.Approvals)
                .Include(requisition => requisition.Items)
                .FirstOrDefaultAsync(r => r.Id == modelId);

            if (requisition is null)
                return RequisitionErrors.NotFound(modelId);

            var expectedType = modelType switch
            {
                "PurchaseRequisition" => RequisitionType.Purchase,
                "StockRequisition" => RequisitionType.Stock,
                "TrialRequisition" => RequisitionType.Trial,
                _ => throw new NotSupportedException($"Unsupported requisition modelType '{modelType}'."),
            };

            if (requisition.RequisitionType != expectedType)
            {
                return Error.Validation(
                    "Approval.TypeMismatch",
                    $"Requisition type mismatch. Expected {expectedType} but got {requisition.RequisitionType}."
                );
            }

            var approvalStages = requisition
                .Approvals.Select(item => new ResponsibleApprovalStage
                {
                    RoleId = item.RoleId,
                    UserId = item.UserId,
                    Order = item.Order,
                    Status = item.Status,
                    Required = item.Required,
                    ApprovalTime = item.ApprovalTime,
                    Comments = item.Comments,
                })
                .ToList();

            var currentApprovals = GetCurrentApprovalStage(approvalStages, userId, roleIds[0]);

            var approvableStage = currentApprovals.FirstOrDefault();

            if (approvableStage == null)
            {
                return Error.Validation(
                    "Approval.Unauthorized",
                    "You are not authorized to approve this resource at this time."
                );
            }

            // Approve the stage in the actual tracked list (not the mapped one)
            var stageToApprove = requisition.Approvals.First(stage =>
                stage.Status != ApprovalStatus.Approved && stage.Order == approvableStage.Order
            );

            stageToApprove.Status = ApprovalStatus.Approved;
            stageToApprove.ApprovalTime = DateTime.UtcNow;
            stageToApprove.Comments = comments;
            stageToApprove.ApprovedById = userId;
            context.RequisitionApprovals.Update(stageToApprove);

            // Optionally mark requisition as fully approved if all required stages are approved
            var allRequiredApproved = requisition
                .Approvals.Where(s => s.Required)
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
            var nextPendingStages = requisition
                .Approvals.Where(s => s.Status == ApprovalStatus.Pending && s.ActivatedAt == null)
                .OrderBy(s => s.Order)
                .ToList();

            if (nextPendingStages.Count != 0)
            {
                // Get the current approval stages after the approval
                var updatedApprovalStages = requisition
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var newlyActiveStages = GetCurrentApprovalStage(
                        updatedApprovalStages,
                        userId,
                        roleIds[0]
                    )
                    .Where(s => !s.ActivatedAt.HasValue)
                    .ToList();

                foreach (var stageToActivate in newlyActiveStages)
                {
                    var actualStage = requisition.Approvals.First(ra =>
                        ra.Status != ApprovalStatus.Approved
                        && (
                            ra.UserId == stageToActivate.UserId && stageToActivate.UserId.HasValue
                            || (
                                ra.RoleId == stageToActivate.RoleId
                                && stageToActivate.RoleId.HasValue
                            )
                        )
                    );
                    actualStage.ActivatedAt = DateTime.UtcNow;
                    context.RequisitionApprovals.Update(actualStage);
                }
            }
            await context.SaveChangesAsync();
            await AddApprovalLogs(
                new CreateApprovalLog
                {
                    UserId = userId,
                    Comments = comments,
                    Status = ApprovalStatus.Approved,
                    ModelId = requisition.Id,
                }
            );
            return Result.Success();
        }

        // Handle other models
        switch (modelType)
        {
            case nameof(PurchaseOrder):
                var purchaseOrder = await context
                    .PurchaseOrders.Include(po => po.Approvals)
                    .FirstOrDefaultAsync(po => po.Id == modelId);

                if (purchaseOrder is null)
                    return Error.Validation(
                        "PurchaseOrder.NotFound",
                        $"Purchase Order {modelId} not found."
                    );

                var purchaseOrderApprovalStages = purchaseOrder
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var purchaseOrderCurrentApprovals = GetCurrentApprovalStage(
                    purchaseOrderApprovalStages,
                    userId,
                    roleIds[0]
                );

                var purchaseOrderApprovingStage = purchaseOrderCurrentApprovals.FirstOrDefault();

                if (purchaseOrderApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                // Approve the purchase order stage in the actual tracked list
                var stageToApprovePo = purchaseOrder.Approvals.First(stage =>
                    stage.Status != ApprovalStatus.Approved
                    && stage.Order == purchaseOrderApprovingStage.Order
                );

                stageToApprovePo.Status = ApprovalStatus.Approved;
                stageToApprovePo.ApprovalTime = DateTime.UtcNow;
                stageToApprovePo.Comments = comments;
                stageToApprovePo.ApprovedById = userId;

                // Optionally mark purchase order as fully approved
                var allRequiredPoApproved = purchaseOrder
                    .Approvals.Where(s => s.Required)
                    .All(s => s.Status == ApprovalStatus.Approved);

                if (allRequiredPoApproved)
                {
                    purchaseOrder.Approved = true;
                    purchaseOrder.Status = PurchaseOrderStatus.Approved;
                    purchaseOrder.Approved = true;
                    context.PurchaseOrders.Update(purchaseOrder);
                }
                await context.SaveChangesAsync();

                //activate next pending stages
                var nextPendingStages = purchaseOrder
                    .Approvals.Where(s =>
                        s.Status == ApprovalStatus.Pending && s.ActivatedAt == null
                    )
                    .OrderBy(s => s.Order)
                    .ToList();

                if (nextPendingStages.Count != 0)
                {
                    // Get the current approval stages after the approval
                    var updatedApprovalStages = purchaseOrder
                        .Approvals.Select(item => new ResponsibleApprovalStage
                        {
                            RoleId = item.RoleId,
                            UserId = item.UserId,
                            Order = item.Order,
                            Status = item.Status,
                            Required = item.Required,
                            ApprovalTime = item.ApprovalTime,
                            Comments = item.Comments,
                        })
                        .ToList();

                    var newlyActiveStages = GetCurrentApprovalStage(
                            updatedApprovalStages,
                            userId,
                            roleIds[0]
                        )
                        .Where(s => !s.ActivatedAt.HasValue)
                        .ToList();

                    foreach (var stageToActivate in newlyActiveStages)
                    {
                        var actualStage = purchaseOrder.Approvals.First(ra =>
                            ra.Status != ApprovalStatus.Approved
                            && (
                                ra.UserId == stageToActivate.UserId
                                    && stageToActivate.UserId.HasValue
                                || (
                                    ra.RoleId == stageToActivate.RoleId
                                    && stageToActivate.RoleId.HasValue
                                )
                            )
                        );
                        actualStage.ActivatedAt = DateTime.UtcNow;
                        context.PurchaseOrderApprovals.Update(actualStage);
                    }
                }
                await context.SaveChangesAsync();
                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Approved,
                        ModelId = purchaseOrder.Id,
                    }
                );
                return Result.Success();

            case nameof(BillingSheet):
                var billingSheet = await context
                    .BillingSheets.Include(bs => bs.Approvals)
                    .FirstOrDefaultAsync(bs => bs.Id == modelId);

                if (billingSheet is null)
                    return Error.Validation(
                        "BillingSheet.NotFound",
                        $"Billing Sheet {modelId} not found."
                    );

                var billingSheetApprovalStages = billingSheet
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var billingSheetCurrentApprovals = GetCurrentApprovalStage(
                    billingSheetApprovalStages,
                    userId,
                    roleIds[0]
                );

                var billingSheetApprovingStage = billingSheetCurrentApprovals.FirstOrDefault();

                if (billingSheetApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                // Approve the billing sheet stage in the actual tracked list
                var stageToApproveBs = billingSheet.Approvals.First(stage =>
                    stage.Status != ApprovalStatus.Approved
                    && stage.Order == billingSheetApprovingStage.Order
                );

                stageToApproveBs.Status = ApprovalStatus.Approved;
                stageToApproveBs.ApprovalTime = DateTime.UtcNow;
                stageToApproveBs.Comments = comments;
                stageToApproveBs.ApprovedById = userId;

                // Optionally mark billing sheet as fully approved
                var allRequiredBsApproved = billingSheet
                    .Approvals.Where(s => s.Required)
                    .All(s => s.Status == ApprovalStatus.Approved);

                if (allRequiredBsApproved)
                {
                    billingSheet.Approved = true;
                    billingSheet.Status = BillingSheetStatus.Pending;
                    context.BillingSheets.Update(billingSheet);
                }
                await context.SaveChangesAsync();

                //activate next pending stages
                var nextBillingStages = billingSheet
                    .Approvals.Where(s =>
                        s.Status == ApprovalStatus.Pending && s.ActivatedAt == null
                    )
                    .OrderBy(s => s.Order)
                    .ToList();

                if (nextBillingStages.Count != 0)
                {
                    // Get the current approval stages after the approval
                    var updatedApprovalStages = billingSheet
                        .Approvals.Select(item => new ResponsibleApprovalStage
                        {
                            RoleId = item.RoleId,
                            UserId = item.UserId,
                            Order = item.Order,
                            Status = item.Status,
                            Required = item.Required,
                            ApprovalTime = item.ApprovalTime,
                            Comments = item.Comments,
                        })
                        .ToList();

                    var newlyActiveStages = GetCurrentApprovalStage(
                            updatedApprovalStages,
                            userId,
                            roleIds[0]
                        )
                        .Where(s => !s.ActivatedAt.HasValue)
                        .ToList();

                    foreach (var stageToActivate in newlyActiveStages)
                    {
                        var actualStage = billingSheet.Approvals.First(ra =>
                            ra.Status != ApprovalStatus.Approved
                            && (
                                ra.UserId == stageToActivate.UserId
                                    && stageToActivate.UserId.HasValue
                                || (
                                    ra.RoleId == stageToActivate.RoleId
                                    && stageToActivate.RoleId.HasValue
                                )
                            )
                        );
                        actualStage.ActivatedAt = DateTime.UtcNow;
                        context.BillingSheetApprovals.Update(actualStage);
                    }
                }
                await context.SaveChangesAsync();
                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Approved,
                        ModelId = billingSheet.Id,
                    }
                );
                return Result.Success();

            case nameof(StaffRequisition):
                var staffRequisition = await context
                    .StaffRequisitions.Include(lr => lr.Approvals)
                    .FirstOrDefaultAsync(lr => lr.Id == modelId);

                if (staffRequisition is null)
                    return Error.Validation(
                        "StaffRequisition.NotFound",
                        $"Staff Requisition {modelId} not found."
                    );

                var staffRequisitionApprovalStages = staffRequisition
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var staffRequisitionCurrentApprovals = GetCurrentApprovalStage(
                    staffRequisitionApprovalStages,
                    userId,
                    roleIds[0]
                );

                var staffRequisitionApprovingStage =
                    staffRequisitionCurrentApprovals.FirstOrDefault();

                if (staffRequisitionApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                // Approve the leave request stage in the actual tracked list
                var stageToApproveSr = staffRequisition.Approvals.First(stage =>
                    stage.Status != ApprovalStatus.Approved
                    && stage.Order == staffRequisitionApprovingStage.Order
                );

                stageToApproveSr.Status = ApprovalStatus.Approved;
                stageToApproveSr.ApprovalTime = DateTime.UtcNow;
                stageToApproveSr.Comments = comments;
                stageToApproveSr.ApprovedById = userId;

                // Optionally mark staff requisition as fully approved
                var allRequiredSrApproved = staffRequisition
                    .Approvals.Where(s => s.Required)
                    .All(s => s.Status == ApprovalStatus.Approved);
                if (allRequiredSrApproved)
                {
                    staffRequisition.Approved = true;
                    staffRequisition.StaffRequisitionStatus = StaffRequisitionStatus.Approved;
                    context.StaffRequisitions.Update(staffRequisition);
                }
                await context.SaveChangesAsync();

                //activate next pending stages
                var nextStaffRequisitionStage = staffRequisition
                    .Approvals.Where(s =>
                        s.Status == ApprovalStatus.Pending && s.ActivatedAt == null
                    )
                    .OrderBy(s => s.Order)
                    .ToList();

                if (nextStaffRequisitionStage.Count != 0)
                {
                    // Get the current approval stages after the approval
                    var updatedApprovalStages = staffRequisition
                        .Approvals.Select(item => new ResponsibleApprovalStage
                        {
                            RoleId = item.RoleId,
                            UserId = item.UserId,
                            Order = item.Order,
                            Status = item.Status,
                            Required = item.Required,
                            ApprovalTime = item.ApprovalTime,
                            Comments = item.Comments,
                        })
                        .ToList();

                    var newlyActiveStages = GetCurrentApprovalStage(
                            updatedApprovalStages,
                            userId,
                            roleIds[0]
                        )
                        .Where(s => !s.ActivatedAt.HasValue)
                        .ToList();

                    foreach (var stageToActivate in newlyActiveStages)
                    {
                        var actualStage = staffRequisition.Approvals.First(ra =>
                            ra.Status != ApprovalStatus.Approved
                            && (
                                ra.UserId == stageToActivate.UserId
                                    && stageToActivate.UserId.HasValue
                                || (
                                    ra.RoleId == stageToActivate.RoleId
                                    && stageToActivate.RoleId.HasValue
                                )
                            )
                        );
                        actualStage.ActivatedAt = DateTime.UtcNow;
                        context.StaffRequisitionApprovals.Update(actualStage);
                    }
                }
                await context.SaveChangesAsync();
                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Approved,
                        ModelId = staffRequisition.Id,
                    }
                );
                return Result.Success();

            case nameof(LeaveRequest):
                var leaveRequest = await context
                    .LeaveRequests.Include(lr => lr.Approvals)
                    .Include(lr => lr.Employee)
                    .FirstOrDefaultAsync(lr => lr.Id == modelId);

                if (leaveRequest is null)
                    return Error.Validation(
                        "LeaveRequest.NotFound",
                        $"Leave Request {modelId} not found."
                    );

                var leaveRequestApprovalStages = leaveRequest
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var leaveRequestCurrentApprovals = GetCurrentApprovalStage(
                    leaveRequestApprovalStages,
                    userId,
                    roleIds[0]
                );

                var leaveRequestApprovingStage = leaveRequestCurrentApprovals.FirstOrDefault();

                if (leaveRequestApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                // Approve the leave request stage in the actual tracked list
                var stageToApproveLr = leaveRequest.Approvals.First(stage =>
                    stage.Status != ApprovalStatus.Approved
                    && stage.Order == leaveRequestApprovingStage.Order
                );

                stageToApproveLr.Status = ApprovalStatus.Approved;
                stageToApproveLr.ApprovalTime = DateTime.UtcNow;
                stageToApproveLr.Comments = comments;
                //context.LeaveRequestApprovals.Update(stageToApproveLr);

                // Optionally mark a leave request as fully approved
                var allRequiredLrApproved = leaveRequest
                    .Approvals.Where(s => s.Required)
                    .All(s => s.Status == ApprovalStatus.Approved);
                if (allRequiredLrApproved)
                {
                    leaveRequest.Approved = true;
                    leaveRequest.LeaveStatus = LeaveStatus.Approved;
                    context.LeaveRequests.Update(leaveRequest);
                }
                await context.SaveChangesAsync();

                //activate next pending stages
                var nextLeaveStage = leaveRequest
                    .Approvals.Where(s =>
                        s.Status == ApprovalStatus.Pending && s.ActivatedAt == null
                    )
                    .OrderBy(s => s.Order)
                    .ToList();

                if (nextLeaveStage.Count != 0)
                {
                    // Get the current approval stages after the approval
                    var updatedApprovalStages = leaveRequest
                        .Approvals.Select(item => new ResponsibleApprovalStage
                        {
                            RoleId = item.RoleId,
                            UserId = item.UserId,
                            Order = item.Order,
                            Status = item.Status,
                            Required = item.Required,
                            ApprovalTime = item.ApprovalTime,
                            Comments = item.Comments,
                        })
                        .ToList();

                    var newlyActiveStages = GetCurrentApprovalStage(
                            updatedApprovalStages,
                            userId,
                            roleIds[0]
                        )
                        .Where(s => !s.ActivatedAt.HasValue)
                        .ToList();

                    foreach (var stageToActivate in newlyActiveStages)
                    {
                        var actualStage = leaveRequest.Approvals.First(ra =>
                            ra.Status != ApprovalStatus.Approved
                            && (
                                ra.UserId == stageToActivate.UserId
                                    && stageToActivate.UserId.HasValue
                                || (
                                    ra.RoleId == stageToActivate.RoleId
                                    && stageToActivate.RoleId.HasValue
                                )
                            )
                        );
                        actualStage.ActivatedAt = DateTime.UtcNow;
                        context.LeaveRequestApprovals.Update(actualStage);
                    }
                }
                await context.SaveChangesAsync();
                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Approved,
                        ModelId = leaveRequest.Id,
                    }
                );
                return Result.Success();

            case nameof(PayrollRun):
                var payrollRun = await context
                    .PayrollRuns.Include(pr => pr.Approvals)
                    .FirstOrDefaultAsync(pr => pr.Id == modelId);

                if (payrollRun is null)
                    return Error.Validation(
                        "PayrollRun.NotFound",
                        $"Payroll run {modelId} not found."
                    );

                var payrollRunApprovalStages = payrollRun
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var payrollRunCurrentApprovals = GetCurrentApprovalStage(
                    payrollRunApprovalStages,
                    userId,
                    roleIds[0]
                );

                var payrollRunApprovingStage = payrollRunCurrentApprovals.FirstOrDefault();

                if (payrollRunApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                var stageToApprovePr = payrollRun.Approvals.First(stage =>
                    stage.Status != ApprovalStatus.Approved
                    && stage.Order == payrollRunApprovingStage.Order
                );

                stageToApprovePr.Status = ApprovalStatus.Approved;
                stageToApprovePr.ApprovalTime = DateTime.UtcNow;
                stageToApprovePr.Comments = comments;
                stageToApprovePr.ApprovedById = userId;

                var allRequiredPrApproved = payrollRun
                    .Approvals.Where(s => s.Required)
                    .All(s => s.Status == ApprovalStatus.Approved);

                if (allRequiredPrApproved)
                {
                    payrollRun.Approved = true;
                    payrollRun.Status = PayrollRunStatus.Approved;
                    context.PayrollRuns.Update(payrollRun);
                }
                await context.SaveChangesAsync();

                var nextPayrollRunStages = payrollRun
                    .Approvals.Where(s =>
                        s.Status == ApprovalStatus.Pending && s.ActivatedAt == null
                    )
                    .OrderBy(s => s.Order)
                    .ToList();

                if (nextPayrollRunStages.Count != 0)
                {
                    var updatedApprovalStages = payrollRun
                        .Approvals.Select(item => new ResponsibleApprovalStage
                        {
                            RoleId = item.RoleId,
                            UserId = item.UserId,
                            Order = item.Order,
                            Status = item.Status,
                            Required = item.Required,
                            ApprovalTime = item.ApprovalTime,
                            Comments = item.Comments,
                        })
                        .ToList();

                    var newlyActiveStages = GetCurrentApprovalStage(
                            updatedApprovalStages,
                            userId,
                            roleIds[0]
                        )
                        .Where(s => !s.ActivatedAt.HasValue)
                        .ToList();

                    foreach (var stageToActivate in newlyActiveStages)
                    {
                        var actualStage = payrollRun.Approvals.First(ra =>
                            ra.Status != ApprovalStatus.Approved
                            && (
                                ra.UserId == stageToActivate.UserId && stageToActivate.UserId.HasValue
                                || (
                                    ra.RoleId == stageToActivate.RoleId
                                    && stageToActivate.RoleId.HasValue
                                )
                            )
                        );
                        actualStage.ActivatedAt = DateTime.UtcNow;
                        context.PayrollRunApprovals.Update(actualStage);
                    }
                }
                await context.SaveChangesAsync();
                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Approved,
                        ModelId = payrollRun.Id,
                    }
                );
                return Result.Success();

            case nameof(PerformanceReview):
                var performanceReview = await context
                    .PerformanceReviews.Include(pr => pr.Approvals)
                    .FirstOrDefaultAsync(pr => pr.Id == modelId);

                if (performanceReview is null)
                    return Error.Validation(
                        "PerformanceReview.NotFound",
                        $"Performance review {modelId} not found."
                    );

                var performanceReviewApprovalStages = performanceReview
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var performanceReviewCurrentApprovals = GetCurrentApprovalStage(
                    performanceReviewApprovalStages,
                    userId,
                    roleIds[0]
                );

                var performanceReviewApprovingStage = performanceReviewCurrentApprovals.FirstOrDefault();

                if (performanceReviewApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                var stageToApprovePrf = performanceReview.Approvals.First(stage =>
                    stage.Status != ApprovalStatus.Approved
                    && stage.Order == performanceReviewApprovingStage.Order
                );

                stageToApprovePrf.Status = ApprovalStatus.Approved;
                stageToApprovePrf.ApprovalTime = DateTime.UtcNow;
                stageToApprovePrf.Comments = comments;
                stageToApprovePrf.ApprovedById = userId;

                var allRequiredPrfApproved = performanceReview
                    .Approvals.Where(s => s.Required)
                    .All(s => s.Status == ApprovalStatus.Approved);

                if (allRequiredPrfApproved)
                {
                    performanceReview.Approved = true;
                    performanceReview.Status = PerformanceReviewStatus.Completed;
                    context.PerformanceReviews.Update(performanceReview);
                }
                await context.SaveChangesAsync();

                var nextPerformanceReviewStages = performanceReview
                    .Approvals.Where(s =>
                        s.Status == ApprovalStatus.Pending && s.ActivatedAt == null
                    )
                    .OrderBy(s => s.Order)
                    .ToList();

                if (nextPerformanceReviewStages.Count != 0)
                {
                    var updatedApprovalStages = performanceReview
                        .Approvals.Select(item => new ResponsibleApprovalStage
                        {
                            RoleId = item.RoleId,
                            UserId = item.UserId,
                            Order = item.Order,
                            Status = item.Status,
                            Required = item.Required,
                            ApprovalTime = item.ApprovalTime,
                            Comments = item.Comments,
                        })
                        .ToList();

                    var newlyActiveStages = GetCurrentApprovalStage(
                            updatedApprovalStages,
                            userId,
                            roleIds[0]
                        )
                        .Where(s => !s.ActivatedAt.HasValue)
                        .ToList();

                    foreach (var stageToActivate in newlyActiveStages)
                    {
                        var actualStage = performanceReview.Approvals.First(ra =>
                            ra.Status != ApprovalStatus.Approved
                            && (
                                ra.UserId == stageToActivate.UserId && stageToActivate.UserId.HasValue
                                || (
                                    ra.RoleId == stageToActivate.RoleId
                                    && stageToActivate.RoleId.HasValue
                                )
                            )
                        );
                        actualStage.ActivatedAt = DateTime.UtcNow;
                        context.PerformanceReviewApprovals.Update(actualStage);
                    }
                }
                await context.SaveChangesAsync();
                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Approved,
                        ModelId = performanceReview.Id,
                    }
                );
                return Result.Success();

            case nameof(OvertimeRequest):
                var overtimeRequest = await context
                    .OvertimeRequests.Include(lr => lr.Approvals)
                    .FirstOrDefaultAsync(lr => lr.Id == modelId);

                if (overtimeRequest is null)
                    return Error.Validation(
                        "OvertimeRequest.NotFound",
                        $"Overtime Request {modelId} not found."
                    );

                var overtimeRequestApprovalStages = overtimeRequest
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var overtimeRequestCurrentApprovals = GetCurrentApprovalStage(
                    overtimeRequestApprovalStages,
                    userId,
                    roleIds[0]
                );

                var overtimeRequestApprovingStage =
                    overtimeRequestCurrentApprovals.FirstOrDefault();

                if (overtimeRequestApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                // Approve the overtime request stage in the actual tracked list
                var stageToApproveOr = overtimeRequest.Approvals.First(stage =>
                    stage.Status != ApprovalStatus.Approved
                    && stage.Order == overtimeRequestApprovingStage.Order
                );

                stageToApproveOr.Status = ApprovalStatus.Approved;
                stageToApproveOr.ApprovalTime = DateTime.UtcNow;
                stageToApproveOr.Comments = comments;
                //context.OvertimeRequestApprovals.Update(stageToApproveOr);

                // Optionally mark a overtime request as fully approved
                var allRequiredOrApproved = overtimeRequest
                    .Approvals.Where(s => s.Required)
                    .All(s => s.Status == ApprovalStatus.Approved);
                if (allRequiredOrApproved)
                {
                    overtimeRequest.Approved = true;
                    overtimeRequest.Status = OvertimeStatus.Approved;
                    overtimeRequest.ApprovalStatus = ApprovalStatus.Approved;
                    context.OvertimeRequests.Update(overtimeRequest);
                }
                await context.SaveChangesAsync();

                //activate next pending stages
                var nextOvertimeStage = overtimeRequest
                    .Approvals.Where(s =>
                        s.Status == ApprovalStatus.Pending && s.ActivatedAt == null
                    )
                    .OrderBy(s => s.Order)
                    .ToList();

                if (nextOvertimeStage.Count != 0)
                {
                    // Get the current approval stages after the approval
                    var updatedApprovalStages = overtimeRequest
                        .Approvals.Select(item => new ResponsibleApprovalStage
                        {
                            RoleId = item.RoleId,
                            UserId = item.UserId,
                            Order = item.Order,
                            Status = item.Status,
                            Required = item.Required,
                            ApprovalTime = item.ApprovalTime,
                            Comments = item.Comments,
                        })
                        .ToList();

                    var newlyActiveStages = GetCurrentApprovalStage(
                            updatedApprovalStages,
                            userId,
                            roleIds[0]
                        )
                        .Where(s => !s.ActivatedAt.HasValue)
                        .ToList();

                    foreach (var stageToActivate in newlyActiveStages)
                    {
                        var actualStage = overtimeRequest.Approvals.First(ra =>
                            ra.Status != ApprovalStatus.Approved
                            && (
                                ra.UserId == stageToActivate.UserId
                                    && stageToActivate.UserId.HasValue
                                || (
                                    ra.RoleId == stageToActivate.RoleId
                                    && stageToActivate.RoleId.HasValue
                                )
                            )
                        );
                        actualStage.ActivatedAt = DateTime.UtcNow;
                        context.OvertimeRequestApprovals.Update(actualStage);
                    }
                }
                await context.SaveChangesAsync();
                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Approved,
                        ModelId = overtimeRequest.Id,
                    }
                );
                return Result.Success();

            case nameof(Response):
                var response = await context
                    .Responses.IgnoreQueryFilters()
                    .AsSplitQuery()
                    .Include(lr => lr.Approvals)
                    .Include(response => response.MaterialBatch)
                    .Include(response => response.BatchManufacturingRecord)
                        .ThenInclude(b => b.ProductionScheduleProduct)
                    .FirstOrDefaultAsync(lr => lr.Id == modelId);

                if (response is null)
                    return Error.Validation("Response.NotFound", $"Response {modelId} not found.");

                var currentResponseApprovals = ResponseApprovalRoundManager.Current(
                    response.Approvals
                );
                var responseApprovalStages = currentResponseApprovals
                    .Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var responseCurrentApprovals = GetCurrentApprovalStage(
                    responseApprovalStages,
                    userId,
                    roleIds[0]
                );

                var responseApprovingStage = responseCurrentApprovals.FirstOrDefault();

                if (responseApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                // Approve the leave request stage in the actual tracked list
                var stageToApproveRe = currentResponseApprovals.First(stage =>
                    stage.Status == ApprovalStatus.Pending
                    && stage.Order == responseApprovingStage.Order
                );

                stageToApproveRe.Status = ApprovalStatus.Approved;
                stageToApproveRe.ApprovalTime = DateTime.UtcNow;
                stageToApproveRe.Comments = comments;
                stageToApproveRe.ApprovedById = userId;
                //context.ResponseApprovals.Update(stageToApproveRe);

                // Optionally mark a leave request as fully approved
                var allRequiredReApproved = currentResponseApprovals
                    .Where(s => s.Required)
                    .All(s => s.Status == ApprovalStatus.Approved);

                if (allRequiredReApproved)
                {
                    var finalApproval = await ResponseFinalApproval.ApplyAsync(
                        context, response, userId);
                    if (!finalApproval.IsSuccess)
                        return finalApproval;
                }
                await context.SaveChangesAsync();

                if (allRequiredReApproved && response.ProductionActivityStepId.HasValue)
                {
                    await stepEventPublisher.PublishStatusChanged(
                        response.ProductionActivityStepId.Value,
                        ProductionStatus.Completed,
                        userId
                    );
                }

                //activate next pending stages
                var nextResponseStage = currentResponseApprovals
                    .Where(s =>
                        s.Status == ApprovalStatus.Pending && s.ActivatedAt == null
                    )
                    .OrderBy(s => s.Order)
                    .ToList();

                if (nextResponseStage.Count != 0)
                {
                    // Get the current approval stages after the approval
                    var updatedApprovalStages = currentResponseApprovals
                        .Select(item => new ResponsibleApprovalStage
                        {
                            RoleId = item.RoleId,
                            UserId = item.UserId,
                            Order = item.Order,
                            Status = item.Status,
                            Required = item.Required,
                            ApprovalTime = item.ApprovalTime,
                            Comments = item.Comments,
                        })
                        .ToList();

                    var newlyActiveStages = GetCurrentApprovalStage(
                            updatedApprovalStages,
                            userId,
                            roleIds[0]
                        )
                        .Where(s => !s.ActivatedAt.HasValue)
                        .ToList();

                    foreach (var stageToActivate in newlyActiveStages)
                    {
                        var actualStage = currentResponseApprovals.First(ra =>
                            ra.Status != ApprovalStatus.Approved
                            && (
                                ra.UserId == stageToActivate.UserId
                                    && stageToActivate.UserId.HasValue
                                || (
                                    ra.RoleId == stageToActivate.RoleId
                                    && stageToActivate.RoleId.HasValue
                                )
                            )
                        );
                        actualStage.ActivatedAt = DateTime.UtcNow;
                        context.ResponseApprovals.Update(actualStage);
                    }
                }
                await context.SaveChangesAsync();
                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Approved,
                        ModelId = response.Id,
                    }
                );
                return Result.Success();

            case nameof(ProformaInvoice):
                var proformaInvoice = await context
                    .ProformaInvoices.AsSplitQuery()
                    .Include(a => a.Approvals)
                    .Include(proformaInvoice => proformaInvoice.AllocateProductionOrder)
                    .FirstOrDefaultAsync(lr => lr.Id == modelId);

                if (proformaInvoice is null)
                    return Error.Validation(
                        "AllocationProductionOrder.NotFound",
                        $"Allocation production order {modelId} not found."
                    );

                var allocateProductionOrderApprovalStages = proformaInvoice
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var allocationCurrentApprovals = GetCurrentApprovalStage(
                    allocateProductionOrderApprovalStages,
                    userId,
                    roleIds[0]
                );

                var allocationApprovingStage = allocationCurrentApprovals.FirstOrDefault();

                if (allocationApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                // Approve the leave request stage in the actual tracked list
                var stageToApproveAl = proformaInvoice.Approvals.First(stage =>
                    stage.Status != ApprovalStatus.Approved
                    && stage.Order == allocationApprovingStage.Order
                );

                stageToApproveAl.Status = ApprovalStatus.Approved;
                stageToApproveAl.ApprovalTime = DateTime.UtcNow;
                stageToApproveAl.Comments = comments;
                stageToApproveAl.ApprovedById = userId;
                //context.ProductionOrderApprovals.Update(stageToApproveAl);

                // Optionally mark a leave request as fully approved
                var allRequiredAlApproved = proformaInvoice
                    .Approvals.Where(s => s.Required)
                    .All(s => s.Status == ApprovalStatus.Approved);

                if (allRequiredAlApproved)
                {
                    proformaInvoice.Approved = true;
                    proformaInvoice.AllocateProductionOrder.Approved = true;
                    context.ProformaInvoices.Update(proformaInvoice);
                }

                await context.SaveChangesAsync();

                //activate next pending stages
                var nextAllocationStage = proformaInvoice
                    .Approvals.Where(s =>
                        s.Status == ApprovalStatus.Pending && s.ActivatedAt == null
                    )
                    .OrderBy(s => s.Order)
                    .ToList();

                if (nextAllocationStage.Count != 0)
                {
                    // Get the current approval stages after the approval
                    var updatedApprovalStages = proformaInvoice
                        .Approvals.Select(item => new ResponsibleApprovalStage
                        {
                            RoleId = item.RoleId,
                            UserId = item.UserId,
                            Order = item.Order,
                            Status = item.Status,
                            Required = item.Required,
                            ApprovalTime = item.ApprovalTime,
                            Comments = item.Comments,
                        })
                        .ToList();

                    var newlyActiveStages = GetCurrentApprovalStage(
                            updatedApprovalStages,
                            userId,
                            roleIds[0]
                        )
                        .Where(s => !s.ActivatedAt.HasValue)
                        .ToList();

                    foreach (var stageToActivate in newlyActiveStages)
                    {
                        var actualStage = proformaInvoice.Approvals.First(ra =>
                            ra.Status != ApprovalStatus.Approved
                            && (
                                ra.UserId == stageToActivate.UserId
                                    && stageToActivate.UserId.HasValue
                                || (
                                    ra.RoleId == stageToActivate.RoleId
                                    && stageToActivate.RoleId.HasValue
                                )
                            )
                        );
                        actualStage.ActivatedAt = DateTime.UtcNow;
                        context.ProformaInvoiceApprovals.Update(actualStage);
                    }
                }
                await context.SaveChangesAsync();
                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Approved,
                        ModelId = proformaInvoice.Id,
                    }
                );
                return Result.Success();

            case nameof(ShipmentDocument):
                var shipmentDocument = await context
                    .ShipmentDocuments.AsSplitQuery()
                    .Include(a => a.Approvals)
                    .FirstOrDefaultAsync(lr => lr.Id == modelId);

                if (shipmentDocument is null)
                    return Error.Validation(
                        "ShipmentDocument.NotFound",
                        $"Shipment document {modelId} not found."
                    );

                var shipmentDocumentOrderApprovalStages = shipmentDocument
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var shipmentDocumentCurrentApprovals = GetCurrentApprovalStage(
                    shipmentDocumentOrderApprovalStages,
                    userId,
                    roleIds[0]
                );

                var shipmentDocumentApprovingStage =
                    shipmentDocumentCurrentApprovals.FirstOrDefault();

                if (shipmentDocumentApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                // Approve the leave request stage in the actual tracked list
                var stageToApproveSd = shipmentDocument.Approvals.First(stage =>
                    stage.Status != ApprovalStatus.Approved
                    && stage.Order == shipmentDocumentApprovingStage.Order
                );

                stageToApproveSd.Status = ApprovalStatus.Approved;
                stageToApproveSd.ApprovalTime = DateTime.UtcNow;
                stageToApproveSd.Comments = comments;
                stageToApproveSd.ApprovedById = userId;
                //context.ProductionOrderApprovals.Update(stageToApproveAl);

                // Optionally mark a leave request as fully approved
                var allRequireSdlApproved = shipmentDocument
                    .Approvals.Where(s => s.Required)
                    .All(s => s.Status == ApprovalStatus.Approved);

                if (allRequireSdlApproved)
                {
                    shipmentDocument.Approved = true;
                }

                context.ShipmentDocuments.Update(shipmentDocument);
                await context.SaveChangesAsync();

                //activate next pending stages
                var nextShipmentStage = shipmentDocument
                    .Approvals.Where(s =>
                        s.Status == ApprovalStatus.Pending && s.ActivatedAt == null
                    )
                    .OrderBy(s => s.Order)
                    .ToList();

                if (nextShipmentStage.Count != 0)
                {
                    // Get the current approval stages after the approval
                    var updatedApprovalStages = shipmentDocument
                        .Approvals.Select(item => new ResponsibleApprovalStage
                        {
                            RoleId = item.RoleId,
                            UserId = item.UserId,
                            Order = item.Order,
                            Status = item.Status,
                            Required = item.Required,
                            ApprovalTime = item.ApprovalTime,
                            Comments = item.Comments,
                        })
                        .ToList();

                    var newlyActiveStages = GetCurrentApprovalStage(
                            updatedApprovalStages,
                            userId,
                            roleIds[0]
                        )
                        .Where(s => !s.ActivatedAt.HasValue)
                        .ToList();

                    foreach (var stageToActivate in newlyActiveStages)
                    {
                        var actualStage = shipmentDocument.Approvals.First(ra =>
                            ra.Status != ApprovalStatus.Approved
                            && (
                                ra.UserId == stageToActivate.UserId
                                    && stageToActivate.UserId.HasValue
                                || (
                                    ra.RoleId == stageToActivate.RoleId
                                    && stageToActivate.RoleId.HasValue
                                )
                            )
                        );
                        actualStage.ActivatedAt = DateTime.UtcNow;
                        context.ShipmentDocumentApprovals.Update(actualStage);
                    }
                }
                await context.SaveChangesAsync();
                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Approved,
                        ModelId = shipmentDocument.Id,
                    }
                );
                return Result.Success();

            case nameof(JobRequest):
                var jobRequest = await context
                    .JobRequests.AsSplitQuery()
                    .Include(a => a.Approvals)
                    .FirstOrDefaultAsync(lr => lr.Id == modelId);

                if (jobRequest is null)
                    return Error.Validation(
                        "JobRequest.NotFound",
                        $"Job request {modelId} not found."
                    );

                var jobRequestOrderApprovalStages = jobRequest
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var jobRequestCurrentApprovals = GetCurrentApprovalStage(
                    jobRequestOrderApprovalStages,
                    userId,
                    roleIds[0]
                );

                var jobRequestApprovingStage = jobRequestCurrentApprovals.FirstOrDefault();

                if (jobRequestApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                // Approve the job request stage in the actual tracked list
                var stageToApproveJr = jobRequest.Approvals.First(stage =>
                    stage.Status != ApprovalStatus.Approved
                    && stage.Order == jobRequestApprovingStage.Order
                );

                stageToApproveJr.Status = ApprovalStatus.Approved;
                stageToApproveJr.ApprovalTime = DateTime.UtcNow;
                stageToApproveJr.Comments = comments;
                stageToApproveJr.ApprovedById = userId;

                // Check if the job request is fully approved
                var allRequireJrApproved = jobRequest
                    .Approvals.Where(s => s.Required)
                    .All(s => s.Status == ApprovalStatus.Approved);

                if (allRequireJrApproved)
                {
                    jobRequest.Approved = true;
                }

                context.JobRequests.Update(jobRequest);
                await context.SaveChangesAsync();

                // Activate next pending stages
                var nextJobRequestStage = jobRequest
                    .Approvals.Where(s =>
                        s.Status == ApprovalStatus.Pending && s.ActivatedAt == null
                    )
                    .OrderBy(s => s.Order)
                    .ToList();

                if (nextJobRequestStage.Count != 0)
                {
                    // Get the current approval stages after the approval
                    var updatedApprovalStages = jobRequest
                        .Approvals.Select(item => new ResponsibleApprovalStage
                        {
                            RoleId = item.RoleId,
                            UserId = item.UserId,
                            Order = item.Order,
                            Status = item.Status,
                            Required = item.Required,
                            ApprovalTime = item.ApprovalTime,
                            Comments = item.Comments,
                        })
                        .ToList();

                    var newlyActiveStages = GetCurrentApprovalStage(
                            updatedApprovalStages,
                            userId,
                            roleIds[0]
                        )
                        .Where(s => !s.ActivatedAt.HasValue)
                        .ToList();

                    foreach (var stageToActivate in newlyActiveStages)
                    {
                        var actualStage = jobRequest.Approvals.First(ra =>
                            ra.Status != ApprovalStatus.Approved
                            && (
                                ra.UserId == stageToActivate.UserId
                                    && stageToActivate.UserId.HasValue
                                || (
                                    ra.RoleId == stageToActivate.RoleId
                                    && stageToActivate.RoleId.HasValue
                                )
                            )
                        );

                        actualStage.ActivatedAt = DateTime.UtcNow;
                        context.JobRequestApprovals.Update(actualStage);
                    }
                }

                await context.SaveChangesAsync();

                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Approved,
                        ModelId = jobRequest.Id,
                    }
                );

                return Result.Success();

            case nameof(ProductionExtraPacking):
                var productionExtraPacking = await context
                    .ProductionExtraPackings.AsSplitQuery()
                    .Include(a => a.Approvals)
                    .FirstOrDefaultAsync(lr => lr.Id == modelId);

                if (productionExtraPacking is null)
                    return Error.Validation(
                        "ProductionExtraPacking.NotFound",
                        $"Production extra packing {modelId} not found."
                    );

                var productionExtraPackingOrderApprovalStages = productionExtraPacking
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var productionExtraPackingCurrentApprovals = GetCurrentApprovalStage(
                    productionExtraPackingOrderApprovalStages,
                    userId,
                    roleIds[0]
                );

                var productionExtraPackingApprovingStage =
                    productionExtraPackingCurrentApprovals.FirstOrDefault();

                if (productionExtraPackingApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                // Approve the production extra packing stage in the actual tracked list
                var stageToApprovePep = productionExtraPacking.Approvals.First(stage =>
                    stage.Status != ApprovalStatus.Approved
                    && stage.Order == productionExtraPackingApprovingStage.Order
                );

                stageToApprovePep.Status = ApprovalStatus.Approved;
                stageToApprovePep.ApprovalTime = DateTime.UtcNow;
                stageToApprovePep.Comments = comments;
                stageToApprovePep.ApprovedById = userId;

                // Check if the production extra packing is fully approved
                var allRequirePepApproved = productionExtraPacking
                    .Approvals.Where(s => s.Required)
                    .All(s => s.Status == ApprovalStatus.Approved);

                if (allRequirePepApproved)
                {
                    productionExtraPacking.Approved = true;
                    productionExtraPacking.Status = ProductionExtraPackingStatus.InProgress;
                }

                context.ProductionExtraPackings.Update(productionExtraPacking);
                await context.SaveChangesAsync();

                // Activate next pending stages
                var nextPepStage = productionExtraPacking
                    .Approvals.Where(s =>
                        s.Status == ApprovalStatus.Pending && s.ActivatedAt == null
                    )
                    .OrderBy(s => s.Order)
                    .ToList();

                if (nextPepStage.Count != 0)
                {
                    // Get the current approval stages after the approval
                    var updatedApprovalStages = productionExtraPacking
                        .Approvals.Select(item => new ResponsibleApprovalStage
                        {
                            RoleId = item.RoleId,
                            UserId = item.UserId,
                            Order = item.Order,
                            Status = item.Status,
                            Required = item.Required,
                            ApprovalTime = item.ApprovalTime,
                            Comments = item.Comments,
                        })
                        .ToList();

                    var newlyActiveStages = GetCurrentApprovalStage(
                            updatedApprovalStages,
                            userId,
                            roleIds[0]
                        )
                        .Where(s => !s.ActivatedAt.HasValue)
                        .ToList();

                    foreach (var stageToActivate in newlyActiveStages)
                    {
                        var actualStage = productionExtraPacking.Approvals.First(ra =>
                            ra.Status != ApprovalStatus.Approved
                            && (
                                ra.UserId == stageToActivate.UserId
                                    && stageToActivate.UserId.HasValue
                                || (
                                    ra.RoleId == stageToActivate.RoleId
                                    && stageToActivate.RoleId.HasValue
                                )
                            )
                        );

                        actualStage.ActivatedAt = DateTime.UtcNow;
                        context.ProductionExtraPackingApprovals.Update(actualStage);
                    }
                }

                await context.SaveChangesAsync();

                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Approved,
                        ModelId = productionExtraPacking.Id,
                    }
                );

                return Result.Success();

            case nameof(FinishedGoodsTransferNote):
                var finishedGoodsTransferNote = await context
                    .FinishedGoodsTransferNotes.AsSplitQuery()
                    .Include(a => a.Approvals)
                    .FirstOrDefaultAsync(lr => lr.Id == modelId);

                if (finishedGoodsTransferNote is null)
                    return Error.Validation(
                        "FinishedGoodsTransferNote.NotFound",
                        $"Finished goods transfer note {modelId} not found."
                    );

                var fgtnOrderApprovalStages = finishedGoodsTransferNote
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var fgtnCurrentApprovals = GetCurrentApprovalStage(
                    fgtnOrderApprovalStages,
                    userId,
                    roleIds[0]
                );

                var fgtnApprovingStage = fgtnCurrentApprovals.FirstOrDefault();

                if (fgtnApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                // Approve the finished goods transfer note stage in the actual tracked list
                var stageToApproveFgtn = finishedGoodsTransferNote.Approvals.First(stage =>
                    stage.Status != ApprovalStatus.Approved
                    && stage.Order == fgtnApprovingStage.Order
                );

                stageToApproveFgtn.Status = ApprovalStatus.Approved;
                stageToApproveFgtn.ApprovalTime = DateTime.UtcNow;
                stageToApproveFgtn.Comments = comments;
                stageToApproveFgtn.ApprovedById = userId;

                // Check if the finished goods transfer note is fully approved
                var allRequireFgtnApproved = finishedGoodsTransferNote
                    .Approvals.Where(s => s.Required)
                    .All(s => s.Status == ApprovalStatus.Approved);

                if (allRequireFgtnApproved)
                {
                    finishedGoodsTransferNote.Approved = true;
                }

                context.FinishedGoodsTransferNotes.Update(finishedGoodsTransferNote);
                await context.SaveChangesAsync();

                // Activate next pending stages
                var nextFgtnStage = finishedGoodsTransferNote
                    .Approvals.Where(s =>
                        s.Status == ApprovalStatus.Pending && s.ActivatedAt == null
                    )
                    .OrderBy(s => s.Order)
                    .ToList();

                if (nextFgtnStage.Count != 0)
                {
                    // Get the current approval stages after the approval
                    var updatedApprovalStages = finishedGoodsTransferNote
                        .Approvals.Select(item => new ResponsibleApprovalStage
                        {
                            RoleId = item.RoleId,
                            UserId = item.UserId,
                            Order = item.Order,
                            Status = item.Status,
                            Required = item.Required,
                            ApprovalTime = item.ApprovalTime,
                            Comments = item.Comments,
                        })
                        .ToList();

                    var newlyActiveStages = GetCurrentApprovalStage(
                            updatedApprovalStages,
                            userId,
                            roleIds[0]
                        )
                        .Where(s => !s.ActivatedAt.HasValue)
                        .ToList();

                    foreach (var stageToActivate in newlyActiveStages)
                    {
                        var actualStage = finishedGoodsTransferNote.Approvals.First(ra =>
                            ra.Status != ApprovalStatus.Approved
                            && (
                                ra.UserId == stageToActivate.UserId
                                    && stageToActivate.UserId.HasValue
                                || (
                                    ra.RoleId == stageToActivate.RoleId
                                    && stageToActivate.RoleId.HasValue
                                )
                            )
                        );

                        actualStage.ActivatedAt = DateTime.UtcNow;
                        context.FinishedGoodsTransferNoteApprovals.Update(actualStage);
                    }
                }

                await context.SaveChangesAsync();

                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Approved,
                        ModelId = finishedGoodsTransferNote.Id,
                    }
                );

                return Result.Success();

            case nameof(StockAdjustment):
                var stockAdjustment = await context
                    .StockAdjustments.AsSplitQuery()
                    .Include(a => a.Approvals)
                    .Include(a => a.Lines)
                    .FirstOrDefaultAsync(lr => lr.Id == modelId);

                if (stockAdjustment is null)
                    return Error.Validation(
                        "StockAdjustment.NotFound",
                        $"Stock adjustment {modelId} not found."
                    );

                var stockAdjustmentOrderApprovalStages = stockAdjustment
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var stockAdjustmentCurrentApprovals = GetCurrentApprovalStage(
                    stockAdjustmentOrderApprovalStages,
                    userId,
                    roleIds[0]
                );

                var stockAdjustmentApprovingStage =
                    stockAdjustmentCurrentApprovals.FirstOrDefault();

                if (stockAdjustmentApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                // Approve the stage
                var stageToApproveSa = stockAdjustment.Approvals.First(stage =>
                    stage.Status != ApprovalStatus.Approved
                    && stage.Order == stockAdjustmentApprovingStage.Order
                );

                stageToApproveSa.Status = ApprovalStatus.Approved;
                stageToApproveSa.ApprovalTime = DateTime.UtcNow;
                stageToApproveSa.Comments = comments;
                stageToApproveSa.ApprovedById = userId;

                // Check if fully approved
                var allRequireSaApproved = stockAdjustment
                    .Approvals.Where(s => s.Required)
                    .All(s => s.Status == ApprovalStatus.Approved);

                if (allRequireSaApproved)
                {
                    stockAdjustment.Approved = true;
                    // Apply the adjustment
                    var stockRepo =
                        serviceProvider.GetRequiredService<IStockAdjustmentRepository>();
                    var applyResult = await stockRepo.ApplyStockAdjustment(
                        stockAdjustment.Id,
                        userId
                    );
                    if (!applyResult.IsSuccess)
                        return applyResult.Error;
                }

                context.StockAdjustments.Update(stockAdjustment);
                await context.SaveChangesAsync();

                // Activate next stages
                var nextSaStage = stockAdjustment
                    .Approvals.Where(s =>
                        s.Status == ApprovalStatus.Pending && s.ActivatedAt == null
                    )
                    .OrderBy(s => s.Order)
                    .ToList();

                if (nextSaStage.Count != 0)
                {
                    var updatedApprovalStages = stockAdjustment
                        .Approvals.Select(item => new ResponsibleApprovalStage
                        {
                            RoleId = item.RoleId,
                            UserId = item.UserId,
                            Order = item.Order,
                            Status = item.Status,
                            Required = item.Required,
                            ApprovalTime = item.ApprovalTime,
                            Comments = item.Comments,
                        })
                        .ToList();

                    var newlyActiveStages = GetCurrentApprovalStage(
                            updatedApprovalStages,
                            userId,
                            roleIds[0]
                        )
                        .Where(s => !s.ActivatedAt.HasValue)
                        .ToList();

                    foreach (var stageToActivate in newlyActiveStages)
                    {
                        var actualStage = stockAdjustment.Approvals.First(ra =>
                            ra.Status != ApprovalStatus.Approved
                            && (
                                ra.UserId == stageToActivate.UserId
                                    && stageToActivate.UserId.HasValue
                                || (
                                    ra.RoleId == stageToActivate.RoleId
                                    && stageToActivate.RoleId.HasValue
                                )
                            )
                        );

                        actualStage.ActivatedAt = DateTime.UtcNow;
                        context.StockAdjustmentApprovals.Update(actualStage);
                    }
                }

                await context.SaveChangesAsync();

                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Approved,
                        ModelId = stockAdjustment.Id,
                    }
                );

                return Result.Success();

            default:
                return Error.Validation(
                    "Approval.InvalidType",
                    $"Unsupported model type: {modelType}"
                );
        }
    }

    public async Task<Result> RejectItem(
        string modelType,
        Guid modelId,
        Guid userId,
        List<Guid> roleIds,
        string comments = null
    )
    {
        if (modelType == nameof(AllocateProductionOrder))
            return await AllocateProductionOrderApprovalHandler.RejectAsync(
                context, modelId, userId, roleIds, comments);

        if (modelType == nameof(CustomerQuotation))
            return await CustomerQuotationApprovalHandler.RejectAsync(
                context, modelId, userId, roleIds, comments);

        if (modelType == nameof(RndProject))
            return await RndProjectApprovalHandler.RejectAsync(
                context, modelId, userId, roleIds, comments);
        if (modelType == nameof(RndFormulation))
            return await RndFormulationApprovalHandler.RejectAsync(
                context, modelId, userId, roleIds, comments);
        if (modelType == nameof(RndTechnologyTransfer))
            return await RndTechnologyTransferApprovalHandler.RejectAsync(
                context, modelId, userId, roleIds, comments);

        // Rebuilt QC module — same re-authentication gate as ApproveItem above.
        if (QcWorksheetModelTypes.IsQcWorksheetModelType(modelType))
            return await QcApprovalHandler.RejectAsync(
                context,
                serviceProvider.GetService<IQcReauthContext>(),
                modelType, modelId, userId, roleIds, comments);

        if (modelType is "PurchaseRequisition" or "StockRequisition" or "TrialRequisition")
        {
            var requisition = await context
                .Requisitions.Include(r => r.Approvals)
                .FirstOrDefaultAsync(r => r.Id == modelId);

            if (requisition is null)
                return RequisitionErrors.NotFound(modelId);

            var expectedType = modelType switch
            {
                "PurchaseRequisition" => RequisitionType.Purchase,
                "StockRequisition" => RequisitionType.Stock,
                "TrialRequisition" => RequisitionType.Trial,
                _ => throw new NotSupportedException($"Unsupported requisition modelType '{modelType}'."),
            };

            if (requisition.RequisitionType != expectedType)
            {
                return Error.Validation(
                    "Approval.TypeMismatch",
                    $"Requisition type mismatch. Expected {expectedType} but got {requisition.RequisitionType}."
                );
            }

            var approvalStages = requisition
                .Approvals.Select(item => new ResponsibleApprovalStage
                {
                    RoleId = item.RoleId,
                    UserId = item.UserId,
                    Order = item.Order,
                    Status = item.Status,
                    Required = item.Required,
                    ApprovalTime = item.ApprovalTime,
                    Comments = item.Comments,
                })
                .ToList();

            var currentApprovals = GetCurrentApprovalStage(approvalStages, userId, roleIds[0]);

            var approvableStage = currentApprovals.FirstOrDefault();

            if (approvableStage == null)
            {
                return Error.Validation(
                    "Approval.Unauthorized",
                    "You are not authorized to approve this resource at this time."
                );
            }

            // Reject the stage in the actual tracked list (not the mapped one)
            var stageToApprove = requisition.Approvals.First(stage =>
                (stage.UserId == approvableStage.UserId && stage.UserId == userId)
                || (
                    stage.RoleId == approvableStage.RoleId
                    && approvableStage.RoleId.HasValue
                    && roleIds.Contains(approvableStage.RoleId.Value)
                )
            );

            stageToApprove.Status = ApprovalStatus.Rejected;
            stageToApprove.Comments = comments;
            stageToApprove.ApprovedById = userId;
            context.RequisitionApprovals.Update(stageToApprove);
            await context.SaveChangesAsync();
            await AddApprovalLogs(
                new CreateApprovalLog
                {
                    UserId = userId,
                    Comments = comments,
                    Status = ApprovalStatus.Rejected,
                    ModelId = requisition.Id,
                }
            );
        }

        // Handle other models
        switch (modelType)
        {
            case nameof(PurchaseOrder):
                var purchaseOrder = await context
                    .PurchaseOrders.Include(po => po.Approvals)
                    .FirstOrDefaultAsync(po => po.Id == modelId);

                if (purchaseOrder is null)
                    return Error.Validation(
                        "PurchaseOrder.NotFound",
                        $"Purchase Order {modelId} not found."
                    );

                var purchaseOrderApprovalStages = purchaseOrder
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var purchaseOrderCurrentApprovals = GetCurrentApprovalStage(
                    purchaseOrderApprovalStages,
                    userId,
                    roleIds[0]
                );

                var purchaseOrderApprovingStage = purchaseOrderCurrentApprovals.FirstOrDefault();

                if (purchaseOrderApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                // Approve the purchase order stage in the actual tracked list
                var stageToApprovePo = purchaseOrder.Approvals.First(stage =>
                    (stage.UserId == purchaseOrderApprovingStage.UserId && stage.UserId == userId)
                    || (
                        stage.RoleId == purchaseOrderApprovingStage.RoleId
                        && purchaseOrderApprovingStage.RoleId.HasValue
                        && roleIds.Contains(purchaseOrderApprovingStage.RoleId.Value)
                    )
                );

                stageToApprovePo.Status = ApprovalStatus.Rejected;
                stageToApprovePo.Comments = comments;
                stageToApprovePo.ApprovedById = userId;
                await context.SaveChangesAsync();
                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Rejected,
                        ModelId = purchaseOrder.Id,
                    }
                );
                break;

            case nameof(BillingSheet):
                var billingSheet = await context
                    .BillingSheets.Include(bs => bs.Approvals)
                    .FirstOrDefaultAsync(bs => bs.Id == modelId);

                if (billingSheet is null)
                    return Error.Validation(
                        "BillingSheet.NotFound",
                        $"Billing Sheet {modelId} not found."
                    );

                var billingSheetApprovalStages = billingSheet
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var billingSheetCurrentApprovals = GetCurrentApprovalStage(
                    billingSheetApprovalStages,
                    userId,
                    roleIds[0]
                );

                var billingSheetApprovingStage = billingSheetCurrentApprovals.FirstOrDefault();

                if (billingSheetApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                // Approve the billing sheet stage in the actual tracked list
                var stageToApproveBs = billingSheet.Approvals.First(stage =>
                    (stage.UserId == billingSheetApprovingStage.UserId && stage.UserId == userId)
                    || (
                        stage.RoleId == billingSheetApprovingStage.RoleId
                        && billingSheetApprovingStage.RoleId.HasValue
                        && roleIds.Contains(billingSheetApprovingStage.RoleId.Value)
                    )
                );

                stageToApproveBs.Status = ApprovalStatus.Rejected;
                stageToApproveBs.ApprovalTime = DateTime.UtcNow;
                stageToApproveBs.Comments = comments;
                await context.SaveChangesAsync();
                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Rejected,
                        ModelId = billingSheet.Id,
                    }
                );
                break;

            case nameof(LeaveRequest):
            {
                var leaveRequest = await context
                    .LeaveRequests.Include(lr => lr.Approvals)
                    .FirstOrDefaultAsync(lr => lr.Id == modelId);

                if (leaveRequest is null)
                    return Error.Validation(
                        "LeaveRequest.NotFound",
                        $"Leave Request {modelId} not found."
                    );

                var leaveRequestApprovalStages = leaveRequest
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var leaveRequestCurrentApprovals = GetCurrentApprovalStage(
                    leaveRequestApprovalStages,
                    userId,
                    roleIds[0]
                );

                var leaveRequestApprovingStage = leaveRequestCurrentApprovals.FirstOrDefault();

                if (leaveRequestApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                // Approve the leave request stage in the actual tracked list
                var stageToApproveLr = leaveRequest.Approvals.First(stage =>
                    (stage.UserId == leaveRequestApprovingStage.UserId && stage.UserId == userId)
                    || (
                        stage.RoleId == leaveRequestApprovingStage.RoleId
                        && leaveRequestApprovingStage.RoleId.HasValue
                        && roleIds.Contains(leaveRequestApprovingStage.RoleId.Value)
                    )
                );

                stageToApproveLr.Status = ApprovalStatus.Rejected;
                stageToApproveLr.ApprovalTime = DateTime.UtcNow;
                stageToApproveLr.Comments = comments;
                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Rejected,
                        ModelId = leaveRequest.Id,
                    }
                );
                await context.SaveChangesAsync();

                await using var transaction = await context.Database.BeginTransactionAsync();

                var employee = await context.Employees.FirstOrDefaultAsync(e =>
                    e.Id == leaveRequest.EmployeeId
                );

                if (employee != null)
                {
                    employee.AnnualLeaveDays +=
                        leaveRequest.PaidDays ?? 0 + leaveRequest.UnpaidDays ?? 0;
                    await context.SaveChangesAsync();
                }

                leaveRequest.LeaveStatus = LeaveStatus.Rejected;

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                break;
            }

            case nameof(PayrollRun):
            {
                var payrollRun = await context
                    .PayrollRuns.Include(pr => pr.Approvals)
                    .FirstOrDefaultAsync(pr => pr.Id == modelId);

                if (payrollRun is null)
                    return Error.Validation(
                        "PayrollRun.NotFound",
                        $"Payroll run {modelId} not found."
                    );

                var payrollRunApprovalStages = payrollRun
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var payrollRunCurrentApprovals = GetCurrentApprovalStage(
                    payrollRunApprovalStages,
                    userId,
                    roleIds[0]
                );

                var payrollRunApprovingStage = payrollRunCurrentApprovals.FirstOrDefault();

                if (payrollRunApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                var stageToApprovePr = payrollRun.Approvals.First(stage =>
                    (stage.UserId == payrollRunApprovingStage.UserId && stage.UserId == userId)
                    || (
                        stage.RoleId == payrollRunApprovingStage.RoleId
                        && payrollRunApprovingStage.RoleId.HasValue
                        && roleIds.Contains(payrollRunApprovingStage.RoleId.Value)
                    )
                );

                stageToApprovePr.Status = ApprovalStatus.Rejected;
                stageToApprovePr.ApprovalTime = DateTime.UtcNow;
                stageToApprovePr.Comments = comments;

                payrollRun.Status = PayrollRunStatus.Draft;
                context.PayrollRuns.Update(payrollRun);

                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Rejected,
                        ModelId = payrollRun.Id,
                    }
                );
                await context.SaveChangesAsync();

                break;
            }

            case nameof(PerformanceReview):
            {
                var performanceReview = await context
                    .PerformanceReviews.Include(pr => pr.Approvals)
                    .FirstOrDefaultAsync(pr => pr.Id == modelId);

                if (performanceReview is null)
                    return Error.Validation(
                        "PerformanceReview.NotFound",
                        $"Performance review {modelId} not found."
                    );

                var performanceReviewApprovalStages = performanceReview
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var performanceReviewCurrentApprovals = GetCurrentApprovalStage(
                    performanceReviewApprovalStages,
                    userId,
                    roleIds[0]
                );

                var performanceReviewApprovingStage = performanceReviewCurrentApprovals.FirstOrDefault();

                if (performanceReviewApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                var stageToApprovePrf = performanceReview.Approvals.First(stage =>
                    (stage.UserId == performanceReviewApprovingStage.UserId && stage.UserId == userId)
                    || (
                        stage.RoleId == performanceReviewApprovingStage.RoleId
                        && performanceReviewApprovingStage.RoleId.HasValue
                        && roleIds.Contains(performanceReviewApprovingStage.RoleId.Value)
                    )
                );

                stageToApprovePrf.Status = ApprovalStatus.Rejected;
                stageToApprovePrf.ApprovalTime = DateTime.UtcNow;
                stageToApprovePrf.Comments = comments;

                performanceReview.Status = PerformanceReviewStatus.ManagerReview;
                context.PerformanceReviews.Update(performanceReview);

                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Rejected,
                        ModelId = performanceReview.Id,
                    }
                );
                await context.SaveChangesAsync();

                break;
            }

            case nameof(OvertimeRequest):
                var overtimeRequest = await context
                    .OvertimeRequests.Include(lr => lr.Approvals)
                    .FirstOrDefaultAsync(lr => lr.Id == modelId);

                if (overtimeRequest is null)
                    return Error.Validation(
                        "OvertimeRequest.NotFound",
                        $"overtime Request {modelId} not found."
                    );

                var overtimeRequestApprovalStages = overtimeRequest
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var overtimeRequestCurrentApprovals = GetCurrentApprovalStage(
                    overtimeRequestApprovalStages,
                    userId,
                    roleIds[0]
                );

                var overtimeRequestApprovingStage =
                    overtimeRequestCurrentApprovals.FirstOrDefault();

                if (overtimeRequestApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                // Approve the overtime request stage in the actual tracked list
                var stageToApproveOr = overtimeRequest.Approvals.First(stage =>
                    (stage.UserId == overtimeRequestApprovingStage.UserId && stage.UserId == userId)
                    || (
                        stage.RoleId == overtimeRequestApprovingStage.RoleId
                        && overtimeRequestApprovingStage.RoleId.HasValue
                        && roleIds.Contains(overtimeRequestApprovingStage.RoleId.Value)
                    )
                );

                stageToApproveOr.Status = ApprovalStatus.Rejected;
                stageToApproveOr.ApprovalTime = DateTime.UtcNow;
                stageToApproveOr.Comments = comments;
                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Rejected,
                        ModelId = overtimeRequest.Id,
                    }
                );
                await context.SaveChangesAsync();

                overtimeRequest.Status = OvertimeStatus.Rejected;
                context.OvertimeRequests.Update(overtimeRequest);
                await context.SaveChangesAsync();
                break;

            case nameof(Response):
                var response = await context
                    .Responses.AsSplitQuery()
                    .Include(lr => lr.Approvals)
                    .Include(response => response.MaterialBatch)
                    .Include(response => response.BatchManufacturingRecord)
                        .ThenInclude(b => b.ProductionScheduleProduct)
                    .FirstOrDefaultAsync(lr => lr.Id == modelId);

                if (response is null)
                    return Error.Validation("Response.NotFound", $"Response {modelId} not found.");

                var currentResponseApprovals = ResponseApprovalRoundManager.Current(
                    response.Approvals
                );
                var responseApprovalStages = currentResponseApprovals
                    .Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var responseCurrentApprovals = GetCurrentApprovalStage(
                    responseApprovalStages,
                    userId,
                    roleIds[0]
                );

                var responseApprovingStage = responseCurrentApprovals.FirstOrDefault();

                if (responseApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                // Approve the leave request stage in the actual tracked list
                var stageToApproveRe = currentResponseApprovals.First(stage =>
                    stage.Status == ApprovalStatus.Pending
                    && stage.Order == responseApprovingStage.Order
                );

                stageToApproveRe.Status = ApprovalStatus.Rejected;
                stageToApproveRe.ApprovalTime = DateTime.UtcNow;
                stageToApproveRe.Comments = comments;
                stageToApproveRe.ApprovedById = userId;
                response.Approved = false;
                response.Rejected = true;
                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Rejected,
                        ModelId = response.Id,
                    }
                );

                if (response.MaterialBatchId.HasValue)
                {
                    var materialAnalyticalRawData = await context
                        .MaterialAnalyticalRawData.AsSplitQuery()
                        .Include(materialAnalyticalRawData =>
                            materialAnalyticalRawData.MaterialStandardTestProcedure
                        )
                        .FirstOrDefaultAsync(m =>
                            m.MaterialStandardTestProcedure.MaterialId
                            == response.MaterialBatch.MaterialId
                        );
                    if (materialAnalyticalRawData is null)
                        return Error.NotFound(
                            "Response.MaterialAnalyticalRawDataNotFound",
                            $"Response {response.MaterialBatchId} not found."
                        );

                    var batch = response.MaterialBatch;
                    if (batch is null)
                        return Error.NotFound(
                            "Response.BatchNotFound",
                            $"Response batch in {response.MaterialBatchId} not found."
                        );
                    batch.Status = BatchStatus.Rejected;
                    context.MaterialBatches.Update(batch);

                    await context.MaterialRejects.AddAsync(
                        new MaterialReject
                        {
                            MaterialBatchId = response.MaterialBatch.Id,
                            ResponseId = response.Id,
                            Reason = comments,
                        }
                    );
                }

                if (response.BatchManufacturingRecordId.HasValue)
                {
                    var productAnalyticalRawData = await context
                        .ProductAnalyticalRawData.AsSplitQuery()
                        .Include(p => p.ProductStandardTestProcedure)
                        .FirstOrDefaultAsync(p =>
                            p.ProductStandardTestProcedure.ProductId
                            == response.BatchManufacturingRecord.ProductionScheduleProduct.ProductId
                        );

                    if (productAnalyticalRawData is null)
                        return Error.NotFound(
                            "Response.ProductAnalyticalRawDataNotFound",
                            $"Response {response.BatchManufacturingRecordId} not found."
                        );

                    var bmr = response.BatchManufacturingRecord;
                    if (bmr is null)
                        return Error.NotFound(
                            "Response.BmrNotFound",
                            $"Response bmr in {response.MaterialBatchId} not found."
                        );
                    bmr.Status = BatchManufacturingStatus.Rejected;
                    context.BatchManufacturingRecords.Update(bmr);
                }

                await context.SaveChangesAsync();
                break;

            case nameof(ProformaInvoice):
                var proformaInvoice = await context
                    .ProformaInvoices.AsSplitQuery()
                    .Include(a => a.Approvals)
                    .FirstOrDefaultAsync(lr => lr.Id == modelId);

                if (proformaInvoice is null)
                    return Error.Validation("Proforma.NotFound", $"Invoice {modelId} not found.");

                var allocationApprovalStages = proformaInvoice
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var allocationCurrentApprovals = GetCurrentApprovalStage(
                    allocationApprovalStages,
                    userId,
                    roleIds[0]
                );

                var allocationApprovingStage = allocationCurrentApprovals.FirstOrDefault();

                if (allocationApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                // Approve the leave request stage in the actual tracked list
                var stageToApproveAl = proformaInvoice.Approvals.First(stage =>
                    (stage.UserId == allocationApprovingStage.UserId && stage.UserId == userId)
                    || (
                        stage.RoleId == allocationApprovingStage.RoleId
                        && allocationApprovingStage.RoleId.HasValue
                        && roleIds.Contains(allocationApprovingStage.RoleId.Value)
                    )
                );

                stageToApproveAl.Status = ApprovalStatus.Rejected;
                stageToApproveAl.ApprovalTime = DateTime.UtcNow;
                stageToApproveAl.Comments = comments;
                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Rejected,
                        ModelId = proformaInvoice.Id,
                    }
                );
                await context.SaveChangesAsync();
                break;

            case nameof(ShipmentDocument):
                var shipmentDocument = await context
                    .ShipmentDocuments.AsSplitQuery()
                    .Include(a => a.Approvals)
                    .FirstOrDefaultAsync(lr => lr.Id == modelId);

                if (shipmentDocument is null)
                    return Error.Validation(
                        "ShipmentDocument.NotFound",
                        $"Shipment document {modelId} not found."
                    );

                var shipmentDocumentApprovalStages = shipmentDocument
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var shipmentDocumentCurrentApprovals = GetCurrentApprovalStage(
                    shipmentDocumentApprovalStages,
                    userId,
                    roleIds[0]
                );

                var shipmentDocumentApprovingStage =
                    shipmentDocumentCurrentApprovals.FirstOrDefault();

                if (shipmentDocumentApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                // Approve the leave request stage in the actual tracked list
                var stageToApproveSd = shipmentDocument.Approvals.First(stage =>
                    (
                        stage.UserId == shipmentDocumentApprovingStage.UserId
                        && stage.UserId == userId
                    )
                    || (
                        stage.RoleId == shipmentDocumentApprovingStage.RoleId
                        && shipmentDocumentApprovingStage.RoleId.HasValue
                        && roleIds.Contains(shipmentDocumentApprovingStage.RoleId.Value)
                    )
                );

                stageToApproveSd.Status = ApprovalStatus.Rejected;
                stageToApproveSd.ApprovalTime = DateTime.UtcNow;
                stageToApproveSd.Comments = comments;
                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Rejected,
                        ModelId = shipmentDocument.Id,
                    }
                );
                await context.SaveChangesAsync();
                break;

            case nameof(JobRequest):
                var jobRequest = await context
                    .JobRequests.AsSplitQuery()
                    .Include(a => a.Approvals)
                    .FirstOrDefaultAsync(lr => lr.Id == modelId);

                if (jobRequest is null)
                    return Error.Validation(
                        "JobRequest.NotFound",
                        $"Job request {modelId} not found."
                    );

                var jobRequestApprovalStages = jobRequest
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var jobRequestCurrentApprovals = GetCurrentApprovalStage(
                    jobRequestApprovalStages,
                    userId,
                    roleIds[0]
                );

                var jobRequestApprovingStage = jobRequestCurrentApprovals.FirstOrDefault();

                if (jobRequestApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                // Approve/Reject the stage in the actual tracked list
                var stageToApproveJr = jobRequest.Approvals.First(stage =>
                    (stage.UserId == jobRequestApprovingStage.UserId && stage.UserId == userId)
                    || (
                        stage.RoleId == jobRequestApprovingStage.RoleId
                        && jobRequestApprovingStage.RoleId.HasValue
                        && roleIds.Contains(jobRequestApprovingStage.RoleId.Value)
                    )
                );

                stageToApproveJr.Status = ApprovalStatus.Rejected;
                stageToApproveJr.ApprovalTime = DateTime.UtcNow;
                stageToApproveJr.Comments = comments;

                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Rejected,
                        ModelId = jobRequest.Id,
                    }
                );

                await context.SaveChangesAsync();
                break;

            case nameof(ProductionExtraPacking):
                var productionExtraPacking = await context
                    .ProductionExtraPackings.AsSplitQuery()
                    .Include(a => a.Approvals)
                    .FirstOrDefaultAsync(lr => lr.Id == modelId);

                if (productionExtraPacking is null)
                    return Error.Validation(
                        "ProductionExtraPacking.NotFound",
                        $"Production extra packing {modelId} not found."
                    );

                var productionExtraPackingApprovalStages = productionExtraPacking
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var productionExtraPackingCurrentApprovals = GetCurrentApprovalStage(
                    productionExtraPackingApprovalStages,
                    userId,
                    roleIds[0]
                );

                var productionExtraPackingApprovingStage =
                    productionExtraPackingCurrentApprovals.FirstOrDefault();

                if (productionExtraPackingApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                // Approve/Reject the stage in the actual tracked list
                var stageToApprovePep = productionExtraPacking.Approvals.First(stage =>
                    (
                        stage.UserId == productionExtraPackingApprovingStage.UserId
                        && stage.UserId == userId
                    )
                    || (
                        stage.RoleId == productionExtraPackingApprovingStage.RoleId
                        && productionExtraPackingApprovingStage.RoleId.HasValue
                        && roleIds.Contains(productionExtraPackingApprovingStage.RoleId.Value)
                    )
                );

                stageToApprovePep.Status = ApprovalStatus.Rejected;
                stageToApprovePep.ApprovalTime = DateTime.UtcNow;
                stageToApprovePep.Comments = comments;

                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Rejected,
                        ModelId = productionExtraPacking.Id,
                    }
                );

                await context.SaveChangesAsync();
                break;

            case nameof(FinishedGoodsTransferNote):
                var finishedGoodsTransferNote = await context
                    .FinishedGoodsTransferNotes.AsSplitQuery()
                    .Include(a => a.Approvals)
                    .FirstOrDefaultAsync(lr => lr.Id == modelId);

                if (finishedGoodsTransferNote is null)
                    return Error.Validation(
                        "FinishedGoodsTransferNote.NotFound",
                        $"Finished goods transfer note {modelId} not found."
                    );

                var fgtnApprovalStages = finishedGoodsTransferNote
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var fgtnCurrentApprovals = GetCurrentApprovalStage(
                    fgtnApprovalStages,
                    userId,
                    roleIds[0]
                );

                var fgtnApprovingStage = fgtnCurrentApprovals.FirstOrDefault();

                if (fgtnApprovingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to approve this resource at this time."
                    );
                }

                // Approve/Reject the stage in the actual tracked list
                var stageToApproveFgtn = finishedGoodsTransferNote.Approvals.First(stage =>
                    (stage.UserId == fgtnApprovingStage.UserId && stage.UserId == userId)
                    || (
                        stage.RoleId == fgtnApprovingStage.RoleId
                        && fgtnApprovingStage.RoleId.HasValue
                        && roleIds.Contains(fgtnApprovingStage.RoleId.Value)
                    )
                );

                stageToApproveFgtn.Status = ApprovalStatus.Rejected;
                stageToApproveFgtn.ApprovalTime = DateTime.UtcNow;
                stageToApproveFgtn.Comments = comments;

                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Rejected,
                        ModelId = finishedGoodsTransferNote.Id,
                    }
                );

                await context.SaveChangesAsync();
                break;

            case nameof(StockAdjustment):
                var stockAdjustment = await context
                    .StockAdjustments.AsSplitQuery()
                    .Include(a => a.Approvals)
                    .FirstOrDefaultAsync(lr => lr.Id == modelId);

                if (stockAdjustment is null)
                    return Error.Validation(
                        "StockAdjustment.NotFound",
                        $"Stock adjustment {modelId} not found."
                    );

                var stockAdjustmentRejectionStages = stockAdjustment
                    .Approvals.Select(item => new ResponsibleApprovalStage
                    {
                        RoleId = item.RoleId,
                        UserId = item.UserId,
                        Order = item.Order,
                        Status = item.Status,
                        Required = item.Required,
                        ApprovalTime = item.ApprovalTime,
                        Comments = item.Comments,
                    })
                    .ToList();

                var stockAdjustmentCurrentRejections = GetCurrentApprovalStage(
                    stockAdjustmentRejectionStages,
                    userId,
                    roleIds[0]
                );

                var stockAdjustmentRejectingStage =
                    stockAdjustmentCurrentRejections.FirstOrDefault();

                if (stockAdjustmentRejectingStage == null)
                {
                    return Error.Validation(
                        "Approval.Unauthorized",
                        "You are not authorized to reject this resource at this time."
                    );
                }

                // Reject the stage
                var stageToRejectSa = stockAdjustment.Approvals.First(stage =>
                    stage.Status != ApprovalStatus.Approved
                    && stage.Order == stockAdjustmentRejectingStage.Order
                );

                stageToRejectSa.Status = ApprovalStatus.Rejected;
                stageToRejectSa.Comments = comments;
                stageToRejectSa.ApprovedById = userId;

                await context.SaveChangesAsync();

                await AddApprovalLogs(
                    new CreateApprovalLog
                    {
                        UserId = userId,
                        Comments = comments,
                        Status = ApprovalStatus.Rejected,
                        ModelId = stockAdjustment.Id,
                    }
                );
                break;

            default:
                return Error.Validation(
                    "Approval.InvalidType",
                    $"Unsupported model type: {modelType}"
                );
        }

        return Result.Success();
    }

    public async Task<List<ApprovalEntity>> GetEntitiesRequiringApproval(
        Guid userId,
        List<Guid> roleIds,
        string modelType
    )
    {
        var entitiesRequiringApproval = new List<ApprovalEntity>();

        // 1. Get Purchase Orders requiring approval
        var purchaseOrders = await context
            .PurchaseOrders.AsSplitQuery()
            .Include(po => po.Approvals)
                .ThenInclude(responsibleApprovalStage => responsibleApprovalStage.ApprovedBy)
            .Include(po => po.CreatedBy)
                .ThenInclude(po => po.Department)
            .Where(po =>
                po.Approvals.Any(a =>
                    (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                    && a.Status != ApprovalStatus.Approved
                )
            )
            .ToListAsync();

        foreach (var po in purchaseOrders)
        {
            entitiesRequiringApproval.Add(
                new ApprovalEntity
                {
                    ModelType = nameof(PurchaseOrder),
                    Id = po.Id,
                    CreatedAt = po.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(po.CreatedBy?.Department),
                    Code = po.Code,
                    RequestedBy = mapper.Map<UserDto>(po.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(po.Id),
                }
            );
        }

        // 2. Get Requisitions requiring approval
        var requisitions = await context
            .Requisitions.AsSplitQuery()
            .Include(p => p.Department)
            .Include(p => p.CreatedBy)
            .Include(po => po.Approvals)
                .ThenInclude(responsibleApprovalStage => responsibleApprovalStage.ApprovedBy)
            .Include(po => po.CreatedBy)
                .ThenInclude(po => po.Department)
            .Where(po =>
                po.Approvals.Any(a =>
                    (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                    && a.Status != ApprovalStatus.Approved
                )
            )
            .ToListAsync();

        foreach (var r in requisitions)
        {
            if (r.RequisitionType == RequisitionType.Stock)
            {
                entitiesRequiringApproval.Add(
                    new ApprovalEntity
                    {
                        ModelType = "StockRequisition",
                        Id = r.Id,
                        CreatedAt = r.CreatedAt,
                        Department = mapper.Map<DepartmentDto>(r.CreatedBy?.Department),
                        Code = r.Code,
                        RequestedBy = mapper.Map<UserDto>(r.CreatedBy),
                        ApprovalLogs = GetApprovalLogs(r.Id),
                    }
                );
            }
            else if (r.RequisitionType == RequisitionType.Trial)
            {
                entitiesRequiringApproval.Add(
                    new ApprovalEntity
                    {
                        ModelType = "TrialRequisition",
                        Id = r.Id,
                        CreatedAt = r.CreatedAt,
                        Department = mapper.Map<DepartmentDto>(r.CreatedBy?.Department),
                        Code = r.Code,
                        RequestedBy = mapper.Map<UserDto>(r.CreatedBy),
                        ApprovalLogs = GetApprovalLogs(r.Id),
                    }
                );
            }
            else
            {
                entitiesRequiringApproval.Add(
                    new ApprovalEntity
                    {
                        ModelType = "PurchaseRequisition",
                        Id = r.Id,
                        CreatedAt = r.CreatedAt,
                        Department = mapper.Map<DepartmentDto>(r.CreatedBy?.Department),
                        Code = r.Code,
                        RequestedBy = mapper.Map<UserDto>(r.CreatedBy),
                    }
                );
            }
        }

        // 3. Get Billing Sheets requiring approval
        var billingSheets = await context
            .BillingSheets.AsSplitQuery()
            .Include(bs => bs.Approvals)
            .Include(po => po.CreatedBy)
                .ThenInclude(po => po.Department)
            .Where(bs =>
                bs.Approvals.Any(a =>
                    (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                    && a.Status != ApprovalStatus.Approved
                )
            )
            .ToListAsync();

        foreach (var bs in billingSheets)
        {
            entitiesRequiringApproval.Add(
                new ApprovalEntity
                {
                    ModelType = nameof(BillingSheet),
                    Id = bs.Id,
                    Code = bs.Code,
                    Department = mapper.Map<DepartmentDto>(bs.CreatedBy?.Department),
                    CreatedAt = bs.CreatedAt,
                    RequestedBy = mapper.Map<UserDto>(bs.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(bs.Id),
                }
            );
        }

        var overtimeRequests = await context
            .OvertimeRequests.AsSplitQuery()
            .Include(bs => bs.Approvals)
            .Include(po => po.CreatedBy)
                .ThenInclude(po => po.Department)
            .Where(bs =>
                bs.Approvals.Any(a =>
                    (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                    && a.Status != ApprovalStatus.Approved
                )
            )
            .ToListAsync();

        foreach (var bs in overtimeRequests)
        {
            entitiesRequiringApproval.Add(
                new ApprovalEntity
                {
                    ModelType = nameof(OvertimeRequest),
                    Id = bs.Id,
                    Code = "",
                    Department = mapper.Map<DepartmentDto>(bs.CreatedBy?.Department),
                    CreatedAt = bs.CreatedAt,
                    RequestedBy = mapper.Map<UserDto>(bs.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(bs.Id),
                }
            );
        }

        var leaveRequests = await context
            .LeaveRequests.AsSplitQuery()
            .Include(bs => bs.Approvals)
            .Include(po => po.CreatedBy)
                .ThenInclude(po => po.Department)
            .Include(a => a.Employee)
                .ThenInclude(a => a.Department)
            .Where(bs =>
                bs.Approvals.Any(a =>
                    (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                    && a.Status != ApprovalStatus.Approved
                )
            )
            .ToListAsync();

        foreach (var bs in leaveRequests)
        {
            entitiesRequiringApproval.Add(
                new ApprovalEntity
                {
                    ModelType = nameof(LeaveRequest),
                    Id = bs.Id,
                    Code = "",
                    Department = mapper.Map<DepartmentDto>(bs.CreatedBy?.Department),
                    CreatedAt = bs.CreatedAt,
                    RequestedBy = mapper.Map<UserDto>(bs.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(bs.Id),
                }
            );
        }

        var payrollRuns = await context
            .PayrollRuns.AsSplitQuery()
            .Include(bs => bs.Approvals)
            .Include(po => po.CreatedBy)
                .ThenInclude(po => po.Department)
            .Where(bs =>
                bs.Approvals.Any(a =>
                    (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                    && a.Status != ApprovalStatus.Approved
                )
            )
            .ToListAsync();

        foreach (var bs in payrollRuns)
        {
            entitiesRequiringApproval.Add(
                new ApprovalEntity
                {
                    ModelType = nameof(PayrollRun),
                    Id = bs.Id,
                    Code = "",
                    Department = mapper.Map<DepartmentDto>(bs.CreatedBy?.Department),
                    CreatedAt = bs.CreatedAt,
                    RequestedBy = mapper.Map<UserDto>(bs.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(bs.Id),
                }
            );
        }

        var performanceReviews = await context
            .PerformanceReviews.AsSplitQuery()
            .Include(bs => bs.Approvals)
            .Include(po => po.CreatedBy)
                .ThenInclude(po => po.Department)
            .Include(a => a.Employee)
                .ThenInclude(a => a.Department)
            .Where(bs =>
                bs.Approvals.Any(a =>
                    (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                    && a.Status != ApprovalStatus.Approved
                )
            )
            .ToListAsync();

        foreach (var bs in performanceReviews)
        {
            entitiesRequiringApproval.Add(
                new ApprovalEntity
                {
                    ModelType = nameof(PerformanceReview),
                    Id = bs.Id,
                    Code = "",
                    Department = mapper.Map<DepartmentDto>(bs.Employee?.Department),
                    CreatedAt = bs.CreatedAt,
                    RequestedBy = mapper.Map<UserDto>(bs.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(bs.Id),
                }
            );
        }

        var responses = await context
            .Responses.AsSplitQuery()
            .Include(bs => bs.Approvals)
            .Include(po => po.CreatedBy)
                .ThenInclude(po => po.Department)
            .Where(bs =>
                bs.Approvals.Any(a =>
                    a.ApprovalRound == bs.Approvals.Max(item => item.ApprovalRound)
                    &&
                    (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                    && a.Status == ApprovalStatus.Pending
                    && a.ActivatedAt.HasValue
                )
            )
            .ToListAsync();

        foreach (var bs in responses)
        {
            entitiesRequiringApproval.Add(
                new ApprovalEntity
                {
                    ModelType = nameof(Response),
                    Id = bs.Id,
                    Code = "",
                    Department = mapper.Map<DepartmentDto>(bs.CreatedBy?.Department),
                    CreatedAt = bs.CreatedAt,
                    RequestedBy = mapper.Map<UserDto>(bs.CreatedBy),
                    MaterialBatchId = bs.MaterialBatchId,
                    BatchManufacturingRecordId = bs.BatchManufacturingRecordId,
                    ProductionActivityStepId = bs.ProductionActivityStepId,
                    ApprovalLogs = GetApprovalLogs(bs.Id),
                }
            );
        }

        var proformaInvoices = await context
            .ProformaInvoices.AsSplitQuery()
            .Include(a => a.Approvals)
            .Include(a => a.CreatedBy)
                .ThenInclude(a => a.Department)
            .Where(bs =>
                bs.Approvals.Any(a =>
                    (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                    && a.Status != ApprovalStatus.Approved
                )
            )
            .ToListAsync();

        foreach (var proformaInvoice in proformaInvoices)
        {
            entitiesRequiringApproval.Add(
                new ApprovalEntity
                {
                    ModelType = nameof(ProformaInvoice),
                    Id = proformaInvoice.Id,
                    Code = proformaInvoice.Code,
                    Department = mapper.Map<DepartmentDto>(proformaInvoice.CreatedBy?.Department),
                    CreatedAt = proformaInvoice.CreatedAt,
                    RequestedBy = mapper.Map<UserDto>(proformaInvoice.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(proformaInvoice.Id),
                }
            );
        }

        var shipmentDocuments = await context
            .ShipmentDocuments.AsSplitQuery()
            .Include(a => a.Approvals)
            .Include(a => a.CreatedBy)
                .ThenInclude(a => a.Department)
            .Where(bs =>
                bs.Approvals.Any(a =>
                    (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                    && a.Status != ApprovalStatus.Approved
                )
            )
            .ToListAsync();

        foreach (var shipmentDocument in shipmentDocuments)
        {
            entitiesRequiringApproval.Add(
                new ApprovalEntity
                {
                    ModelType = nameof(ShipmentDocument),
                    Id = shipmentDocument.Id,
                    Code = shipmentDocument.Code,
                    Department = mapper.Map<DepartmentDto>(shipmentDocument.CreatedBy?.Department),
                    CreatedAt = shipmentDocument.CreatedAt,
                    RequestedBy = mapper.Map<UserDto>(shipmentDocument.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(shipmentDocument.Id),
                }
            );
        }

        var jobRequests = await context
            .JobRequests.AsSplitQuery()
            .Include(a => a.Approvals)
            .Include(a => a.CreatedBy)
                .ThenInclude(a => a.Department)
            .Where(bs =>
                bs.Approvals.Any(a =>
                    (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                    && a.Status != ApprovalStatus.Approved
                )
            )
            .ToListAsync();

        foreach (var jobRequest in jobRequests)
        {
            entitiesRequiringApproval.Add(
                new ApprovalEntity
                {
                    ModelType = nameof(JobRequest),
                    Id = jobRequest.Id,
                    Code = jobRequest.Code,
                    Department = mapper.Map<DepartmentDto>(jobRequest.CreatedBy?.Department),
                    CreatedAt = jobRequest.CreatedAt,
                    RequestedBy = mapper.Map<UserDto>(jobRequest.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(jobRequest.Id),
                }
            );
        }

        var productExtraPackings = await context
            .ProductionExtraPackings.AsSplitQuery()
            .Include(a => a.Approvals)
            .Include(a => a.CreatedBy)
                .ThenInclude(a => a.Department)
            .Include(productionExtraPacking => productionExtraPacking.ProductionScheduleProduct)
                .ThenInclude(productionScheduleProduct =>
                    productionScheduleProduct.ProductionSchedule
                )
            .Where(bs =>
                bs.Approvals.Any(a =>
                    (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                    && a.Status != ApprovalStatus.Approved
                )
            )
            .ToListAsync();

        foreach (var productExtraPacking in productExtraPackings)
        {
            entitiesRequiringApproval.Add(
                new ApprovalEntity
                {
                    ModelType = nameof(ProductionExtraPacking),
                    Id = productExtraPacking.Id,
                    Code = productExtraPacking.ProductionScheduleProduct.ProductionSchedule.Code,
                    Department = mapper.Map<DepartmentDto>(
                        productExtraPacking.CreatedBy?.Department
                    ),
                    CreatedAt = productExtraPacking.CreatedAt,
                    RequestedBy = mapper.Map<UserDto>(productExtraPacking.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(productExtraPacking.Id),
                }
            );
        }

        var fgtns = await context
            .FinishedGoodsTransferNotes.AsSplitQuery()
            .Include(a => a.Approvals)
            .Include(a => a.CreatedBy)
                .ThenInclude(a => a.Department)
            .Where(bs =>
                bs.Approvals.Any(a =>
                    (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                    && a.Status != ApprovalStatus.Approved
                )
            )
            .ToListAsync();

        foreach (var fgtn in fgtns)
        {
            entitiesRequiringApproval.Add(
                new ApprovalEntity
                {
                    ModelType = nameof(FinishedGoodsTransferNote),
                    Id = fgtn.Id,
                    Code = fgtn.TransferNoteNumber,
                    Department = mapper.Map<DepartmentDto>(fgtn.CreatedBy?.Department),
                    CreatedAt = fgtn.CreatedAt,
                    RequestedBy = mapper.Map<UserDto>(fgtn.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(fgtn.Id),
                }
            );
        }

        var stockAdjustments = await context
            .StockAdjustments.AsSplitQuery()
            .Include(a => a.Approvals)
            .Include(a => a.CreatedBy)
                .ThenInclude(a => a.Department)
            .Where(bs =>
                bs.Approvals.Any(a =>
                    (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                    && a.Status != ApprovalStatus.Approved
                )
            )
            .ToListAsync();

        foreach (var sa in stockAdjustments)
        {
            entitiesRequiringApproval.Add(
                new ApprovalEntity
                {
                    ModelType = nameof(StockAdjustment),
                    Id = sa.Id,
                    Code = sa.AdjustmentNumber,
                    Department = mapper.Map<DepartmentDto>(sa.CreatedBy?.Department),
                    CreatedAt = sa.CreatedAt,
                    RequestedBy = mapper.Map<UserDto>(sa.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(sa.Id),
                }
            );
        }

        var allocations = await context.AllocateProductionOrders
            .AsSplitQuery()
            .Include(item => item.Approvals)
            .Include(item => item.CreatedBy)
                .ThenInclude(user => user.Department)
            .Include(item => item.ProductionOrder)
            .Where(item => item.Approvals.Any(stage =>
                (stage.UserId == userId
                    || stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value))
                && stage.Status != ApprovalStatus.Approved))
            .ToListAsync();

        foreach (var allocation in allocations)
        {
            entitiesRequiringApproval.Add(new ApprovalEntity
            {
                ModelType = nameof(AllocateProductionOrder),
                Id = allocation.Id,
                Code = allocation.ProductionOrder?.Code ?? string.Empty,
                Department = mapper.Map<DepartmentDto>(allocation.CreatedBy?.Department),
                CreatedAt = allocation.CreatedAt,
                RequestedBy = mapper.Map<UserDto>(allocation.CreatedBy),
                ApprovalLogs = GetApprovalLogs(allocation.Id),
            });
        }

        var customerQuotations = await context.CustomerQuotations
            .AsSplitQuery()
            .Include(item => item.Approvals)
            .Include(item => item.CreatedBy)
                .ThenInclude(user => user.Department)
            .Where(item => item.Status == CustomerQuotationStatus.Sent
                && item.Approvals.Any(stage =>
                    stage.ActivatedAt.HasValue
                    && stage.Status == ApprovalStatus.Pending
                    && (stage.UserId == userId
                        || stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value))))
            .ToListAsync();

        foreach (var quotation in customerQuotations)
        {
            entitiesRequiringApproval.Add(new ApprovalEntity
            {
                ModelType = nameof(CustomerQuotation),
                Id = quotation.Id,
                Code = quotation.Code,
                Department = mapper.Map<DepartmentDto>(quotation.CreatedBy?.Department),
                CreatedAt = quotation.CreatedAt,
                RequestedBy = mapper.Map<UserDto>(quotation.CreatedBy),
                ApprovalLogs = GetApprovalLogs(quotation.Id),
            });
        }

        var payments = await PaymentApprovalQueue.GetAsync(context, userId, roleIds);

        foreach (var payment in payments)
        {
            entitiesRequiringApproval.Add(new ApprovalEntity
            {
                ModelType = nameof(Payment),
                Id = payment.Id,
                Code = payment.Reference,
                Department = mapper.Map<DepartmentDto>(payment.RecordedBy?.Department),
                CreatedAt = payment.CreatedAt,
                RequestedBy = mapper.Map<UserDto>(payment.RecordedBy),
                ApprovalLogs = GetApprovalLogs(payment.Id),
            });
        }

        var rndProjects = await context
            .RndProjects.AsSplitQuery()
            .Include(item => item.Approvals)
                .ThenInclude(stage => stage.ApprovedBy)
            .Include(item => item.RequestedBy)
                .ThenInclude(user => user.Department)
            .Include(item => item.Department)
            .Where(item =>
                item.Approvals.Any(a =>
                    (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                    && a.Status != ApprovalStatus.Approved
                )
            )
            .ToListAsync();

        foreach (var rndProject in rndProjects)
        {
            entitiesRequiringApproval.Add(new ApprovalEntity
            {
                ModelType = nameof(RndProject),
                Id = rndProject.Id,
                CreatedAt = rndProject.CreatedAt,
                Department = mapper.Map<DepartmentDto>(rndProject.Department),
                Code = rndProject.Code,
                RequestedBy = mapper.Map<UserDto>(rndProject.RequestedBy),
                ApprovalLogs = GetApprovalLogs(rndProject.Id),
            });
        }

        var rndFormulations = await context.RndFormulations.AsSplitQuery()
            .Include(item => item.Approvals).ThenInclude(stage => stage.ApprovedBy)
            .Include(item => item.RndProject).ThenInclude(project => project.Department)
            .Include(item => item.RndProject).ThenInclude(project => project.RequestedBy)
            .Where(item => item.Status == RndFormulationStatus.InReview && item.Approvals.Any(stage =>
                stage.ActivatedAt.HasValue && stage.Status == ApprovalStatus.Pending
                && (stage.UserId == userId || stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value))))
            .ToListAsync();
        entitiesRequiringApproval.AddRange(rndFormulations.Select(item => new ApprovalEntity
        {
            ModelType = nameof(RndFormulation), Id = item.Id,
            Code = $"{item.RndProject.Code} · V{item.Version}", CreatedAt = item.CreatedAt,
            Department = mapper.Map<DepartmentDto>(item.RndProject.Department),
            RequestedBy = mapper.Map<UserDto>(item.RndProject.RequestedBy),
            ApprovalLogs = GetApprovalLogs(item.Id),
        }));

        var rndTransfers = await context.RndTechnologyTransfers.AsSplitQuery()
            .Include(item => item.Approvals).ThenInclude(stage => stage.ApprovedBy)
            .Include(item => item.RndProject).ThenInclude(project => project.Department)
            .Include(item => item.RndProject).ThenInclude(project => project.RequestedBy)
            .Where(item => item.Status == RndTechnologyTransferStatus.ProtocolInReview
                && item.Approvals.Any(stage => stage.ActivatedAt.HasValue
                    && stage.Status == ApprovalStatus.Pending
                    && (stage.UserId == userId || stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value))))
            .ToListAsync();
        entitiesRequiringApproval.AddRange(rndTransfers.Select(item => new ApprovalEntity
        {
            ModelType = nameof(RndTechnologyTransfer), Id = item.Id,
            Code = $"{item.RndProject.Code} · Transfer", CreatedAt = item.CreatedAt,
            Department = mapper.Map<DepartmentDto>(item.RndProject.Department),
            RequestedBy = mapper.Map<UserDto>(item.RndProject.RequestedBy),
            ApprovalLogs = GetApprovalLogs(item.Id),
        }));

        if (!string.IsNullOrEmpty(modelType))
        {
            entitiesRequiringApproval = entitiesRequiringApproval
                .Where(a => a.ModelType == modelType)
                .ToList();
        }

        return entitiesRequiringApproval.OrderByDescending(a => a.CreatedAt).ToList();
    }

    public async Task<List<UserDto>> GetUsersWithPendingApprovals()
    {
        var userIdsFromStages = new HashSet<Guid>();
        var roleIdsFromStages = new HashSet<Guid>();

        // Helper to collect IDs from approval tables
        async Task Collect(IQueryable<ResponsibleApprovalStage> query)
        {
            var results = await query
                .Where(a => a.Status == ApprovalStatus.Pending)
                .Select(a => new { a.UserId, a.RoleId })
                .ToListAsync();

            foreach (var r in results)
            {
                if (r.UserId.HasValue)
                    userIdsFromStages.Add(r.UserId.Value);
                if (r.RoleId.HasValue)
                    roleIdsFromStages.Add(r.RoleId.Value);
            }
        }

        await Collect(context.RequisitionApprovals);
        await Collect(context.PurchaseOrderApprovals);
        await Collect(context.BillingSheetApprovals);
        await Collect(context.ShipmentDocumentApprovals);
        await Collect(context.StaffRequisitionApprovals);
        await Collect(context.LeaveRequestApprovals);
        await Collect(context.OvertimeRequestApprovals);
        await Collect(context.JobRequestApprovals);
        await Collect(context.StockAdjustmentApprovals);
        await Collect(context.ServiceMemoApprovals);
        await Collect(context.ProformaInvoiceApprovals);
        await Collect(context.ProductionOrderApprovals);
        await Collect(context.CustomerQuotationApprovals.Where(stage => stage.ActivatedAt.HasValue));
        await Collect(context.AllocateProductionOrderApprovals);
        await Collect(context.RndProjectApprovals);
        await Collect(context.RndFormulationApprovals.Where(stage => stage.ActivatedAt.HasValue));
        await Collect(context.RndTechnologyTransferApprovals.Where(stage => stage.ActivatedAt.HasValue));
        await Collect(context.FinishedGoodsTransferNoteApprovals);
        await Collect(context.ProductionExtraPackingApprovals);
        await Collect(context.PaymentApprovals.Where(stage => stage.ActivatedAt.HasValue));
        await Collect(
            context.ResponseApprovals.Where(a =>
                a.ActivatedAt.HasValue
                && a.ApprovalRound
                    == context.ResponseApprovals
                        .Where(item => item.ResponseId == a.ResponseId)
                        .Max(item => item.ApprovalRound)
            )
        );

        var finalUserIds = new HashSet<Guid>(userIdsFromStages);

        if (roleIdsFromStages.Count != 0)
        {
            var usersInRoles = await context
                .UserRoles.Where(ur => roleIdsFromStages.Contains(ur.RoleId))
                .Select(ur => ur.UserId)
                .ToListAsync();

            foreach (var uid in usersInRoles)
                finalUserIds.Add(uid);
        }

        var users = await context
            .Users.Include(u => u.Department)
            .Where(u => finalUserIds.Contains(u.Id))
            .ToListAsync();

        return mapper.Map<List<UserDto>>(users);
    }

    public async Task<Dictionary<string, int>> GetStatisticsOfEntitiesRequiringApproval(
        Guid userId,
        List<Guid> roleIds
    )
    {
        var entities = await GetEntitiesRequiringApproval(userId, roleIds, null);

        return entities.GroupBy(e => e.ModelType).ToDictionary(e => e.Key, e => e.Count());
    }

    public async Task<Result<ApprovalEntity>> GetEntityRequiringApproval(
        string modelType,
        Guid modelId
    )
    {
        switch (modelType)
        {
            case "PurchaseRequisition" or "StockRequisition" or "TrialRequisition":
                var requisition = await context
                    .Requisitions.AsSplitQuery()
                    .Include(r => r.CreatedBy)
                        .ThenInclude(r => r.Department)
                    .Include(r => r.Approvals)
                        .ThenInclude(a => a.ApprovedBy)
                    .FirstOrDefaultAsync(r => r.Id == modelId);
                return new ApprovalEntity
                {
                    ModelType = modelType,
                    Id = modelId,
                    Code = requisition.Code,
                    CreatedAt = requisition.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(requisition.CreatedBy?.Department),
                    RequestedBy = mapper.Map<UserDto>(requisition.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(modelId),
                };

            case nameof(BillingSheet):
                var billingSheet = await context
                    .BillingSheets.AsSplitQuery()
                    .Include(b => b.CreatedBy)
                        .ThenInclude(u => u.Department)
                    .Include(b => b.Approvals)
                        .ThenInclude(a => a.ApprovedBy)
                    .FirstOrDefaultAsync(b => b.Id == modelId);
                return new ApprovalEntity
                {
                    ModelType = modelType,
                    Id = modelId,
                    Code = billingSheet.Code,
                    CreatedAt = billingSheet.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(billingSheet.CreatedBy?.Department),
                    RequestedBy = mapper.Map<UserDto>(billingSheet.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(modelId),
                };

            case nameof(PurchaseOrder):
                var purchaseOrder = await context
                    .PurchaseOrders.AsSplitQuery()
                    .Include(p => p.CreatedBy)
                        .ThenInclude(u => u.Department)
                    .Include(p => p.Approvals)
                        .ThenInclude(a => a.ApprovedBy)
                    .FirstOrDefaultAsync(p => p.Id == modelId);
                return new ApprovalEntity
                {
                    ModelType = modelType,
                    Id = modelId,
                    Code = purchaseOrder.Code,
                    CreatedAt = purchaseOrder.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(purchaseOrder.CreatedBy?.Department),
                    RequestedBy = mapper.Map<UserDto>(purchaseOrder.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(modelId),
                };

            case nameof(LeaveRequest):
                var leaveRequest = await context
                    .LeaveRequests.AsSplitQuery()
                    .Include(l => l.CreatedBy)
                        .ThenInclude(u => u.Department)
                    .Include(l => l.Approvals)
                        .ThenInclude(a => a.ApprovedBy)
                    .FirstOrDefaultAsync(l => l.Id == modelId);
                return new ApprovalEntity
                {
                    ModelType = modelType,
                    Id = modelId,
                    Code = "",
                    CreatedAt = leaveRequest.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(leaveRequest.CreatedBy?.Department),
                    RequestedBy = mapper.Map<UserDto>(leaveRequest.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(modelId),
                };

            case nameof(OvertimeRequest):
                var overtimeRequest = await context
                    .OvertimeRequests.AsSplitQuery()
                    .Include(l => l.CreatedBy)
                        .ThenInclude(u => u.Department)
                    .Include(l => l.Approvals)
                        .ThenInclude(a => a.ApprovedBy)
                    .FirstOrDefaultAsync(l => l.Id == modelId);
                return new ApprovalEntity
                {
                    ModelType = modelType,
                    Id = modelId,
                    Code = overtimeRequest.Code,
                    CreatedAt = overtimeRequest.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(overtimeRequest.CreatedBy?.Department),
                    RequestedBy = mapper.Map<UserDto>(overtimeRequest.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(modelId),
                };

            case nameof(PayrollRun):
                var payrollRun = await context
                    .PayrollRuns.AsSplitQuery()
                    .Include(l => l.CreatedBy)
                        .ThenInclude(u => u.Department)
                    .Include(l => l.Approvals)
                        .ThenInclude(a => a.ApprovedBy)
                    .FirstOrDefaultAsync(l => l.Id == modelId);
                return new ApprovalEntity
                {
                    ModelType = modelType,
                    Id = modelId,
                    Code = "",
                    CreatedAt = payrollRun.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(payrollRun.CreatedBy?.Department),
                    RequestedBy = mapper.Map<UserDto>(payrollRun.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(modelId),
                };

            case nameof(PerformanceReview):
                var performanceReview = await context
                    .PerformanceReviews.AsSplitQuery()
                    .Include(l => l.CreatedBy)
                        .ThenInclude(u => u.Department)
                    .Include(l => l.Employee)
                        .ThenInclude(e => e.Department)
                    .Include(l => l.Approvals)
                        .ThenInclude(a => a.ApprovedBy)
                    .FirstOrDefaultAsync(l => l.Id == modelId);
                return new ApprovalEntity
                {
                    ModelType = modelType,
                    Id = modelId,
                    Code = "",
                    CreatedAt = performanceReview.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(performanceReview.Employee?.Department),
                    RequestedBy = mapper.Map<UserDto>(performanceReview.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(modelId),
                };

            case nameof(Response):
                var response = await context
                    .Responses.AsSplitQuery()
                    .Include(l => l.CreatedBy)
                        .ThenInclude(u => u.Department)
                    .Include(l => l.Approvals)
                        .ThenInclude(a => a.ApprovedBy)
                    .FirstOrDefaultAsync(l => l.Id == modelId);
                return new ApprovalEntity
                {
                    ModelType = modelType,
                    Id = modelId,
                    Code = "",
                    CreatedAt = response.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(response.CreatedBy?.Department),
                    RequestedBy = mapper.Map<UserDto>(response.CreatedBy),
                    MaterialBatchId = response.MaterialBatchId,
                    BatchManufacturingRecordId = response.BatchManufacturingRecordId,
                    ProductionActivityStepId = response.ProductionActivityStepId,
                    ApprovalLogs = GetApprovalLogs(modelId),
                };

            case nameof(ProformaInvoice):
                var proformaInvoice = await context
                    .ProformaInvoices.AsSplitQuery()
                    .Include(l => l.CreatedBy)
                        .ThenInclude(u => u.Department)
                    .Include(l => l.Approvals)
                        .ThenInclude(a => a.ApprovedBy)
                    .FirstOrDefaultAsync(l => l.Id == modelId);
                return new ApprovalEntity
                {
                    ModelType = modelType,
                    Id = modelId,
                    Code = proformaInvoice.Code,
                    CreatedAt = proformaInvoice.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(proformaInvoice.CreatedBy?.Department),
                    RequestedBy = mapper.Map<UserDto>(proformaInvoice.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(modelId),
                };

            case nameof(ShipmentDocument):
                var shipmentDoc = await context
                    .ShipmentDocuments.AsSplitQuery()
                    .Include(l => l.CreatedBy)
                        .ThenInclude(u => u.Department)
                    .Include(l => l.Approvals)
                        .ThenInclude(a => a.ApprovedBy)
                    .FirstOrDefaultAsync(l => l.Id == modelId);
                return new ApprovalEntity
                {
                    ModelType = modelType,
                    Id = modelId,
                    Code = shipmentDoc.Code,
                    CreatedAt = shipmentDoc.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(shipmentDoc.CreatedBy?.Department),
                    RequestedBy = mapper.Map<UserDto>(shipmentDoc.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(modelId),
                };

            case nameof(JobRequest):
                var jobRequest = await context
                    .JobRequests.AsSplitQuery()
                    .Include(l => l.CreatedBy)
                        .ThenInclude(u => u.Department)
                    .Include(l => l.Approvals)
                        .ThenInclude(a => a.ApprovedBy)
                    .FirstOrDefaultAsync(l => l.Id == modelId);
                return new ApprovalEntity
                {
                    ModelType = modelType,
                    Id = modelId,
                    Code = jobRequest.Code,
                    CreatedAt = jobRequest.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(jobRequest.CreatedBy?.Department),
                    RequestedBy = mapper.Map<UserDto>(jobRequest.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(modelId),
                };

            case nameof(ProductionExtraPacking):
                var productionExtraPacking = await context
                    .ProductionExtraPackings.AsSplitQuery()
                    .Include(l => l.CreatedBy)
                        .ThenInclude(u => u.Department)
                    .Include(l => l.Approvals)
                        .ThenInclude(a => a.ApprovedBy)
                    .Include(productionExtraPacking =>
                        productionExtraPacking.ProductionScheduleProduct
                    )
                        .ThenInclude(productionScheduleProduct =>
                            productionScheduleProduct.ProductionSchedule
                        )
                    .FirstOrDefaultAsync(l => l.Id == modelId);
                return new ApprovalEntity
                {
                    ModelType = modelType,
                    Id = modelId,
                    Code = productionExtraPacking.ProductionScheduleProduct.ProductionSchedule.Code,
                    CreatedAt = productionExtraPacking.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(
                        productionExtraPacking.CreatedBy?.Department
                    ),
                    RequestedBy = mapper.Map<UserDto>(productionExtraPacking.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(modelId),
                };

            case nameof(FinishedGoodsTransferNote):
                var finishedGoodsTransferNote = await context
                    .FinishedGoodsTransferNotes.AsSplitQuery()
                    .Include(l => l.CreatedBy)
                        .ThenInclude(u => u.Department)
                    .Include(l => l.Approvals)
                        .ThenInclude(a => a.ApprovedBy)
                    .FirstOrDefaultAsync(l => l.Id == modelId);
                return new ApprovalEntity
                {
                    ModelType = modelType,
                    Id = modelId,
                    Code = finishedGoodsTransferNote.TransferNoteNumber,
                    CreatedAt = finishedGoodsTransferNote.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(
                        finishedGoodsTransferNote.CreatedBy?.Department
                    ),
                    RequestedBy = mapper.Map<UserDto>(finishedGoodsTransferNote.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(modelId),
                };

            case nameof(StockAdjustment):
                var stockAdjustment = await context
                    .StockAdjustments.AsSplitQuery()
                    .Include(l => l.CreatedBy)
                        .ThenInclude(u => u.Department)
                    .Include(l => l.Approvals)
                        .ThenInclude(a => a.ApprovedBy)
                    .FirstOrDefaultAsync(l => l.Id == modelId);
                return new ApprovalEntity
                {
                    ModelType = modelType,
                    Id = modelId,
                    Code = stockAdjustment.AdjustmentNumber,
                    CreatedAt = stockAdjustment.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(stockAdjustment.CreatedBy?.Department),
                    RequestedBy = mapper.Map<UserDto>(stockAdjustment.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(modelId),
                };

            case nameof(AllocateProductionOrder):
                var allocation = await context.AllocateProductionOrders
                    .AsSplitQuery()
                    .Include(item => item.CreatedBy)
                        .ThenInclude(user => user.Department)
                    .Include(item => item.Approvals)
                        .ThenInclude(stage => stage.ApprovedBy)
                    .Include(item => item.ProductionOrder)
                    .FirstOrDefaultAsync(item => item.Id == modelId);
                return new ApprovalEntity
                {
                    ModelType = modelType,
                    Id = modelId,
                    Code = allocation.ProductionOrder?.Code ?? string.Empty,
                    CreatedAt = allocation.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(allocation.CreatedBy?.Department),
                    RequestedBy = mapper.Map<UserDto>(allocation.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(modelId),
                };

            case nameof(RndProject):
                var rndProject = await context.RndProjects
                    .AsSplitQuery()
                    .Include(item => item.Department)
                    .Include(item => item.RequestedBy)
                        .ThenInclude(user => user.Department)
                    .Include(item => item.Approvals)
                        .ThenInclude(stage => stage.ApprovedBy)
                    .FirstOrDefaultAsync(item => item.Id == modelId);
                if (rndProject is null)
                    return Error.NotFound("RndProject.NotFound", "R&D project not found.");
                return new ApprovalEntity
                {
                    ModelType = modelType,
                    Id = modelId,
                    Code = rndProject.Code,
                    CreatedAt = rndProject.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(rndProject.Department),
                    RequestedBy = mapper.Map<UserDto>(rndProject.RequestedBy),
                    ApprovalLogs = GetApprovalLogs(modelId),
                };

            case nameof(RndFormulation):
                var rndFormulation = await context.RndFormulations
                    .Include(item => item.RndProject).ThenInclude(project => project.Department)
                    .Include(item => item.RndProject).ThenInclude(project => project.RequestedBy)
                    .FirstOrDefaultAsync(item => item.Id == modelId);
                if (rndFormulation is null)
                    return Error.NotFound("RndFormulation.NotFound", "Formulation not found.");
                return new ApprovalEntity
                {
                    ModelType = modelType, Id = modelId,
                    Code = $"{rndFormulation.RndProject.Code} · V{rndFormulation.Version}",
                    CreatedAt = rndFormulation.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(rndFormulation.RndProject.Department),
                    RequestedBy = mapper.Map<UserDto>(rndFormulation.RndProject.RequestedBy),
                    ApprovalLogs = GetApprovalLogs(modelId),
                };

            case nameof(RndTechnologyTransfer):
                var rndTransfer = await context.RndTechnologyTransfers
                    .Include(item => item.RndProject).ThenInclude(project => project.Department)
                    .Include(item => item.RndProject).ThenInclude(project => project.RequestedBy)
                    .FirstOrDefaultAsync(item => item.Id == modelId);
                if (rndTransfer is null)
                    return Error.NotFound("RndTechnologyTransfer.NotFound", "Technology transfer not found.");
                return new ApprovalEntity
                {
                    ModelType = modelType, Id = modelId,
                    Code = $"{rndTransfer.RndProject.Code} · Transfer",
                    CreatedAt = rndTransfer.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(rndTransfer.RndProject.Department),
                    RequestedBy = mapper.Map<UserDto>(rndTransfer.RndProject.RequestedBy),
                    ApprovalLogs = GetApprovalLogs(modelId),
                };

            case nameof(CustomerQuotation):
                var quotation = await context.CustomerQuotations
                    .AsSplitQuery()
                    .Include(item => item.CreatedBy)
                        .ThenInclude(user => user.Department)
                    .Include(item => item.Approvals)
                        .ThenInclude(stage => stage.ApprovedBy)
                    .FirstOrDefaultAsync(item => item.Id == modelId);
                if (quotation is null)
                    return Error.NotFound("CustomerQuotation.NotFound", "Quotation not found.");
                return new ApprovalEntity
                {
                    ModelType = modelType,
                    Id = quotation.Id,
                    Code = quotation.Code,
                    CreatedAt = quotation.CreatedAt,
                    Department = mapper.Map<DepartmentDto>(quotation.CreatedBy?.Department),
                    RequestedBy = mapper.Map<UserDto>(quotation.CreatedBy),
                    ApprovalLogs = GetApprovalLogs(modelId),
                };

            default:
                throw new NotImplementedException(
                    $"Approval handling not implemented for model type: {modelType}"
                );
        }
    }

    public List<ResponsibleApprovalStage> GetCurrentApprovalStage(
        List<ResponsibleApprovalStage> stages,
        Guid userId,
        Guid roleId
    )
    {
        var result = new List<ResponsibleApprovalStage>();

        // Sort by order first
        var sortedStages = stages.OrderBy(s => s.Order).ToList();

        // Find the next required unapproved stage
        var nextRequired = sortedStages.FirstOrDefault(s =>
            s.Required
            && s.Status != ApprovalStatus.Approved
            && (s.UserId == userId || s.RoleId == roleId)
        );

        if (nextRequired == null)
        {
            // If there's no more required stages, return any unapproved unrequired ones
            result.AddRange(
                sortedStages.Where(s =>
                    !s.Required
                    && s.Status != ApprovalStatus.Approved
                    && (s.UserId == userId || s.RoleId == roleId)
                )
            );
            return result;
        }

        // Get all unrequired unapproved stages that come *before* the required one
        var priorUnrequired = sortedStages
            .Where(s =>
                !s.Required && s.Status != ApprovalStatus.Approved && s.Order < nextRequired.Order
            )
            .ToList();

        result.AddRange(priorUnrequired);
        result.Add(nextRequired);

        return result;
    }

    public async Task CreateInitialApprovalsAsync(string modelType, Guid modelId)
    {
        var configurationType = ApprovalDocumentPolicy.ConfigurationTypeFor(modelType);
        var approval = await context.Approvals.FirstOrDefaultAsync(item =>
            item.ItemType == configurationType
        );
        var stages = approval is null
            ? []
            : await context.ApprovalStages
                .Where(stage => stage.ApprovalId == approval.Id)
                .OrderBy(stage => stage.Order)
                .ToListAsync();

        // QC deliberately opts out of the automatic-approval fallback below. Every other
        // module treats "no workflow configured" as "approve it automatically"; for a QC
        // controlled document that would produce an approved record with no identified
        // approver and no re-authenticated signature, which is exactly what
        // meaning-of-signature forbids. QC fails loudly instead.
        if (QcWorksheetModelTypes.IsQcWorksheetModelType(modelType)
            && (approval is null || stages.Count == 0))
        {
            throw new InvalidOperationException(
                $"No approval workflow is configured for '{configurationType}'. A QC document "
                + "cannot be submitted for review until an administrator defines its approval "
                + "stages, because QC documents may never be automatically approved.");
        }

        if (approval is null || stages.Count == 0)
        {
            var reason = approval is null
                ? $"System auto-approved because no {configurationType} approval workflow is configured."
                : $"System auto-approved because the {configurationType} approval workflow has no stages.";
            logger.LogInformation(
                "Applying automatic approval for {ModelType} {ModelId}: {Reason}",
                modelType,
                modelId,
                reason
            );
            await AutomaticApprovalProcessor.ApplyAsync(
                context,
                modelType,
                modelId,
                reason,
                stepEventPublisher
            );
            return;
        }

        switch (modelType)
        {
            case "RawStockRequisition":
            case "PackageStockRequisition":
            case "PurchaseRequisition":
            case "TrialRequisition":
            case "Requisition":
                await CreateRequisitionApprovals(modelId, stages, approval);
                break;
            case nameof(BillingSheet):
                await CreateBillingSheetApprovals(modelId, stages, approval);
                break;
            case nameof(PurchaseOrder):
                await CreatePurchaseOrderApprovals(modelId, stages, approval);
                break;
            case nameof(LeaveRequest):
                await CreateLeaveRequestApprovals(modelId, stages, approval);
                break;
            case nameof(OvertimeRequest):
                await CreateOvertimeRequestApprovals(modelId, stages, approval);
                break;
            case nameof(PayrollRun):
                await CreatePayrollRunApprovals(modelId, stages, approval);
                break;
            case nameof(PerformanceReview):
                await CreatePerformanceReviewApprovals(modelId, stages, approval);
                break;
            case nameof(Response):
                await CreateResponseApprovals(modelId, stages, approval);
                break;
            case nameof(ProformaInvoice):
                await CreateProformaInvoiceApprovals(modelId, stages, approval);
                break;
            case nameof(ShipmentDocument):
                await CreateShipmentDocumentApprovals(modelId, stages, approval);
                break;
            case nameof(JobRequest):
                await CreateJobRequestApprovals(modelId, stages, approval);
                break;
            case nameof(ProductionExtraPacking):
                await CreateProductionExtraPackingApprovals(modelId, stages, approval);
                break;
            case nameof(FinishedGoodsTransferNote):
                await CreateFinishedGoodsTransferNoteApprovals(modelId, stages, approval);
                break;
            case nameof(StockAdjustment):
                await CreateStockAdjustmentApprovals(modelId, stages, approval);
                break;
            case nameof(AllocateProductionOrder):
                await AllocateProductionOrderApprovalHandler.CreateAsync(
                    context, modelId, stages, approval);
                break;
            case nameof(StaffRequisition):
                await CreateStaffRequisitionApprovals(modelId, stages, approval);
                break;
            case nameof(ProductionOrder):
                await CreateProductionOrderApprovals(modelId, stages, approval);
                break;
            case nameof(CustomerQuotation):
                await CustomerQuotationApprovalHandler.CreateAsync(
                    context, modelId, stages, approval);
                break;
            case nameof(RndProject):
                await RndProjectApprovalHandler.CreateAsync(context, modelId, stages, approval);
                break;
            case nameof(RndFormulation):
                await RndFormulationApprovalHandler.CreateAsync(context, modelId, stages, approval);
                break;
            case nameof(RndTechnologyTransfer):
                await RndTechnologyTransferApprovalHandler.CreateAsync(context, modelId, stages, approval);
                break;
            case QcWorksheetModelTypes.StandardTestProcedure:
            case QcWorksheetModelTypes.WorksheetTemplate:
            case QcWorksheetModelTypes.Specification:
            case QcWorksheetModelTypes.WorksheetInstance:
                await QcApprovalHandler.CreateAsync(context, modelType, modelId, stages, approval);
                break;
            default:
                throw new NotSupportedException(
                    $"Approval creation is not supported for model type '{modelType}'.");
        }
    }


    private async Task CreateRequisitionApprovals(
        Guid requisitionId,
        List<ApprovalStage> stages,
        Approval approval
    )
    {
        var exists = await context.RequisitionApprovals.AnyAsync(a =>
            a.RequisitionId == requisitionId && a.ApprovalId == approval.Id
        );
        if (exists)
            return;

        var approvals = stages
            .Select(stage => new RequisitionApproval
            {
                Required = stage.Required,
                Order = stage.Order,
                RequisitionId = requisitionId,
                CreatedAt = DateTime.UtcNow,
                ApprovalId = approval.Id,
                UserId = stage.UserId,
                RoleId = stage.RoleId,
                ActivatedAt = stage.Order == 1 ? DateTime.UtcNow : null,
            })
            .ToList();

        await context.RequisitionApprovals.AddRangeAsync(approvals);
        await context.SaveChangesAsync();
    }

    private async Task CreateBillingSheetApprovals(
        Guid sheetId,
        List<ApprovalStage> stages,
        Approval approval
    )
    {
        var exists = await context.BillingSheetApprovals.AnyAsync(a =>
            a.BillingSheetId == sheetId && a.ApprovalId == approval.Id
        );
        if (exists)
            return;

        var approvals = stages
            .Select(stage => new BillingSheetApproval
            {
                Required = stage.Required,
                Order = stage.Order,
                BillingSheetId = sheetId,
                CreatedAt = DateTime.UtcNow,
                ApprovalId = approval.Id,
                UserId = stage.UserId,
                RoleId = stage.RoleId,
                ActivatedAt = stage.Order == 1 ? DateTime.UtcNow : null,
            })
            .ToList();

        await context.BillingSheetApprovals.AddRangeAsync(approvals);
        await context.SaveChangesAsync();
    }

    private async Task CreatePurchaseOrderApprovals(
        Guid orderId,
        List<ApprovalStage> stages,
        Approval approval
    )
    {
        var exists = await context.PurchaseOrderApprovals.AnyAsync(a =>
            a.PurchaseOrderId == orderId && a.ApprovalId == approval.Id
        );
        if (exists)
            return;

        var approvals = stages
            .Select(stage => new PurchaseOrderApproval
            {
                Required = stage.Required,
                Order = stage.Order,
                PurchaseOrderId = orderId,
                CreatedAt = DateTime.UtcNow,
                ApprovalId = approval.Id,
                UserId = stage.UserId,
                RoleId = stage.RoleId,
                ActivatedAt = stage.Order == 1 ? DateTime.UtcNow : null,
            })
            .ToList();

        await context.PurchaseOrderApprovals.AddRangeAsync(approvals);
        await context.SaveChangesAsync();
    }

    private async Task CreateLeaveRequestApprovals(
        Guid leaveRequestId,
        List<ApprovalStage> stages,
        Approval approval
    )
    {
        var exists = await context.LeaveRequestApprovals.AnyAsync(a =>
            a.LeaveRequestId == leaveRequestId && a.ApprovalId == approval.Id
        );
        if (exists)
            return;

        var approvals = stages
            .Select(stage => new LeaveRequestApproval
            {
                Required = stage.Required,
                Order = stage.Order,
                LeaveRequestId = leaveRequestId,
                CreatedAt = DateTime.UtcNow,
                ApprovalId = approval.Id,
                UserId = stage.UserId,
                RoleId = stage.RoleId,
                ActivatedAt = stage.Order == 1 ? DateTime.UtcNow : null,
            })
            .ToList();

        await context.LeaveRequestApprovals.AddRangeAsync(approvals);
        await context.SaveChangesAsync();
    }

    private async Task CreatePayrollRunApprovals(
        Guid payrollRunId,
        List<ApprovalStage> stages,
        Approval approval
    )
    {
        var exists = await context.PayrollRunApprovals.AnyAsync(a =>
            a.PayrollRunId == payrollRunId && a.ApprovalId == approval.Id
        );
        if (exists)
            return;

        var approvals = stages
            .Select(stage => new PayrollRunApproval
            {
                Required = stage.Required,
                Order = stage.Order,
                PayrollRunId = payrollRunId,
                CreatedAt = DateTime.UtcNow,
                ApprovalId = approval.Id,
                UserId = stage.UserId,
                RoleId = stage.RoleId,
                ActivatedAt = stage.Order == 1 ? DateTime.UtcNow : null,
            })
            .ToList();

        await context.PayrollRunApprovals.AddRangeAsync(approvals);
        await context.SaveChangesAsync();
    }

    private async Task CreatePerformanceReviewApprovals(
        Guid performanceReviewId,
        List<ApprovalStage> stages,
        Approval approval
    )
    {
        var exists = await context.PerformanceReviewApprovals.AnyAsync(a =>
            a.PerformanceReviewId == performanceReviewId && a.ApprovalId == approval.Id
        );
        if (exists)
            return;

        var approvals = stages
            .Select(stage => new PerformanceReviewApproval
            {
                Required = stage.Required,
                Order = stage.Order,
                PerformanceReviewId = performanceReviewId,
                CreatedAt = DateTime.UtcNow,
                ApprovalId = approval.Id,
                UserId = stage.UserId,
                RoleId = stage.RoleId,
                ActivatedAt = stage.Order == 1 ? DateTime.UtcNow : null,
            })
            .ToList();

        await context.PerformanceReviewApprovals.AddRangeAsync(approvals);
        await context.SaveChangesAsync();
    }

    private async Task CreateOvertimeRequestApprovals(
        Guid overtimeRequestId,
        List<ApprovalStage> stages,
        Approval approval
    )
    {
        var exists = await context.OvertimeRequestApprovals.AnyAsync(a =>
            a.OvertimeRequestId == overtimeRequestId && a.ApprovalId == approval.Id
        );
        if (exists)
            return;

        var approvals = stages
            .Select(stage => new OvertimeRequestApproval
            {
                Required = stage.Required,
                Order = stage.Order,
                OvertimeRequestId = overtimeRequestId,
                CreatedAt = DateTime.UtcNow,
                ApprovalId = approval.Id,
                UserId = stage.UserId,
                RoleId = stage.RoleId,
                ActivatedAt = stage.Order == 1 ? DateTime.UtcNow : null,
            })
            .ToList();

        await context.OvertimeRequestApprovals.AddRangeAsync(approvals);
        await context.SaveChangesAsync();
    }

    private async Task CreateResponseApprovals(
        Guid responseId,
        List<ApprovalStage> stages,
        Approval approval
    )
    {
        await ResponseApprovalRoundManager.StartAsync(context, responseId, stages, approval);
    }

    private async Task CreateProductionOrderApprovals(
        Guid productionOrderId,
        List<ApprovalStage> stages,
        Approval approval
    )
    {
        var exists = await context.ProductionOrderApprovals.AnyAsync(a =>
            a.ProductionOrderId == productionOrderId && a.ApprovalId == approval.Id
        );
        if (exists)
            return;

        var approvals = stages
            .Select(stage => new ProductionOrderApprovals
            {
                Required = stage.Required,
                Order = stage.Order,
                ProductionOrderId = productionOrderId,
                CreatedAt = DateTime.UtcNow,
                ApprovalId = approval.Id,
                UserId = stage.UserId,
                RoleId = stage.RoleId,
                ActivatedAt = stage.Order == 1 ? DateTime.UtcNow : null,
            })
            .ToList();

        await context.ProductionOrderApprovals.AddRangeAsync(approvals);
        await context.SaveChangesAsync();
    }

    private async Task CreateShipmentDocumentApprovals(
        Guid shipmentDocumentId,
        List<ApprovalStage> stages,
        Approval approval
    )
    {
        var exists = await context.ShipmentDocumentApprovals.AnyAsync(a =>
            a.ShipmentDocumentId == shipmentDocumentId && a.ApprovalId == approval.Id
        );
        if (exists)
            return;

        var approvals = stages
            .Select(stage => new ShipmentDocumentApproval
            {
                Required = stage.Required,
                Order = stage.Order,
                ShipmentDocumentId = shipmentDocumentId,
                CreatedAt = DateTime.UtcNow,
                ApprovalId = approval.Id,
                UserId = stage.UserId,
                RoleId = stage.RoleId,
                ActivatedAt = stage.Order == 1 ? DateTime.UtcNow : null,
            })
            .ToList();

        await context.ShipmentDocumentApprovals.AddRangeAsync(approvals);
        await context.SaveChangesAsync();
    }

    private async Task CreateStaffRequisitionApprovals(
        Guid staffRequisitionId,
        List<ApprovalStage> stages,
        Approval approval
    )
    {
        var exists = await context.StaffRequisitionApprovals.AnyAsync(a =>
            a.StaffRequisitionId == staffRequisitionId && a.ApprovalId == approval.Id
        );
        if (exists)
            return;

        var approvals = stages
            .Select(stage => new StaffRequisitionApproval()
            {
                Required = stage.Required,
                Order = stage.Order,
                StaffRequisitionId = staffRequisitionId,
                CreatedAt = DateTime.UtcNow,
                ApprovalId = approval.Id,
                UserId = stage.UserId,
                RoleId = stage.RoleId,
                ActivatedAt = stage.Order == 1 ? DateTime.UtcNow : null,
            })
            .ToList();

        await context.StaffRequisitionApprovals.AddRangeAsync(approvals);
        await context.SaveChangesAsync();
    }

    private async Task CreateJobRequestApprovals(
        Guid jobRequestId,
        List<ApprovalStage> stages,
        Approval approval
    )
    {
        var exists = await context.JobRequestApprovals.AnyAsync(a =>
            a.JobRequestId == jobRequestId && a.ApprovalId == approval.Id
        );
        if (exists)
            return;

        var approvals = stages
            .Select(stage => new JobRequestApproval()
            {
                Required = stage.Required,
                Order = stage.Order,
                JobRequestId = jobRequestId,
                CreatedAt = DateTime.UtcNow,
                ApprovalId = approval.Id,
                UserId = stage.UserId,
                RoleId = stage.RoleId,
                ActivatedAt = stage.Order == 1 ? DateTime.UtcNow : null,
            })
            .ToList();

        await context.JobRequestApprovals.AddRangeAsync(approvals);
        await context.SaveChangesAsync();
    }

    private async Task CreateProductionExtraPackingApprovals(
        Guid productionExtraPackingId,
        List<ApprovalStage> stages,
        Approval approval
    )
    {
        var exists = await context.ProductionExtraPackingApprovals.AnyAsync(a =>
            a.ProductionExtraPackingId == productionExtraPackingId && a.ApprovalId == approval.Id
        );
        if (exists)
            return;

        var approvals = stages
            .Select(stage => new ProductionExtraPackingApproval()
            {
                Required = stage.Required,
                Order = stage.Order,
                ProductionExtraPackingId = productionExtraPackingId,
                CreatedAt = DateTime.UtcNow,
                ApprovalId = approval.Id,
                UserId = stage.UserId,
                RoleId = stage.RoleId,
                ActivatedAt = stage.Order == 1 ? DateTime.UtcNow : null,
            })
            .ToList();

        await context.ProductionExtraPackingApprovals.AddRangeAsync(approvals);
        await context.SaveChangesAsync();
    }

    private async Task CreateFinishedGoodsTransferNoteApprovals(
        Guid finishedGoodsTransferNoteId,
        List<ApprovalStage> stages,
        Approval approval
    )
    {
        var exists = await context.FinishedGoodsTransferNoteApprovals.AnyAsync(a =>
            a.FinishedGoodsTransferNoteId == finishedGoodsTransferNoteId
            && a.ApprovalId == approval.Id
        );
        if (exists)
            return;

        var approvals = stages
            .Select(stage => new FinishedGoodsTransferNoteApproval()
            {
                Required = stage.Required,
                Order = stage.Order,
                FinishedGoodsTransferNoteId = finishedGoodsTransferNoteId,
                CreatedAt = DateTime.UtcNow,
                ApprovalId = approval.Id,
                UserId = stage.UserId,
                RoleId = stage.RoleId,
                ActivatedAt = stage.Order == 1 ? DateTime.UtcNow : null,
            })
            .ToList();

        await context.FinishedGoodsTransferNoteApprovals.AddRangeAsync(approvals);
        await context.SaveChangesAsync();
    }

    private async Task CreateStockAdjustmentApprovals(
        Guid adjustmentId,
        List<ApprovalStage> stages,
        Approval approval
    )
    {
        var exists = await context.StockAdjustmentApprovals.AnyAsync(a =>
            a.StockAdjustmentId == adjustmentId && a.ApprovalId == approval.Id
        );
        if (exists)
            return;

        var approvals = stages
            .Select(stage => new StockAdjustmentApproval
            {
                Required = stage.Required,
                Order = stage.Order,
                StockAdjustmentId = adjustmentId,
                CreatedAt = DateTime.UtcNow,
                ApprovalId = approval.Id,
                UserId = stage.UserId,
                RoleId = stage.RoleId,
                ActivatedAt = stage.Order == 1 ? DateTime.UtcNow : null,
            })
            .ToList();

        await context.StockAdjustmentApprovals.AddRangeAsync(approvals);
        await context.SaveChangesAsync();
    }

    private async Task CreateProformaInvoiceApprovals(
        Guid proformaInvoiceId,
        List<ApprovalStage> stages,
        Approval approval
    )
    {
        var exists = await context.ProformaInvoiceApprovals.AnyAsync(a =>
            a.ProformaInvoiceId == proformaInvoiceId && a.ApprovalId == approval.Id
        );
        if (exists)
            return;

        var approvals = stages
            .Select(stage => new ProformaInvoiceApproval
            {
                Required = stage.Required,
                Order = stage.Order,
                ProformaInvoiceId = proformaInvoiceId,
                CreatedAt = DateTime.UtcNow,
                ApprovalId = approval.Id,
                UserId = stage.UserId,
                RoleId = stage.RoleId,
                ActivatedAt = stage.Order == 1 ? DateTime.UtcNow : null,
            })
            .ToList();

        await context.ProformaInvoiceApprovals.AddRangeAsync(approvals);
        await context.SaveChangesAsync();
    }

    private async Task AddApprovalLogs(CreateApprovalLog log)
    {
        await context.ApprovalActionLogs.AddAsync(
            new ApprovalActionLog
            {
                UserId = log.UserId,
                ModelId = log.ModelId,
                Status = log.Status,
                CreatedAt = DateTime.UtcNow,
                Comments = log.Comments,
            }
        );
        await context.SaveChangesAsync();
    }

    public List<ApprovalLog> GetApprovalLogs(Guid modelId)
    {
        return context
            .ApprovalActionLogs.AsSplitQuery()
            .Include(log => log.User)
            .Where(log => log.ModelId == modelId)
            .OrderBy(log => log.CreatedAt)
            .Select(log => new ApprovalLog
            {
                User = mapper.Map<CollectionItemDto>(log.User),
                ApprovedAt = log.CreatedAt,
                Comments = log.Comments,
                Status = log.Status,
            })
            .ToList();
    }

    public async Task ProcessApprovalEscalations(Guid userId, Guid roleId)
    {
        var cacheKey = "ApprovalsWithEscalation";
        if (!cache.TryGetValue(cacheKey, out List<Approval> approvalsWithEscalation))
        {
            approvalsWithEscalation = await context.Approvals.ToListAsync();

            var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(
                TimeSpan.FromMinutes(10)
            ); // Adjust expiration as needed

            cache.Set(cacheKey, approvalsWithEscalation, cacheEntryOptions);
        }

        // Get entities that are not fully approved
        var unapprovedRequisitions = await context
            .Requisitions.AsSplitQuery()
            .Where(r => !r.Approved)
            .Include(requisition => requisition.Approvals)
            .ToListAsync();

        var unapprovedBillingSheets = await context
            .BillingSheets.AsSplitQuery()
            .Where(r => !r.Approved)
            .Include(requisition => requisition.Approvals)
            .ToListAsync();

        var unapprovedPurchaseOrders = await context
            .PurchaseOrders.AsSplitQuery()
            .Where(r => !r.Approved)
            .Include(requisition => requisition.Approvals)
            .ToListAsync();

        var unapprovedLeaveRequests = await context
            .LeaveRequests.AsSplitQuery()
            .Where(r => !r.Approved)
            .Include(requisition => requisition.Approvals)
            .ToListAsync();

        var unapprovedOverTimeRequests = await context
            .OvertimeRequests.AsSplitQuery()
            .Where(r => !r.Approved)
            .Include(requisition => requisition.Approvals)
            .ToListAsync();

        foreach (var approval in approvalsWithEscalation)
        {
            switch (approval.ItemType)
            {
                case "PurchaseRequisition":
                case "StockRequisition":
                    var requisition = unapprovedRequisitions.FirstOrDefault(r =>
                        r.Approvals.Any(a => a.ApprovalId == approval.Id)
                    );
                    if (requisition != null)
                    {
                        await ProcessRequisitionEscalations(
                            requisition,
                            approval.EscalationDuration,
                            userId,
                            roleId
                        );
                    }
                    break;
                case "BillingSheet":
                    var billingSheet = unapprovedBillingSheets.FirstOrDefault(bs =>
                        bs.Approvals.Any(a => a.ApprovalId == approval.Id)
                    );
                    if (billingSheet != null)
                    {
                        await ProcessBillingSheetEscalations(
                            billingSheet,
                            approval.EscalationDuration,
                            userId,
                            roleId
                        );
                    }
                    break;
                case "PurchaseOrder":
                    var purchaseOrder = unapprovedPurchaseOrders.FirstOrDefault(po =>
                        po.Approvals.Any(a => a.ApprovalId == approval.Id)
                    );
                    if (purchaseOrder != null)
                    {
                        await ProcessPurchaseOrderEscalations(
                            purchaseOrder,
                            approval.EscalationDuration,
                            userId,
                            roleId
                        );
                    }
                    break;
                case nameof(LeaveRequest):
                    var leaveRequest = unapprovedLeaveRequests.FirstOrDefault(po =>
                        po.Approvals.Any(a => a.ApprovalId == approval.Id)
                    );
                    if (leaveRequest != null)
                    {
                        await ProcessLeaveRequestEscalations(
                            leaveRequest,
                            approval.EscalationDuration,
                            userId,
                            roleId
                        );
                    }
                    break;

                case nameof(OvertimeRequest):
                    var overtimeRequest = unapprovedOverTimeRequests.FirstOrDefault(po =>
                        po.Approvals.Any(a => a.ApprovalId == approval.Id)
                    );
                    if (overtimeRequest != null)
                    {
                        await ProcessOvertimeRequestEscalations(
                            overtimeRequest,
                            approval.EscalationDuration,
                            userId,
                            roleId
                        );
                    }
                    break;
            }
        }
    }

    public Result DelegateApproval(DelegateApproval approval)
    {
        var user = userManager.FindByIdAsync(approval.EmployeeId.ToString());

        return user.Result == null
            ? Error.NotFound("User.Invalid", "User not found")
            : Result.Success();
    }

    private async Task ProcessRequisitionEscalations(
        Requisition requisition,
        TimeSpan escalationDuration,
        Guid userId,
        Guid roleId
    )
    {
        if (requisition is null || requisition.Approvals.Count == 0)
            return;

        var currentApprovalStages = GetCurrentApprovalStage(
            requisition
                .Approvals.Select(a => new ResponsibleApprovalStage
                {
                    Status = a.Status,
                    Required = a.Required,
                    Order = a.Order,
                    CreatedAt = a.CreatedAt,
                    ActivatedAt = a.ActivatedAt, // Include ActivatedAt
                })
                .ToList(),
            userId,
            roleId
        );

        if (currentApprovalStages.Count <= 1)
            return;

        var stagesToAutoApprove = currentApprovalStages
            .Where(s =>
                s.Status == ApprovalStatus.Pending
                && s.ActivatedAt.HasValue
                && DateTime.UtcNow - s.ActivatedAt.Value > escalationDuration
            )
            .ToList();

        if (stagesToAutoApprove.Count != 0)
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
            var allRequiredApproved = requisition
                .Approvals.Where(s => s.Required)
                .All(s => s.Status == ApprovalStatus.Approved);

            if (allRequiredApproved)
            {
                requisition.Approved = true;
            }

            context.Requisitions.Update(requisition);
            await context.SaveChangesAsync();
        }
    }

    private async Task ProcessBillingSheetEscalations(
        BillingSheet billingSheet,
        TimeSpan escalationDuration,
        Guid userId,
        Guid roleId
    )
    {
        if (billingSheet is null || billingSheet.Approvals.Count == 0)
            return;

        var currentApprovalStages = GetCurrentApprovalStage(
            billingSheet
                .Approvals.Select(a => new ResponsibleApprovalStage
                {
                    Status = a.Status,
                    Required = a.Required,
                    Order = a.Order,
                    CreatedAt = a.CreatedAt,
                    ActivatedAt = a.ActivatedAt,
                })
                .ToList(),
            userId,
            roleId
        );

        if (currentApprovalStages.Count <= 1)
            return;

        var stagesToAutoApprove = currentApprovalStages
            .Where(s =>
                s.Status == ApprovalStatus.Pending
                && s.ActivatedAt.HasValue
                && DateTime.UtcNow - s.ActivatedAt.Value > escalationDuration
            )
            .ToList();

        if (stagesToAutoApprove.Count != 0)
        {
            foreach (var stage in stagesToAutoApprove)
            {
                //var actualStage = billingSheet.Approvals.First(a => a.Id == stage.Id);
                stage.Status = ApprovalStatus.Approved;
                stage.ApprovalTime = DateTime.UtcNow;
                stage.Comments = "Approval stage exceeded escalation duration.";
            }

            var allRequiredApproved = billingSheet
                .Approvals.Where(s => s.Required)
                .All(s => s.Status == ApprovalStatus.Approved);

            if (allRequiredApproved)
            {
                billingSheet.Approved = true;
            }

            context.BillingSheets.Update(billingSheet);
            await context.SaveChangesAsync();
        }
    }

    private async Task ProcessPurchaseOrderEscalations(
        PurchaseOrder purchaseOrder,
        TimeSpan escalationDuration,
        Guid userId,
        Guid roleId
    )
    {
        if (purchaseOrder is null || purchaseOrder.Approvals.Count == 0)
            return;

        var currentApprovalStages = GetCurrentApprovalStage(
            purchaseOrder
                .Approvals.Select(a => new ResponsibleApprovalStage
                {
                    Status = a.Status,
                    Required = a.Required,
                    Order = a.Order,
                    CreatedAt = a.CreatedAt,
                    ActivatedAt = a.ActivatedAt,
                })
                .ToList(),
            userId,
            roleId
        );

        if (currentApprovalStages.Count <= 1)
            return;

        var stagesToAutoApprove = currentApprovalStages
            .Where(s =>
                s.Status == ApprovalStatus.Pending
                && s.ActivatedAt.HasValue
                && DateTime.UtcNow - s.ActivatedAt.Value > escalationDuration
            )
            .ToList();

        if (stagesToAutoApprove.Count != 0)
        {
            foreach (var stage in stagesToAutoApprove)
            {
                //var actualStage = purchaseOrder.Approvals.First(a => a.Id == stage.Id);
                stage.Status = ApprovalStatus.Approved;
                stage.ApprovalTime = DateTime.UtcNow;
                stage.Comments = "Approval stage exceeded escalation duration.";
            }

            var allRequiredApproved = purchaseOrder
                .Approvals.Where(s => s.Required)
                .All(s => s.Status == ApprovalStatus.Approved);

            if (allRequiredApproved)
            {
                purchaseOrder.Approved = true;
            }

            await context.SaveChangesAsync();
        }
    }

    private async Task ProcessLeaveRequestEscalations(
        LeaveRequest leaveRequest,
        TimeSpan escalationDuration,
        Guid userId,
        Guid roleId
    )
    {
        if (leaveRequest is null || leaveRequest.Approvals.Count == 0)
            return;

        var currentApprovalStages = GetCurrentApprovalStage(
            leaveRequest
                .Approvals.Select(a => new ResponsibleApprovalStage
                {
                    Status = a.Status,
                    Required = a.Required,
                    Order = a.Order,
                    CreatedAt = a.CreatedAt,
                    ActivatedAt = a.ActivatedAt,
                })
                .ToList(),
            userId,
            roleId
        );

        if (currentApprovalStages.Count <= 1)
            return;

        var stagesToAutoApprove = currentApprovalStages
            .Where(s =>
                s.Status == ApprovalStatus.Pending
                && s.ActivatedAt.HasValue
                && DateTime.UtcNow - s.ActivatedAt.Value > escalationDuration
            )
            .ToList();

        if (stagesToAutoApprove.Count != 0)
        {
            foreach (var stage in stagesToAutoApprove)
            {
                //var actualStage = purchaseOrder.Approvals.First(a => a.Id == stage.Id);
                stage.Status = ApprovalStatus.Approved;
                stage.ApprovalTime = DateTime.UtcNow;
                stage.Comments = "Approval stage exceeded escalation duration.";
            }

            var allRequiredApproved = leaveRequest
                .Approvals.Where(s => s.Required)
                .All(s => s.Status == ApprovalStatus.Approved);

            if (allRequiredApproved)
            {
                leaveRequest.Approved = true;
                leaveRequest.LeaveStatus = LeaveStatus.Approved;
            }

            await context.SaveChangesAsync();
        }
    }

    private async Task ProcessOvertimeRequestEscalations(
        OvertimeRequest overtimeRequest,
        TimeSpan escalationDuration,
        Guid userId,
        Guid roleId
    )
    {
        if (overtimeRequest is null || overtimeRequest.Approvals.Count == 0)
            return;

        var currentApprovalStages = GetCurrentApprovalStage(
            overtimeRequest
                .Approvals.Select(a => new ResponsibleApprovalStage
                {
                    Status = a.Status,
                    Required = a.Required,
                    Order = a.Order,
                    CreatedAt = a.CreatedAt,
                    ActivatedAt = a.ActivatedAt,
                })
                .ToList(),
            userId,
            roleId
        );

        if (currentApprovalStages.Count <= 1)
            return;

        var stagesToAutoApprove = currentApprovalStages
            .Where(s =>
                s.Status == ApprovalStatus.Pending
                && s.ActivatedAt.HasValue
                && DateTime.UtcNow - s.ActivatedAt.Value > escalationDuration
            )
            .ToList();

        if (stagesToAutoApprove.Count != 0)
        {
            foreach (var stage in stagesToAutoApprove)
            {
                //var actualStage = purchaseOrder.Approvals.First(a => a.Id == stage.Id);
                stage.Status = ApprovalStatus.Approved;
                stage.ApprovalTime = DateTime.UtcNow;
                stage.Comments = "Approval stage exceeded escalation duration.";
            }

            var allRequiredApproved = overtimeRequest
                .Approvals.Where(s => s.Required)
                .All(s => s.Status == ApprovalStatus.Approved);

            if (allRequiredApproved)
            {
                overtimeRequest.Approved = true;
                overtimeRequest.ApprovalStatus = ApprovalStatus.Approved;
                overtimeRequest.Status = OvertimeStatus.Approved;
            }

            await context.SaveChangesAsync();
        }
    }

    /*private async Task<Result> AllocateProduct(ProductionOrder request)
    {
        var productionOrder = await context.ProductionOrders
            .AsSplitQuery()
            .Include(p => p.Products)
                .ThenInclude(p => p.FulfilledQuantities)
            .FirstOrDefaultAsync(f => f.Id == request.ProductionOrderId);

        if (productionOrder == null)
            return Error.NotFound("ProductionOrder.NotFound", "Production order not found");

        foreach (var product in request.Products)
        {
            var allocationProduct = productionOrder.Products.FirstOrDefault(p => p.ProductId == product.ProductId);
            if (allocationProduct == null)
                return Error.NotFound("ProductionOrder.ProductNotFound",
                    $"Product {product.ProductId} not found in this production order");

            if (allocationProduct.RemainingQuantity == 0)
                return Error.Validation("ProductionOrder.Product",
                    $"Product {product.ProductId} has already been allocated completely.");

            if (allocationProduct.Fulfilled)
                return Error.Validation("ProductionOrder.Product",
                    "Product has already been marked as fulfilled.");

            var totalToAllocate = product.FulfilledQuantities.Sum(q => q.Quantity);
            if (totalToAllocate > allocationProduct.RemainingQuantity)
            {
                return Error.Validation("ProductionOrder.Product",
                    $"Allocation quantity {totalToAllocate} is more than what is left to be fulfilled {allocationProduct.RemainingQuantity}");
            }

            foreach (var quantityToFulfill in product.FulfilledQuantities)
            {
                var finishedGoodsTransferNote = await context.FinishedGoodsTransferNotes
                    .FirstOrDefaultAsync(f => f.Id == quantityToFulfill.FinishedGoodsTransferNoteId);

                if (finishedGoodsTransferNote is null)
                    return Error.NotFound("ProductionOrder.FinishedGoodsTransferNoteNotFound",
                        $"Finished goods transfer note {quantityToFulfill.FinishedGoodsTransferNoteId} not found.");

                if (finishedGoodsTransferNote.RemainingQuantity == 0)
                    return Error.Validation("ProductionOrder.FinishedGoodsTransferNoteValidation",
                        $"The finished good transfer note {quantityToFulfill.FinishedGoodsTransferNoteId} does not have any remaining quantity.");

                if (quantityToFulfill.Quantity > finishedGoodsTransferNote.RemainingQuantity)
                    return Error.Validation("ProductionOrder.FinishedGoodsTransferNoteValidation",
                        $"Trying to allocate {quantityToFulfill.Quantity}, " +
                        $"but only {finishedGoodsTransferNote.RemainingQuantity} is left in transfer note {quantityToFulfill.FinishedGoodsTransferNoteId}.");

                // Check if an allocation for this note already exists
                var existingAllocationProductForNote = allocationProduct
                    .FulfilledQuantities
                    .FirstOrDefault(p => p.FinishedGoodsTransferNoteId == quantityToFulfill.FinishedGoodsTransferNoteId);

                if (existingAllocationProductForNote is not null)
                {
                    existingAllocationProductForNote.Quantity += quantityToFulfill.Quantity;
                }
                else
                {
                    allocationProduct.FulfilledQuantities.Add(new ProductionOrderProductQuantity
                    {
                        Quantity = quantityToFulfill.Quantity,
                        FinishedGoodsTransferNoteId = quantityToFulfill.FinishedGoodsTransferNoteId
                    });
                }

                finishedGoodsTransferNote.AllocatedQuantity += quantityToFulfill.Quantity;
            }
        }

        // Save all changes once
        await context.SaveChangesAsync();

        // Mark products as fulfilled if no remaining quantity
        foreach (var product in productionOrder.Products)
        {
            if (product.RemainingQuantity == 0 && !product.Fulfilled)
            {
                product.Fulfilled = true;
            }
        }
        request.Approved = true;
        context.ProductionOrders.Update(productionOrder);
        context.ProductionOrders.Update(request);
        await context.SaveChangesAsync();
        return Result.Success();
    }*/
    public async Task<Result> TransferApprovalRights(TransferApprovalRequest request)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // 1. Update Approval Templates
            var templateStages = await context
                .ApprovalStages.Where(s => s.UserId == request.FromUserId)
                .ToListAsync();

            foreach (var stage in templateStages)
            {
                stage.UserId = request.ToUserId;
            }

            // 2. Update Pending Approval Instances
            // Requisitions
            var requisitionApprovals = await context
                .RequisitionApprovals.Where(a =>
                    a.UserId == request.FromUserId && a.Status == ApprovalStatus.Pending
                )
                .ToListAsync();
            foreach (var a in requisitionApprovals)
                a.UserId = request.ToUserId;

            // Purchase Orders
            var poApprovals = await context
                .PurchaseOrderApprovals.Where(a =>
                    a.UserId == request.FromUserId && a.Status == ApprovalStatus.Pending
                )
                .ToListAsync();
            foreach (var a in poApprovals)
                a.UserId = request.ToUserId;

            // Billing Sheets
            var bsApprovals = await context
                .BillingSheetApprovals.Where(a =>
                    a.UserId == request.FromUserId && a.Status == ApprovalStatus.Pending
                )
                .ToListAsync();
            foreach (var a in bsApprovals)
                a.UserId = request.ToUserId;

            // Shipment Documents
            var sdApprovals = await context
                .ShipmentDocumentApprovals.Where(a =>
                    a.UserId == request.FromUserId && a.Status == ApprovalStatus.Pending
                )
                .ToListAsync();
            foreach (var a in sdApprovals)
                a.UserId = request.ToUserId;

            // Staff Requisitions
            var srApprovals = await context
                .StaffRequisitionApprovals.Where(a =>
                    a.UserId == request.FromUserId && a.Status == ApprovalStatus.Pending
                )
                .ToListAsync();
            foreach (var a in srApprovals)
                a.UserId = request.ToUserId;

            // Leave Requests
            var lrApprovals = await context
                .LeaveRequestApprovals.Where(a =>
                    a.UserId == request.FromUserId && a.Status == ApprovalStatus.Pending
                )
                .ToListAsync();
            foreach (var a in lrApprovals)
                a.UserId = request.ToUserId;

            // Overtime Requests
            var orApprovals = await context
                .OvertimeRequestApprovals.Where(a =>
                    a.UserId == request.FromUserId && a.Status == ApprovalStatus.Pending
                )
                .ToListAsync();
            foreach (var a in orApprovals)
                a.UserId = request.ToUserId;

            // Job Requests
            var jrApprovals = await context
                .JobRequestApprovals.Where(a =>
                    a.UserId == request.FromUserId && a.Status == ApprovalStatus.Pending
                )
                .ToListAsync();
            foreach (var a in jrApprovals)
                a.UserId = request.ToUserId;

            // Stock Adjustments
            var saApprovals = await context
                .StockAdjustmentApprovals.Where(a =>
                    a.UserId == request.FromUserId && a.Status == ApprovalStatus.Pending
                )
                .ToListAsync();
            foreach (var a in saApprovals)
                a.UserId = request.ToUserId;

            // Service Memos
            var smApprovals = await context
                .ServiceMemoApprovals.Where(a =>
                    a.UserId == request.FromUserId && a.Status == ApprovalStatus.Pending
                )
                .ToListAsync();
            foreach (var a in smApprovals)
                a.UserId = request.ToUserId;

            // Proforma Invoices
            var piApprovals = await context
                .ProformaInvoiceApprovals.Where(a =>
                    a.UserId == request.FromUserId && a.Status == ApprovalStatus.Pending
                )
                .ToListAsync();
            foreach (var a in piApprovals)
                a.UserId = request.ToUserId;

            // Production Orders
            var productionOrderApprovals = await context
                .ProductionOrderApprovals.Where(a =>
                    a.UserId == request.FromUserId && a.Status == ApprovalStatus.Pending
                )
                .ToListAsync();
            foreach (var a in productionOrderApprovals)
                a.UserId = request.ToUserId;

            // Customer Quotations
            var customerQuotationApprovals = await context
                .CustomerQuotationApprovals.Where(a =>
                    a.UserId == request.FromUserId && a.Status == ApprovalStatus.Pending
                )
                .ToListAsync();
            foreach (var a in customerQuotationApprovals)
                a.UserId = request.ToUserId;

            // Allocate Production Orders
            var apoApprovals = await context
                .AllocateProductionOrderApprovals.Where(a =>
                    a.UserId == request.FromUserId && a.Status == ApprovalStatus.Pending
                )
                .ToListAsync();
            foreach (var a in apoApprovals)
                a.UserId = request.ToUserId;

            // Finished Goods Transfer Notes
            var fgtnApprovals = await context
                .FinishedGoodsTransferNoteApprovals.Where(a =>
                    a.UserId == request.FromUserId && a.Status == ApprovalStatus.Pending
                )
                .ToListAsync();
            foreach (var a in fgtnApprovals)
                a.UserId = request.ToUserId;

            // Production Extra Packing
            var pepApprovals = await context
                .ProductionExtraPackingApprovals.Where(a =>
                    a.UserId == request.FromUserId && a.Status == ApprovalStatus.Pending
                )
                .ToListAsync();
            foreach (var a in pepApprovals)
                a.UserId = request.ToUserId;

            // Response Approvals
            var responseApprovals = await context
                .ResponseApprovals.Where(a =>
                    a.UserId == request.FromUserId && a.Status == ApprovalStatus.Pending
                    && a.ActivatedAt.HasValue
                    && a.ApprovalRound
                        == context.ResponseApprovals
                            .Where(item => item.ResponseId == a.ResponseId)
                            .Max(item => item.ApprovalRound)
                )
                .ToListAsync();
            foreach (var a in responseApprovals)
                a.UserId = request.ToUserId;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(
                ex,
                "Error transferring approval rights from {FromUserId} to {ToUserId}",
                request.FromUserId,
                request.ToUserId
            );
            return Error.Failure(
                "Approval.TransferError",
                "An error occurred while transferring approval rights."
            );
        }
    }
}

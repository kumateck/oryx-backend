using APP.Repository;
using APP.Services.Reviews;
using APP.Tests.Repository;
using APP.Utils;
using DOMAIN.Entities.Designations;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.LeaveRequests;
using DOMAIN.Entities.OvertimeRequests;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.PurchaseOrders.Request;
using Microsoft.Extensions.DependencyInjection;
using DOMAIN.Entities.Reviews;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.StaffRequisitions;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Reviews;

file sealed class NoUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

/// <summary>Phase 2 document types: billing sheets, staff requisitions, overtime, leave and job requests, packing lists.</summary>
public class DocumentReviewBatchTests
{
    private readonly ApplicationDbContext db = new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new NoUser());
    private int claimId;

    private ApprovalRepository Approvals() => new(db, null!, null!, null!, NullLogger<ApprovalRepository>.Instance, null!,
        new NoOpProductionActivityStepEventPublisher());
    private DocumentReviewService Service() => new(db, Approvals(), null!);

    private async Task<Guid> Person(string name, params string[] keys)
    {
        var id = Guid.NewGuid();
        var role = new Role { Id = Guid.NewGuid(), Name = name + " role" };
        db.Users.Add(new User { Id = id, UserName = name, FirstName = name, LastName = "Tester" });
        db.Roles.Add(role);
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = id, RoleId = role.Id });
        foreach (var key in keys)
        {
            var claim = ++claimId;
            db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = claim, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = key });
            db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = claim, Key = key, Type = "View" });
        }
        await db.SaveChangesAsync();
        return id;
    }

    /// <summary>A record of the type, prepared by <paramref name="preparer"/>.</summary>
    private async Task<Guid> Record(string type, Guid preparer)
    {
        var id = Guid.NewGuid();
        switch (type)
        {
            case ReviewDocumentTypes.BillingSheet:
                db.BillingSheets.Add(new BillingSheet { Id = id, Code = "BS-1" });
                break;
            case ReviewDocumentTypes.StaffRequisition:
                var designation = new Designation { Id = Guid.NewGuid(), Name = "Pharmacist" };
                db.Designations.Add(designation);
                db.StaffRequisitions.Add(new StaffRequisition { Id = id, StaffRequired = 2, DesignationId = designation.Id, Designation = designation });
                break;
            case ReviewDocumentTypes.OvertimeRequest:
                db.OvertimeRequests.Add(new OvertimeRequest { Id = id, Code = "OT-1", StartTime = "05:00 PM", EndTime = "07:00 PM" });
                break;
            case ReviewDocumentTypes.JobRequest:
                db.JobRequests.Add(new DOMAIN.Entities.JobRequests.JobRequest { Id = id, Code = "JR-1" });
                break;
            case ReviewDocumentTypes.PackingList:
                var order = new DOMAIN.Entities.ProductionOrders.ProductionOrder { Id = Guid.NewGuid(), Code = "PO-77" };
                db.ProductionOrders.Add(order);
                db.AllocateProductionOrders.Add(new DOMAIN.Entities.ProductionOrders.AllocateProductionOrder { Id = id, ProductionOrderId = order.Id, ProductionOrder = order });
                break;
            case ReviewDocumentTypes.LeaveRequest:
                var employee = new Employee { Id = Guid.NewGuid(), FirstName = "Ama", LastName = "Owusu" };
                db.Employees.Add(employee);
                db.LeaveRequests.Add(new LeaveRequest { Id = id, EmployeeId = employee.Id, Employee = employee, StartDate = new DateTime(2026, 12, 1) });
                break;
        }
        await db.SaveChangesAsync();
        // The context stamps the creator from the signed-in user when a row is added.
        db.ChangeTracker.Entries<DOMAIN.Entities.Base.BaseEntity>().First(item => item.Entity.Id == id).Entity.CreatedById = preparer;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return id;
    }

    private async Task Configure(string type, Guid reviewer)
    {
        db.ReviewConfigurations.Add(new ReviewConfiguration { Id = Guid.NewGuid(), ItemType = type,
            Assignees = [new ReviewConfigurationAssignee { Id = Guid.NewGuid(), UserId = reviewer }] });
        await db.SaveChangesAsync();
    }

    public static TheoryData<string, string, string, string, string> Types => new()
    {
        { ReviewDocumentTypes.BillingSheet, PermissionKeys.CanViewBillingSheet, PermissionKeys.CanReviewBillingSheet, PermissionKeys.CanReturnBillingSheet, PermissionKeys.CanEditBillingSheet },
        { ReviewDocumentTypes.StaffRequisition, PermissionKeys.CanViewStaffRequisition, PermissionKeys.CanReviewStaffRequisition, PermissionKeys.CanReturnStaffRequisition, PermissionKeys.CanEditStaffRequisition },
        { ReviewDocumentTypes.OvertimeRequest, PermissionKeys.CanViewOvertimeManagement, PermissionKeys.CanReviewOvertimeRequest, PermissionKeys.CanReturnOvertimeRequest, PermissionKeys.CanEditOvertimeManagement },
        { ReviewDocumentTypes.LeaveRequest, PermissionKeys.CanViewLeaveRequests, PermissionKeys.CanReviewLeaveRequest, PermissionKeys.CanReturnLeaveRequest, PermissionKeys.CanEditLeaveRequest },
        { ReviewDocumentTypes.JobRequest, PermissionKeys.CanViewJobRequests, PermissionKeys.CanReviewJobRequest, PermissionKeys.CanReturnJobRequest, PermissionKeys.CanEditJobRequest },
        { ReviewDocumentTypes.PackingList, PermissionKeys.CanViewPackingList, PermissionKeys.CanReviewPackingList, PermissionKeys.CanReturnPackingList, PermissionKeys.CanGeneratePackingList },
    };

    [Theory]
    [MemberData(nameof(Types))]
    public async Task Each_type_is_held_reviewed_returned_and_resubmitted_with_its_own_keys(string type, string view, string confirm, string send, string edit)
    {
        var preparer = await Person("Preparer", edit);
        var reviewer = await Person("Reviewer", view, confirm, send);
        var outsider = await Person("Outsider", PermissionKeys.CanReviewRawMaterialPurchaseRequisition);
        var id = await Record(type, preparer);
        Assert.True(ReviewDocumentTypes.IsConfigurable(type));

        await Configure(type, reviewer);
        await Approvals().CreateInitialApprovalsAsync(type, id, preparer);
        var review = await db.DocumentReviews.AsNoTracking().SingleAsync(item => item.ModelId == id);
        Assert.Equal(DocumentReviewStatus.AwaitingReview, review.Status);
        Assert.Equal(preparer, review.PreparedById);

        Assert.Equal(ErrorType.Forbidden, (await Service().Confirm(type, id, new(), outsider)).Errors[0].Type);
        Assert.Equal(ErrorType.Forbidden, (await Service().GetState(type, id, outsider)).Errors[0].Type);
        Assert.True((await Service().Return(type, id, new() { Comments = "Wrong date" }, reviewer)).IsSuccess);
        // Submitted again through its own flow, it goes back to review rather than to approval.
        await Approvals().CreateInitialApprovalsAsync(type, id, preparer);
        Assert.Equal(DocumentReviewStatus.AwaitingReview, (await db.DocumentReviews.AsNoTracking().SingleAsync(item => item.ModelId == id)).Status);
        Assert.True((await Service().Return(type, id, new() { Comments = "Still wrong" }, reviewer)).IsSuccess);
        Assert.True((await Service().Resubmit(type, id, new(), preparer)).IsSuccess);

        var state = (await Service().GetState(type, id, reviewer)).Value;
        Assert.True(state.CanConfirm);
        Assert.False(string.IsNullOrWhiteSpace(state.Code));
        Assert.True((await Service().Confirm(type, id, new(), reviewer)).IsSuccess);
        var confirmed = await db.DocumentReviews.AsNoTracking().SingleAsync(item => item.ModelId == id);
        Assert.Equal(DocumentReviewStatus.Confirmed, confirmed.Status);
        Assert.NotNull(confirmed.ApprovalsStartedAt);
    }

    [Fact]
    public async Task A_billing_sheet_can_be_corrected_while_in_review_but_not_while_waiting_for_approval()
    {
        var preparer = await Person("Preparer", PermissionKeys.CanEditBillingSheet);
        var reviewer = await Person("Reviewer", PermissionKeys.CanReviewBillingSheet);
        var id = await Record(ReviewDocumentTypes.BillingSheet, preparer);
        // Approval stages exist, so the sheet stays unapproved after the review is confirmed.
        db.Approvals.Add(new DOMAIN.Entities.Approvals.Approval { Id = Guid.NewGuid(), ItemType = nameof(BillingSheet),
            ApprovalStages = [new() { Id = Guid.NewGuid(), Order = 1, Required = true, UserId = reviewer }] });
        await db.SaveChangesAsync();
        await Configure(ReviewDocumentTypes.BillingSheet, reviewer);
        await Approvals().CreateInitialApprovalsAsync(ReviewDocumentTypes.BillingSheet, id, preparer);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSingleton(db);
        services.AddAutoMapper(_ => { }, typeof(APP.Mapper.OryxMapper));
        var mapper = services.BuildServiceProvider().GetRequiredService<AutoMapper.IMapper>();
        var procurement = new ProcurementRepository(db, mapper, null!, null!, Approvals(), null!, null!);

        Assert.True((await procurement.UpdateBillingSheet(new UpdateBillingSheetRequest { Code = "BS-1A" }, id, reviewer)).IsSuccess);
        Assert.Contains(await db.DocumentReviewEvents.AsNoTracking().ToListAsync(), item => item.Action == DocumentReviewAction.Edited && item.ActorId == reviewer);
        Assert.True((await Service().Confirm(ReviewDocumentTypes.BillingSheet, id, new(), reviewer)).IsSuccess);
        db.ChangeTracker.Clear();
        Assert.Equal("BillingSheet.Pending", (await procurement.UpdateBillingSheet(new UpdateBillingSheetRequest { Code = "BS-1B" }, id, reviewer)).Errors[0].Code);
    }

    [Fact]
    public async Task A_record_submitted_again_after_its_review_was_confirmed_is_reviewed_again()
    {
        // A packing list edited after its check restarts its approvals; the edit is checked as a new submission.
        var preparer = await Person("Preparer", PermissionKeys.CanGeneratePackingList);
        var reviewer = await Person("Reviewer", PermissionKeys.CanReviewPackingList, PermissionKeys.CanViewPackingList);
        var id = await Record(ReviewDocumentTypes.PackingList, preparer);
        await Configure(ReviewDocumentTypes.PackingList, reviewer);
        await Approvals().CreateInitialApprovalsAsync(ReviewDocumentTypes.PackingList, id, preparer);
        Assert.True((await Service().Confirm(ReviewDocumentTypes.PackingList, id, new(), reviewer)).IsSuccess);
        await Approvals().CreateInitialApprovalsAsync(ReviewDocumentTypes.PackingList, id, preparer);
        var reviews = await db.DocumentReviews.AsNoTracking().Where(item => item.ModelId == id).OrderBy(item => item.Submission).ToListAsync();
        Assert.Equal([DocumentReviewStatus.Confirmed, DocumentReviewStatus.AwaitingReview], reviews.Select(item => item.Status));
        Assert.Equal(2, reviews[1].Submission);
        // The print keeps the earlier check until the new submission is confirmed.
        var state = (await Service().GetState(ReviewDocumentTypes.PackingList, id, reviewer)).Value;
        Assert.Equal(reviewer, state.Signatures.CheckedBy?.Id);
    }
}

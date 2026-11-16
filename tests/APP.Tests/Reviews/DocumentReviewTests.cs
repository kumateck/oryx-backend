using System.Reflection;
using API.Authorization;
using API.Controllers;
using APP.Mapper;
using APP.Repository;
using APP.Services.Reviews;
using APP.Tests.Repository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Requisitions.Request;
using DOMAIN.Entities.Reviews;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using INFRASTRUCTURE.Migrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

public class DocumentReviewTests
{
    private const string PR = ReviewDocumentTypes.PurchaseRequisition;
    private readonly ApplicationDbContext db = new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new NoUser());
    private int claimId;

    private ApprovalRepository Approvals() => new(db, null!, null!, null!, NullLogger<ApprovalRepository>.Instance, null!,
        new NoOpProductionActivityStepEventPublisher());

    private DocumentReviewService Service() => new(db, Approvals(), Procurement());

    private ProcurementRepository Procurement()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSingleton(db);
        services.AddAutoMapper(_ => { }, typeof(OryxMapper));
        var mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();
        return new ProcurementRepository(db, mapper, null!, null!, Approvals(), null!, null!);
    }

    private async Task<Guid> Person(string name, params string[] keys)
    {
        var id = Guid.NewGuid();
        var role = new Role { Id = Guid.NewGuid(), Name = name + " role" };
        db.Users.Add(new User { Id = id, UserName = name, FirstName = name, LastName = "Tester", Signature = name + ".png" });
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

    private async Task<Guid> RoleOf(Guid user) => await db.UserRoles.Where(item => item.UserId == user).Select(item => item.RoleId).SingleAsync();

    private async Task Configure(string itemType, params Guid[] users)
    {
        db.ReviewConfigurations.Add(new ReviewConfiguration
        {
            Id = Guid.NewGuid(), ItemType = itemType,
            Assignees = users.Select(user => new ReviewConfigurationAssignee { Id = Guid.NewGuid(), UserId = user }).ToList(),
        });
        await db.SaveChangesAsync();
    }

    private async Task<Guid> Requisition(Guid preparer, MaterialKind kind = MaterialKind.Raw)
    {
        var department = new Department { Id = Guid.NewGuid(), Name = "Production" };
        var material = new Material { Id = Guid.NewGuid(), Name = "Lactose", Code = "RM-1", Kind = kind };
        var requisition = new Requisition
        {
            Id = Guid.NewGuid(), Code = "PR-1", RequisitionType = RequisitionType.Purchase, DepartmentId = department.Id,
            RequestedById = preparer, CreatedById = preparer, Status = RequestStatus.New,
            Items = [new RequisitionItem { Id = Guid.NewGuid(), MaterialId = material.Id, Quantity = 10 }],
        };
        db.AddRange(department, material, requisition);
        await db.SaveChangesAsync();
        return requisition.Id;
    }

    private async Task<DocumentReview> Review(Guid id) =>
        await db.DocumentReviews.AsNoTracking().Include(item => item.Events).SingleAsync(item => item.ModelId == id);

    private static DocumentReviewActionRequest Say(string comment = null) => new() { Comments = comment };

    [Fact]
    public async Task Without_a_configuration_approvals_start_as_before()
    {
        var preparer = await Person("Preparer");
        var id = await Requisition(preparer);
        await Approvals().CreateInitialApprovalsAsync(PR, id, preparer);
        Assert.False(await db.DocumentReviews.AnyAsync());
        Assert.True((await db.Requisitions.AsNoTracking().SingleAsync(item => item.Id == id)).Approved);
    }

    [Fact]
    public async Task A_configured_type_is_held_for_review_until_confirmed_then_approvals_start()
    {
        var preparer = await Person("Preparer");
        var reviewer = await Person("Reviewer", PermissionKeys.CanReviewRawMaterialPurchaseRequisition);
        await Configure(ReviewDocumentTypes.PurchaseRequisition, reviewer);
        var id = await Requisition(preparer);

        await Approvals().CreateInitialApprovalsAsync(PR, id, preparer);
        await Approvals().CreateInitialApprovalsAsync(PR, id, preparer);
        var held = await Review(id);
        Assert.Equal(DocumentReviewStatus.AwaitingReview, held.Status);
        Assert.Equal(preparer, held.PreparedById);
        Assert.Single(held.Events);
        Assert.False((await db.Requisitions.AsNoTracking().SingleAsync(item => item.Id == id)).Approved);

        var result = await Service().Confirm(PR, id, Say("All good"), reviewer);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(error => error.Description)));
        var confirmed = await Review(id);
        Assert.Equal(DocumentReviewStatus.Confirmed, confirmed.Status);
        Assert.Equal(reviewer, confirmed.ReviewedById);
        Assert.NotNull(confirmed.ApprovalsStartedAt);
        Assert.Equal("All good", confirmed.Comments);
        Assert.True((await db.Requisitions.AsNoTracking().SingleAsync(item => item.Id == id)).Approved);
    }

    [Fact]
    public async Task Only_named_reviewers_or_the_preparer_with_the_kind_key_may_confirm()
    {
        var preparer = await Person("Preparer", PermissionKeys.CanReviewRawMaterialPurchaseRequisition);
        var reviewer = await Person("Reviewer", PermissionKeys.CanReviewRawMaterialPurchaseRequisition);
        var outsider = await Person("Outsider", PermissionKeys.CanReviewRawMaterialPurchaseRequisition);
        var wrongKind = await Person("Packaging", PermissionKeys.CanReviewPackageMaterialPurchaseRequisition);
        await Configure(ReviewDocumentTypes.PurchaseRequisition, reviewer, wrongKind);
        var id = await Requisition(preparer);
        await Approvals().CreateInitialApprovalsAsync(PR, id, preparer);

        Assert.Equal(ErrorType.Forbidden, (await Service().Confirm(PR, id, Say(), outsider)).Errors[0].Type);
        Assert.Equal(ErrorType.Forbidden, (await Service().Confirm(PR, id, Say(), wrongKind)).Errors[0].Type);
        // The preparer may confirm their own record, so work is not stuck when no reviewer is free.
        Assert.True((await Service().Confirm(PR, id, Say(), preparer)).IsSuccess);
        Assert.Equal(preparer, (await Review(id)).ReviewedById);
    }

    [Fact]
    public async Task A_role_named_in_the_configuration_may_review()
    {
        var preparer = await Person("Preparer");
        var reviewer = await Person("Reviewer", PermissionKeys.CanReviewRawMaterialPurchaseRequisition);
        db.ReviewConfigurations.Add(new ReviewConfiguration
        {
            Id = Guid.NewGuid(), ItemType = ReviewDocumentTypes.PurchaseRequisition,
            Assignees = [new ReviewConfigurationAssignee { Id = Guid.NewGuid(), RoleId = await RoleOf(reviewer) }],
        });
        await db.SaveChangesAsync();
        var id = await Requisition(preparer);
        await Approvals().CreateInitialApprovalsAsync(PR, id, preparer);
        Assert.True((await Service().Confirm(PR, id, Say(), reviewer)).IsSuccess);
    }

    [Fact]
    public async Task Return_needs_a_comment_and_the_preparer_corrects_and_resubmits()
    {
        var preparer = await Person("Preparer", PermissionKeys.CanEditRawMaterialPurchaseRequisition);
        var reviewer = await Person("Reviewer", PermissionKeys.CanReviewRawMaterialPurchaseRequisition, PermissionKeys.CanReturnRawMaterialPurchaseRequisition);
        await Configure(ReviewDocumentTypes.PurchaseRequisition, reviewer);
        var id = await Requisition(preparer);
        await Approvals().CreateInitialApprovalsAsync(PR, id, preparer);

        Assert.Equal(ErrorType.Validation, (await Service().Return(PR, id, Say(" "), reviewer)).Errors[0].Type);
        Assert.Equal(ErrorType.Forbidden, (await Service().Return(PR, id, Say("Fix"), preparer)).Errors[0].Type);
        Assert.True((await Service().Return(PR, id, Say("Quantity should be 12"), reviewer)).IsSuccess);
        Assert.Equal(DocumentReviewStatus.Returned, (await Review(id)).Status);
        Assert.Equal(ErrorType.Conflict, (await Service().Confirm(PR, id, Say(), reviewer)).Errors[0].Type);

        var material = await db.RequisitionItems.AsNoTracking().Select(item => item.MaterialId).FirstAsync();
        var corrected = await PurchaseRequisitionCorrection.ApplyAsync(db, id, new CreateRequisitionRequest
            { RequisitionType = RequisitionType.Purchase, Items = [new CreateRequisitionItemRequest { MaterialId = material, Quantity = 12 }] }, preparer);
        Assert.True(corrected.IsSuccess, string.Join("; ", corrected.Errors.Select(error => error.Description)));
        Assert.Equal(12, (await db.RequisitionItems.AsNoTracking().SingleAsync(item => item.RequisitionId == id && item.DeletedAt == null)).Quantity);
        Assert.Equal(1, (await db.Requisitions.AsNoTracking().SingleAsync(item => item.Id == id)).ContentRevision);

        Assert.True((await Service().Resubmit(PR, id, Say(), preparer)).IsSuccess);
        var review = await Review(id);
        Assert.Equal(DocumentReviewStatus.AwaitingReview, review.Status);
        Assert.Equal([DocumentReviewAction.Submitted, DocumentReviewAction.Returned, DocumentReviewAction.Edited, DocumentReviewAction.Resubmitted],
            review.Events.OrderBy(item => item.At).Select(item => item.Action));
        Assert.True((await Service().Confirm(PR, id, Say(), reviewer)).IsSuccess);
    }

    [Fact]
    public async Task A_requisition_is_corrected_only_in_review_and_with_its_kind_key()
    {
        var preparer = await Person("Preparer", PermissionKeys.CanEditPackageMaterialPurchaseRequisition);
        var reviewer = await Person("Reviewer", PermissionKeys.CanReviewRawMaterialPurchaseRequisition, PermissionKeys.CanEditRawMaterialPurchaseRequisition);
        var id = await Requisition(preparer);
        var material = await db.RequisitionItems.AsNoTracking().Select(item => item.MaterialId).FirstAsync();
        var request = new CreateRequisitionRequest { RequisitionType = RequisitionType.Purchase, Items = [new() { MaterialId = material, Quantity = 3 }] };

        Assert.Equal(ErrorType.Conflict, (await PurchaseRequisitionCorrection.ApplyAsync(db, id, request, reviewer)).Errors[0].Type);
        await Configure(ReviewDocumentTypes.PurchaseRequisition, reviewer);
        await Approvals().CreateInitialApprovalsAsync(PR, id, preparer);
        Assert.Equal(ErrorType.Forbidden, (await PurchaseRequisitionCorrection.ApplyAsync(db, id, request, preparer)).Errors[0].Type);
        // The reviewer may correct it directly.
        Assert.True((await PurchaseRequisitionCorrection.ApplyAsync(db, id, request, reviewer)).IsSuccess);
        Assert.Contains((await Review(id)).Events, item => item.Action == DocumentReviewAction.Edited && item.ActorId == reviewer);
    }

    [Fact]
    public async Task A_stale_page_cannot_act_on_a_newer_state()
    {
        var preparer = await Person("Preparer");
        var reviewer = await Person("Reviewer", PermissionKeys.CanReviewRawMaterialPurchaseRequisition);
        await Configure(ReviewDocumentTypes.PurchaseRequisition, reviewer);
        var id = await Requisition(preparer);
        await Approvals().CreateInitialApprovalsAsync(PR, id, preparer);
        var version = (await Review(id)).Version;
        Assert.Equal(ErrorType.Conflict, (await Service().Confirm(PR, id, new() { Version = version + 1 }, reviewer)).Errors[0].Type);
        Assert.True((await Service().Confirm(PR, id, new() { Version = version }, reviewer)).IsSuccess);
    }

    [Fact]
    public async Task The_stored_kind_decides_the_model_type()
    {
        var preparer = await Person("Preparer");
        var id = await Requisition(preparer);
        Assert.Null(await ReviewDocumentTypes.LoadAsync(db, ReviewDocumentTypes.RawStockRequisition, id));
        Assert.Equal(ErrorType.NotFound, (await Service().Confirm(ReviewDocumentTypes.RawStockRequisition, id, Say(), preparer)).Errors[0].Type);
    }

    [Fact]
    public async Task State_needs_the_view_key_and_shows_signatures_and_actions()
    {
        var preparer = await Person("Preparer");
        var reviewer = await Person("Reviewer", PermissionKeys.CanViewRawMaterialRequisitionsForProduction,
            PermissionKeys.CanReviewRawMaterialPurchaseRequisition, PermissionKeys.CanReturnRawMaterialPurchaseRequisition);
        await Configure(ReviewDocumentTypes.PurchaseRequisition, reviewer);
        var id = await Requisition(preparer);
        await Approvals().CreateInitialApprovalsAsync(PR, id, preparer);

        Assert.Equal(ErrorType.Forbidden, (await Service().GetState(PR, id, preparer)).Errors[0].Type);
        var before = (await Service().GetState(PR, id, reviewer)).Value;
        Assert.True(before.ReviewRequired);
        Assert.True(before.CanConfirm);
        Assert.True(before.CanReturn);
        Assert.Equal("Preparer Tester", before.Signatures.PreparedBy.Name);
        Assert.Null(before.Signatures.CheckedBy);

        await Service().Confirm(PR, id, Say(), reviewer);
        var after = (await Service().GetState(PR, id, reviewer)).Value;
        Assert.False(after.CanConfirm);
        Assert.Equal("Reviewer Tester", after.Signatures.CheckedBy.Name);
        Assert.Equal("Reviewer.png", after.Signatures.CheckedBy.Signature);
        Assert.Equal(DocumentReviewAction.Confirmed, after.Review.Events.Last().Action);
    }

    [Fact]
    public async Task Pending_lists_awaiting_reviews_for_reviewers_and_returned_ones_for_their_preparer()
    {
        var preparer = await Person("Preparer", PermissionKeys.CanViewPendingReviews, PermissionKeys.CanViewRawMaterialRequisitionsForProduction,
            PermissionKeys.CanEditRawMaterialPurchaseRequisition);
        var reviewer = await Person("Reviewer", PermissionKeys.CanViewPendingReviews, PermissionKeys.CanViewRawMaterialRequisitionsForProduction,
            PermissionKeys.CanReviewRawMaterialPurchaseRequisition, PermissionKeys.CanReturnRawMaterialPurchaseRequisition);
        var noKey = await Person("No key", PermissionKeys.CanViewRawMaterialRequisitionsForProduction);
        await Configure(ReviewDocumentTypes.PurchaseRequisition, reviewer);
        var id = await Requisition(preparer);
        await Approvals().CreateInitialApprovalsAsync(PR, id, preparer);

        Assert.Equal(ErrorType.Forbidden, (await Service().GetPending(noKey, 1, 20)).Errors[0].Type);
        Assert.Equal(id, Assert.Single((await Service().GetPending(reviewer, 1, 20)).Value.Data).ModelId);
        Assert.Empty((await Service().GetPending(preparer, 1, 20)).Value.Data);
        await Service().Return(PR, id, Say("Fix the quantity"), reviewer);
        Assert.Empty((await Service().GetPending(reviewer, 1, 20)).Value.Data);
        var returned = Assert.Single((await Service().GetPending(preparer, 1, 20)).Value.Data);
        Assert.Equal("Fix the quantity", returned.Comments);
    }

    [Fact]
    public async Task Settings_accept_only_reviewable_types_with_one_role_or_user_each()
    {
        var admin = await Person("Admin", PermissionKeys.CanEditDocumentReviewSettings, PermissionKeys.CanViewDocumentReviewSettings,
            PermissionKeys.CanDeleteDocumentReviewSettings);
        var reviewer = await Person("Reviewer");
        var service = Service();
        Assert.Equal(ErrorType.Validation, (await service.SaveConfiguration(new() { ItemType = "LeaveRequest", Assignees = [new() { UserId = reviewer }] }, admin)).Errors[0].Type);
        Assert.Equal(ErrorType.Validation, (await service.SaveConfiguration(new() { ItemType = PR, Assignees = [] }, admin)).Errors[0].Type);
        Assert.Equal(ErrorType.Validation, (await service.SaveConfiguration(new() { ItemType = PR,
            Assignees = [new() { UserId = reviewer, RoleId = await RoleOf(reviewer) }] }, admin)).Errors[0].Type);
        Assert.Equal(ErrorType.Forbidden, (await service.SaveConfiguration(new() { ItemType = PR, Assignees = [new() { UserId = reviewer }] }, reviewer)).Errors[0].Type);
        Assert.True((await service.SaveConfiguration(new() { ItemType = PR, Assignees = [new() { UserId = reviewer }] }, admin)).IsSuccess);
        Assert.True((await service.SaveConfiguration(new() { ItemType = PR, Assignees = [new() { RoleId = await RoleOf(reviewer) }] }, admin)).IsSuccess);
        var saved = Assert.Single((await service.GetConfigurations(admin)).Value);
        Assert.NotNull(Assert.Single(saved.Assignees).Role);
        Assert.True((await service.DeleteConfiguration(PR, admin)).IsSuccess);
        Assert.Empty((await service.GetConfigurations(admin)).Value);
    }

    [Fact]
    public async Task Checking_a_purchase_order_awaiting_check_confirms_its_review_and_records_the_checker()
    {
        var preparer = await Person("Buyer");
        var checker = await Person("Checker", PermissionKeys.CanCheckForeignPurchaseOrder, PermissionKeys.CanViewForeignPurchaseOrder);
        var outsider = await Person("Local checker", PermissionKeys.CanCheckLocalPurchaseOrder);
        var orderId = await Order(preparer, PurchaseOrderStatus.PendingCheck);

        Assert.Equal(ErrorType.Forbidden, (await Service().Confirm(ReviewDocumentTypes.PurchaseOrder, orderId, Say(), outsider)).Errors[0].Type);
        var result = await Service().Confirm(ReviewDocumentTypes.PurchaseOrder, orderId, Say(), checker);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(error => error.Description)));
        var review = await Review(orderId);
        Assert.Equal(DocumentReviewStatus.Confirmed, review.Status);
        Assert.Equal(checker, review.ReviewedById);
        Assert.NotEqual(PurchaseOrderStatus.PendingCheck, (await db.PurchaseOrders.AsNoTracking().SingleAsync(item => item.Id == orderId)).Status);
        var state = (await Service().GetState(ReviewDocumentTypes.PurchaseOrder, orderId, checker)).Value;
        Assert.Equal("Checker Tester", state.Signatures.CheckedBy.Name);
        Assert.Equal("Buyer Tester", state.Signatures.PreparedBy.Name);
    }

    [Fact]
    public async Task A_returned_purchase_order_goes_back_to_review_when_finalised_again()
    {
        var preparer = await Person("Buyer");
        var checker = await Person("Checker", PermissionKeys.CanCheckForeignPurchaseOrder, PermissionKeys.CanReturnForeignPurchaseOrderCheck);
        var orderId = await Order(preparer, PurchaseOrderStatus.PendingCheck);
        Assert.True((await Service().Return(ReviewDocumentTypes.PurchaseOrder, orderId, Say("Wrong freight"), checker)).IsSuccess);
        Assert.Equal(DocumentReviewStatus.Returned, (await Review(orderId)).Status);
        Assert.Equal(ErrorType.Conflict, (await Service().Confirm(ReviewDocumentTypes.PurchaseOrder, orderId, Say(), checker)).Errors[0].Type);
        await DocumentReviewGate.SubmitAsync(db, ReviewDocumentTypes.PurchaseOrder, orderId, preparer);
        Assert.Equal(DocumentReviewStatus.AwaitingReview, (await Review(orderId)).Status);
        Assert.True((await Service().Confirm(ReviewDocumentTypes.PurchaseOrder, orderId, Say(), checker)).IsSuccess);
    }

    private async Task<Guid> Order(Guid preparer, PurchaseOrderStatus status)
    {
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Supplier", Type = SupplierType.Foreign };
        var material = new Material { Id = Guid.NewGuid(), Name = "Amoxicillin" };
        var kg = new UnitOfMeasure { Id = Guid.NewGuid(), Symbol = "kg" };
        var source = new SourceRequisition { Id = Guid.NewGuid() };
        var order = new PurchaseOrder
        {
            Id = Guid.NewGuid(), Code = "EPRC-1", SupplierId = supplier.Id, SourceRequisitionId = source.Id, Status = status, CreatedById = preparer,
            Items = [new PurchaseOrderItem { Id = Guid.NewGuid(), MaterialId = material.Id, UoMId = kg.Id, Quantity = 5, Price = 2, PriceUoM = "kg" }],
        };
        db.AddRange(supplier, material, kg, source, order);
        await db.SaveChangesAsync();
        // The context stamps the creator from the signed-in user when a row is added.
        order.CreatedById = preparer;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return order.Id;
    }

    [Theory]
    [InlineData(nameof(DocumentReviewController.GetTypes), PermissionKeys.CanViewDocumentReviewSettings)]
    [InlineData(nameof(DocumentReviewController.GetSettings), PermissionKeys.CanViewDocumentReviewSettings)]
    [InlineData(nameof(DocumentReviewController.SaveSettings), PermissionKeys.CanEditDocumentReviewSettings)]
    [InlineData(nameof(DocumentReviewController.DeleteSettings), PermissionKeys.CanDeleteDocumentReviewSettings)]
    [InlineData(nameof(DocumentReviewController.GetPending), PermissionKeys.CanViewPendingReviews)]
    public void Settings_and_pending_endpoints_carry_their_own_key(string action, string key)
    {
        var method = typeof(DocumentReviewController).GetMethod(action)!;
        Assert.Equal(key, Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());
    }

    [Theory]
    [InlineData(nameof(DocumentReviewController.GetState))]
    [InlineData(nameof(DocumentReviewController.Confirm))]
    [InlineData(nameof(DocumentReviewController.Return))]
    [InlineData(nameof(DocumentReviewController.Resubmit))]
    public void Record_endpoints_are_guarded_and_check_the_stored_kind_in_the_service(string action)
    {
        var method = typeof(DocumentReviewController).GetMethod(action)!;
        Assert.NotNull(method.GetCustomAttribute<AnyPermissionAttribute>());
        Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());
        Assert.NotNull(typeof(RequisitionController).GetMethod(nameof(RequisitionController.CorrectPurchaseRequisition))!
            .GetCustomAttribute<AnyPermissionAttribute>());
    }

    [Fact]
    public void Every_new_key_is_registered_once()
    {
        string[] keys =
        [
            PermissionKeys.CanViewDocumentReviewSettings, PermissionKeys.CanEditDocumentReviewSettings, PermissionKeys.CanDeleteDocumentReviewSettings,
            PermissionKeys.CanViewPendingReviews, PermissionKeys.CanReturnForeignPurchaseOrderCheck, PermissionKeys.CanReturnLocalPurchaseOrderCheck,
            PermissionKeys.CanEditRawMaterialPurchaseRequisition, PermissionKeys.CanEditPackageMaterialPurchaseRequisition,
            PermissionKeys.CanReviewRawMaterialPurchaseRequisition, PermissionKeys.CanReviewPackageMaterialPurchaseRequisition,
            PermissionKeys.CanReturnRawMaterialPurchaseRequisition, PermissionKeys.CanReturnPackageMaterialPurchaseRequisition,
            PermissionKeys.CanReviewRawMaterialStockRequisition, PermissionKeys.CanReviewPackageMaterialStockRequisition,
            PermissionKeys.CanReturnRawMaterialStockRequisition, PermissionKeys.CanReturnPackageMaterialStockRequisition,
        ];
        var registered = PermissionUtils.GeneratePermissions().Select(item => item.Key).ToList();
        foreach (var key in keys) Assert.Single(registered, item => item == key);
    }

    [Fact]
    public void The_grant_migration_keeps_existing_roles_working_and_grants_review_keys_to_nobody()
    {
        var pairs = typeof(GrantDocumentReviewKeys).GetField("Pairs", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null) as string;
        Assert.Contains("('CanCheckForeignPurchaseOrder', 'CanReturnForeignPurchaseOrderCheck')", pairs);
        Assert.Contains("('CanEditApprovalWorkflow', 'CanEditDocumentReviewSettings')", pairs);
        Assert.Contains("('CanCreateRawMaterialRequisitions', 'CanEditRawMaterialPurchaseRequisition')", pairs);
        Assert.DoesNotContain("CanReviewRawMaterialPurchaseRequisition", pairs);
        Assert.DoesNotContain("CanReviewRawMaterialStockRequisition", pairs);
    }
}

using System.Reflection;
using API.Controllers;
using APP.Repository;
using APP.Services.ApprovedVendorLists;
using APP.Services.Reviews;
using APP.Tests.Repository;
using APP.Utils;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.ApprovedVendorLists;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.PurchaseOrders.Request;
using DOMAIN.Entities.Reviews;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.ApprovedVendorLists;

internal sealed class AvlActor : ICurrentUserService
{
    public Guid? Current { get; set; }
    public Guid? UserId => Current;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class ApprovedVendorListTests
{
    private readonly AvlActor actor = new();
    private readonly ApplicationDbContext db;
    private int claimId;
    private static readonly DateTime Today = DateTime.UtcNow.Date;

    public ApprovedVendorListTests()
    {
        db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, actor);
    }

    private ApprovalRepository Approvals() => new(db, null!, null!, null!, NullLogger<ApprovalRepository>.Instance, null!,
        new NoOpProductionActivityStepEventPublisher());
    private ApprovedVendorListService Lists() => new(db, Approvals());
    private DocumentReviewService Reviews() => new(db, Approvals(), null!);

    private async Task<(Guid User, Guid Role)> Person(string name, params string[] keys)
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
        return (id, role.Id);
    }

    private sealed record Fixture(Guid Material, Guid OtherMaterial, Guid Manufacturer, Guid Supplier, Guid OtherSupplier);

    /// <summary>Albendazole from K. A. Malle through Indosol (mapped), and a second supplier and an unlisted material.</summary>
    private async Task<Fixture> Masters()
    {
        var material = new Material { Id = Guid.NewGuid(), Code = "RMA002", Name = "Albendazole USP (Micronized)", Kind = MaterialKind.Raw };
        var other = new Material { Id = Guid.NewGuid(), Code = "RMA010", Name = "Paracetamol", Kind = MaterialKind.Raw };
        var manufacturer = new Manufacturer { Id = Guid.NewGuid(), Name = "K. A. Malle Pharmaceuticals Ltd", Address = "Ankleshwar, Gujarat, India" };
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Indosol Export" };
        var otherSupplier = new Supplier { Id = Guid.NewGuid(), Name = "Other Traders" };
        db.Materials.AddRange(material, other);
        db.Manufacturers.Add(manufacturer);
        db.Suppliers.AddRange(supplier, otherSupplier);
        db.ManufacturerMaterials.Add(new ManufacturerMaterial { Id = Guid.NewGuid(), ManufacturerId = manufacturer.Id, MaterialId = material.Id,
            QuantityPerPackOption = new QuantityPerPackOption { Type = QuantityType.Single, Values = [25m] } });
        db.SupplierManufacturers.Add(new SupplierManufacturer { Id = Guid.NewGuid(), SupplierId = supplier.Id, ManufacturerId = manufacturer.Id, MaterialId = material.Id });
        await db.SaveChangesAsync();
        return new Fixture(material.Id, other.Id, manufacturer.Id, supplier.Id, otherSupplier.Id);
    }

    private static SaveApprovedVendorListRequest First(Fixture f, DateTime validity) => new()
    {
        AnnexureNumber = "QAD/SOP/007/A03-06", RevisionNumber = 17, SupersedesRevision = 16,
        Lines = [new SaveApprovedVendorListLineRequest { MaterialId = f.Material, ManufacturerId = f.Manufacturer, SupplierId = f.Supplier,
            QuantityPerPack = 25m, ApprovalDate = new DateTime(2024, 6, 1), ValidityDate = validity }],
    };

    private async Task<Guid> ApprovedList(Fixture f, DateTime validity)
    {
        var preparer = await Person("Officer", PermissionKeys.CanCreateApprovedVendorList);
        var checker = await Person("Deputy", PermissionKeys.CanReviewApprovedVendorList);
        actor.Current = preparer.User;
        var id = (await Lists().Create(First(f, validity), preparer.User)).Value;
        actor.Current = checker.User;
        // No approval workflow configured: confirming the review approves it automatically.
        Assert.True((await Reviews().Confirm(ReviewDocumentTypes.ApprovedVendorList, id, new(), checker.User)).IsSuccess);
        db.ChangeTracker.Clear();
        return id;
    }

    private static string Policy(string action)
    {
        var method = typeof(ApprovedVendorListController).GetMethod(action) ?? throw new InvalidOperationException(action);
        Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());
        return Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy;
    }

    [Fact]
    public void Every_endpoint_names_its_own_key_and_none_is_anonymous()
    {
        Assert.Equal(PermissionKeys.CanViewApprovedVendorList, Policy(nameof(ApprovedVendorListController.GetApprovedVendorLists)));
        Assert.Equal(PermissionKeys.CanViewApprovedVendorList, Policy(nameof(ApprovedVendorListController.GetApprovedVendorList)));
        Assert.Equal(PermissionKeys.CanViewApprovedVendorList, Policy(nameof(ApprovedVendorListController.GetApprovedVendorMappings)));
        Assert.Equal(PermissionKeys.CanPrintApprovedVendorList, Policy(nameof(ApprovedVendorListController.GetApprovedVendorListPrint)));
        Assert.Equal(PermissionKeys.CanViewApprovedVendorEligibility, Policy(nameof(ApprovedVendorListController.GetApprovedVendorEligibility)));
        Assert.Equal(PermissionKeys.CanCreateApprovedVendorList, Policy(nameof(ApprovedVendorListController.CreateApprovedVendorList)));
        Assert.Equal(PermissionKeys.CanReviseApprovedVendorList, Policy(nameof(ApprovedVendorListController.ReviseApprovedVendorList)));
        Assert.Equal(PermissionKeys.CanEditApprovedVendorList, Policy(nameof(ApprovedVendorListController.UpdateApprovedVendorList)));
        Assert.Equal(PermissionKeys.CanApproveApprovedVendorList, ApprovalController.DecisionKey(ReviewDocumentTypes.ApprovedVendorList, approve: true));
        Assert.Equal(PermissionKeys.CanRejectApprovedVendorList, ApprovalController.DecisionKey(ReviewDocumentTypes.ApprovedVendorList, approve: false));
    }

    [Fact]
    public void Each_key_is_registered_exactly_once()
    {
        var keys = PermissionUtils.GeneratePermissions().Select(item => item.Key).ToList();
        foreach (var key in new[] { PermissionKeys.CanViewApprovedVendorList, PermissionKeys.CanCreateApprovedVendorList, PermissionKeys.CanEditApprovedVendorList,
                     PermissionKeys.CanReviseApprovedVendorList, PermissionKeys.CanReviewApprovedVendorList, PermissionKeys.CanReturnApprovedVendorList,
                     PermissionKeys.CanApproveApprovedVendorList, PermissionKeys.CanRejectApprovedVendorList, PermissionKeys.CanPrintApprovedVendorList,
                     PermissionKeys.CanViewApprovedVendorEligibility })
            Assert.Single(keys, key);
    }

    [Fact]
    public async Task Create_is_refused_without_its_key_and_opens_a_review_with_it()
    {
        var f = await Masters();
        var outsider = await Person("Outsider", PermissionKeys.CanViewApprovedVendorList);
        actor.Current = outsider.User;
        Assert.Equal(ErrorType.Forbidden, (await Lists().Create(First(f, Today), outsider.User)).Errors[0].Type);
        Assert.Empty(db.ApprovedVendorLists);

        var preparer = await Person("Officer", PermissionKeys.CanCreateApprovedVendorList);
        actor.Current = preparer.User;
        var created = await Lists().Create(First(f, Today), preparer.User);
        Assert.True(created.IsSuccess);
        var review = await db.DocumentReviews.AsNoTracking().SingleAsync(item => item.ModelId == created.Value);
        Assert.Equal(DocumentReviewStatus.AwaitingReview, review.Status);
        Assert.Equal(preparer.User, review.PreparedById);
        var list = (await Lists().Get(created.Value)).Value;
        Assert.Equal(ApprovedVendorListStatus.Pending, list.Status);
        Assert.Equal(DocumentReviewStatus.AwaitingReview, list.ReviewStatus);
        Assert.Equal(16, list.SupersedesRevision);
        Assert.Equal("Officer Tester", list.PreparedBySignatory.Name);
        Assert.Null(list.CheckedBy);
    }

    [Fact]
    public async Task Review_return_correction_and_workflow_approval_sign_checked_and_approved_by()
    {
        var f = await Masters();
        var preparer = await Person("Officer", PermissionKeys.CanCreateApprovedVendorList, PermissionKeys.CanEditApprovedVendorList);
        var checker = await Person("Deputy", PermissionKeys.CanReviewApprovedVendorList, PermissionKeys.CanReturnApprovedVendorList);
        var manager = await Person("Manager", PermissionKeys.CanApproveApprovedVendorList);
        db.Approvals.Add(new Approval { Id = Guid.NewGuid(), ItemType = ReviewDocumentTypes.ApprovedVendorList,
            ApprovalStages = [new ApprovalStage { Id = Guid.NewGuid(), RoleId = manager.Role, Order = 1, Required = true }] });
        await db.SaveChangesAsync();

        actor.Current = preparer.User;
        var id = (await Lists().Create(First(f, Today.AddYears(2)), preparer.User)).Value;
        actor.Current = checker.User;
        Assert.True((await Reviews().Return(ReviewDocumentTypes.ApprovedVendorList, id, new() { Comments = "Wrong pack size" }, checker.User)).IsSuccess);

        actor.Current = preparer.User;
        var correction = First(f, Today.AddYears(2));
        correction.Lines[0].QuantityPerPack = 50m;
        Assert.True((await Lists().Update(id, correction, preparer.User)).IsSuccess);
        Assert.True((await Reviews().Resubmit(ReviewDocumentTypes.ApprovedVendorList, id, new(), preparer.User)).IsSuccess);

        actor.Current = checker.User;
        Assert.True((await Reviews().Confirm(ReviewDocumentTypes.ApprovedVendorList, id, new(), checker.User)).IsSuccess);
        Assert.Equal(ApprovedVendorListStatus.Pending, (await db.ApprovedVendorLists.AsNoTracking().SingleAsync()).Status);

        actor.Current = manager.User;
        Assert.True((await Approvals().ApproveItem(ReviewDocumentTypes.ApprovedVendorList, id, manager.User, [manager.Role], "OK", null)).IsSuccess);
        db.ChangeTracker.Clear();
        var list = (await Lists().Get(id)).Value;
        Assert.Equal(ApprovedVendorListStatus.Approved, list.Status);
        Assert.Equal(50m, Assert.Single(list.Lines).QuantityPerPack);
        Assert.Equal("Deputy Tester", list.CheckedBy.Name);
        Assert.Equal("Manager Tester", list.ApprovedBy.Name);
        // Approved lists are changed only by a revision.
        actor.Current = preparer.User;
        Assert.Equal(ErrorType.Conflict, (await Lists().Update(id, correction, preparer.User)).Errors[0].Type);
    }

    [Fact]
    public async Task A_revision_supersedes_the_approved_list_and_records_what_it_supersedes()
    {
        var f = await Masters();
        var first = await ApprovedList(f, Today.AddYears(1));
        var reviser = await Person("Reviser", PermissionKeys.CanReviseApprovedVendorList);
        var checker = await Person("Checker", PermissionKeys.CanReviewApprovedVendorList);
        actor.Current = reviser.User;
        var request = new ReviseApprovedVendorListRequest { AnnexureNumber = "QAD/SOP/007/A03-06", RevisionNumber = 17,
            Lines = First(f, Today.AddYears(3)).Lines };
        Assert.Equal(ErrorType.Validation, (await Lists().Revise(request, reviser.User)).Errors[0].Type);
        request.RevisionNumber = 18;
        var second = await Lists().Revise(request, reviser.User);
        Assert.True(second.IsSuccess);
        Assert.Equal(ErrorType.Conflict, (await Lists().Revise(request, reviser.User)).Errors[0].Type);

        actor.Current = checker.User;
        Assert.True((await Reviews().Confirm(ReviewDocumentTypes.ApprovedVendorList, second.Value, new(), checker.User)).IsSuccess);
        db.ChangeTracker.Clear();
        Assert.Equal(ApprovedVendorListStatus.Superseded, (await db.ApprovedVendorLists.SingleAsync(item => item.Id == first)).Status);
        var current = await db.ApprovedVendorLists.SingleAsync(item => item.Id == second.Value);
        Assert.Equal(ApprovedVendorListStatus.Approved, current.Status);
        Assert.Equal(17, current.SupersedesRevision);
        Assert.Equal(first, current.PreviousRevisionId);
    }

    [Fact]
    public async Task A_new_manufacturer_needs_the_create_key_and_is_mapped_to_the_material()
    {
        var f = await Masters();
        var preparer = await Person("Officer", PermissionKeys.CanCreateApprovedVendorList);
        actor.Current = preparer.User;
        var request = First(f, Today);
        request.Lines[0].ManufacturerId = null;
        request.Lines[0].NewManufacturer = new NewApprovedManufacturerRequest { Name = "New Pharma", Address = "Hyderabad" };
        Assert.Equal(ErrorType.Forbidden, (await Lists().Create(request, preparer.User)).Errors[0].Type);

        var creator = await Person("Creator", PermissionKeys.CanCreateApprovedVendorList, PermissionKeys.CanCreateManufacturer, PermissionKeys.CanUpdateVendorDetails);
        actor.Current = creator.User;
        db.ChangeTracker.Clear();
        Assert.True((await Lists().Create(request, creator.User)).IsSuccess);
        var manufacturer = await db.Manufacturers.SingleAsync(item => item.Name == "New Pharma");
        Assert.True(await db.ManufacturerMaterials.AnyAsync(item => item.ManufacturerId == manufacturer.Id && item.MaterialId == f.Material));
        Assert.True(await db.SupplierManufacturers.AnyAsync(item => item.ManufacturerId == manufacturer.Id && item.SupplierId == f.Supplier && item.MaterialId == f.Material));
    }

    [Fact]
    public async Task Mappings_offer_the_materials_manufacturers_and_their_suppliers()
    {
        var f = await Masters();
        var mapping = Assert.Single((await Lists().Mappings(f.Material)).Value);
        Assert.Equal(f.Manufacturer, mapping.ManufacturerId);
        Assert.Equal(25m, mapping.QuantityPerPack);
        Assert.Equal(f.Supplier, Assert.Single(mapping.Suppliers).SupplierId);
    }

    [Fact]
    public async Task Only_listed_materials_are_restricted_and_only_to_their_active_lines()
    {
        var f = await Masters();
        await ApprovedList(f, Today);
        Assert.True((await ApprovedVendorRules.CheckSuppliers(db, [(f.Material, f.Supplier)], Today)).IsSuccess);
        // An unlisted material is unrestricted.
        Assert.True((await ApprovedVendorRules.CheckSuppliers(db, [(f.OtherMaterial, f.OtherSupplier)], Today)).IsSuccess);
        var notListed = await ApprovedVendorRules.CheckSuppliers(db, [(f.Material, f.OtherSupplier)], Today);
        Assert.Equal("ApprovedVendorList.SupplierNotApproved", notListed.Errors[0].Code);
        Assert.Contains("RMA002", notListed.Errors[0].Description);
        // The line is valid through the end of its validity month, and lapses on the first day after it.
        var lapsesOn = ApprovedVendorRules.LapsesOn(Today);
        Assert.True((await ApprovedVendorRules.CheckSuppliers(db, [(f.Material, f.Supplier)], lapsesOn.AddDays(-1))).IsSuccess);
        Assert.Equal("ApprovedVendorList.SupplierLapsed",
            (await ApprovedVendorRules.CheckSuppliers(db, [(f.Material, f.Supplier)], lapsesOn)).Errors[0].Code);
        Assert.Equal("ApprovedVendorList.ManufacturerLapsed",
            (await ApprovedVendorRules.CheckManufacturers(db, [(f.Material, f.Manufacturer, f.Supplier)], lapsesOn)).Errors[0].Code);
        Assert.Equal("ApprovedVendorList.ManufacturerNotApproved",
            (await ApprovedVendorRules.CheckManufacturers(db, [(f.Material, f.Manufacturer, f.OtherSupplier)], Today)).Errors[0].Code);

        var eligibility = await ApprovedVendorRules.Eligibility(db, [f.Material, f.OtherMaterial], lapsesOn);
        Assert.True(eligibility[0].Restricted);
        Assert.Empty(eligibility[0].Approved);
        Assert.False(eligibility[1].Restricted);
    }

    [Fact]
    public async Task A_pending_list_restricts_nothing_until_it_is_approved()
    {
        var f = await Masters();
        var preparer = await Person("Officer", PermissionKeys.CanCreateApprovedVendorList);
        actor.Current = preparer.User;
        await Lists().Create(First(f, Today), preparer.User);
        Assert.True((await ApprovedVendorRules.CheckSuppliers(db, [(f.Material, f.OtherSupplier)], Today)).IsSuccess);
    }

    [Fact]
    public async Task Purchase_orders_and_invoices_refuse_a_supplier_or_manufacturer_off_the_list()
    {
        var f = await Masters();
        await ApprovedList(f, Today);
        var procurement = new ProcurementRepository(db, null!, null!, null!, null!, null!, null!);
        var order = await procurement.CreatePurchaseOrder(new CreatePurchaseOrderRequest
        {
            SupplierId = f.OtherSupplier,
            Items = [new CreatePurchaseOrderItemRequest { MaterialId = f.Material, UomId = Guid.NewGuid(), Quantity = 1, Price = 1, PriceUoM = "kg" }],
        }, Guid.NewGuid());
        Assert.Equal("ApprovedVendorList.SupplierNotApproved", order.Errors[0].Code);
        Assert.Empty(db.PurchaseOrders);

        var otherManufacturer = new Manufacturer { Id = Guid.NewGuid(), Name = "Unlisted Labs" };
        db.Manufacturers.Add(otherManufacturer);
        var purchaseOrder = new PurchaseOrder { Id = Guid.NewGuid(), Code = "PO-1", SupplierId = f.Supplier,
            Items = [new PurchaseOrderItem { Id = Guid.NewGuid(), MaterialId = f.Material, UoMId = Guid.NewGuid() }] };
        db.PurchaseOrders.Add(purchaseOrder);
        await db.SaveChangesAsync();
        var invoice = await procurement.CreatePurchaseOrderInvoice(new CreatePurchaseOrderInvoiceRequest
        {
            PurchaseOrderId = purchaseOrder.Id, BatchItems = [new CreateBatchItemRequest { BatchNumber = "B1", ManufacturerId = otherManufacturer.Id, Quantity = 1 }],
        }, Guid.NewGuid());
        Assert.Equal("ApprovedVendorList.ManufacturerNotApproved", invoice.Errors[0].Code);
        Assert.True((await ApprovedVendorRules.CheckOrderManufacturers(db, purchaseOrder.Id, [f.Manufacturer], Today)).IsSuccess);
    }
}

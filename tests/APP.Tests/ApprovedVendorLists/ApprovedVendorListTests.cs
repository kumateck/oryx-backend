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

public partial class ApprovedVendorListTests
{
    private readonly AvlActor actor = new();
    private readonly ApplicationDbContext db;
    private int claimId;
    private static readonly DateTime Today = DateTime.UtcNow.Date;
    /// <summary>A validity month still to come, so its line can be used today.</summary>
    private static readonly DateTime Valid = ApprovedVendorRules.Month(Today).AddMonths(2);

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

    /// <summary>A draft started and submitted by one person: it is then in review.</summary>
    private async Task<Guid> Submitted(SaveApprovedVendorListRequest request, params string[] extra)
    {
        var preparer = await Person("Officer" + Guid.NewGuid().ToString("N")[..6],
            [PermissionKeys.CanCreateApprovedVendorList, PermissionKeys.CanSubmitApprovedVendorList, .. extra]);
        actor.Current = preparer.User;
        var id = (await Lists().Create(request, preparer.User)).Value;
        var submitted = await Lists().Submit(id, new() { DraftVersion = 0 }, preparer.User);
        Assert.True(submitted.IsSuccess, submitted.IsFailure ? submitted.Errors[0].Description : null);
        return id;
    }

    private async Task<Guid> ApprovedList(Fixture f, DateTime validity)
    {
        var checker = await Person("Deputy", PermissionKeys.CanReviewApprovedVendorList);
        var id = await Submitted(First(f, validity));
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
        Assert.Equal(PermissionKeys.CanViewApprovedVendorList, Policy(nameof(ApprovedVendorListController.GetApprovedVendorOptions)));
        Assert.Equal(PermissionKeys.CanPrintApprovedVendorList, Policy(nameof(ApprovedVendorListController.GetApprovedVendorListPrint)));
        Assert.Equal(PermissionKeys.CanViewApprovedVendorEligibility, Policy(nameof(ApprovedVendorListController.GetApprovedVendorEligibility)));
        Assert.Equal(PermissionKeys.CanCreateApprovedVendorList, Policy(nameof(ApprovedVendorListController.CreateApprovedVendorList)));
        Assert.Equal(PermissionKeys.CanReviseApprovedVendorList, Policy(nameof(ApprovedVendorListController.ReviseApprovedVendorList)));
        Assert.Equal(PermissionKeys.CanEditApprovedVendorList, Policy(nameof(ApprovedVendorListController.UpdateApprovedVendorList)));
        Assert.Equal(PermissionKeys.CanEditApprovedVendorListDraft, Policy(nameof(ApprovedVendorListController.SaveApprovedVendorListDraft)));
        Assert.Equal(PermissionKeys.CanSubmitApprovedVendorList, Policy(nameof(ApprovedVendorListController.SubmitApprovedVendorList)));
        Assert.Equal(PermissionKeys.CanDeleteApprovedVendorList, Policy(nameof(ApprovedVendorListController.DeleteApprovedVendorList)));
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
                     PermissionKeys.CanViewApprovedVendorEligibility, PermissionKeys.CanEditApprovedVendorListDraft,
                     PermissionKeys.CanSubmitApprovedVendorList, PermissionKeys.CanDeleteApprovedVendorList })
            Assert.Single(keys, key);
    }

    [Fact]
    public async Task Create_starts_a_draft_that_restricts_nothing_and_opens_no_review()
    {
        var f = await Masters();
        var outsider = await Person("Outsider", PermissionKeys.CanViewApprovedVendorList);
        actor.Current = outsider.User;
        Assert.Equal(ErrorType.Forbidden, (await Lists().Create(First(f, Valid), outsider.User)).Errors[0].Type);
        Assert.Empty(db.ApprovedVendorLists);

        var preparer = await Person("Officer", PermissionKeys.CanCreateApprovedVendorList);
        actor.Current = preparer.User;
        var created = await Lists().Create(First(f, Valid), preparer.User);
        Assert.True(created.IsSuccess);
        Assert.Empty(db.DocumentReviews);
        var list = (await Lists().Get(created.Value)).Value;
        Assert.Equal(ApprovedVendorListStatus.Draft, list.Status);
        Assert.Equal(16, list.SupersedesRevision);
        Assert.Equal("Officer Tester", list.PreparedBySignatory.Name);
        // A draft is not a list of record: it restricts no supplier.
        Assert.True((await ApprovedVendorRules.CheckSuppliers(db, [(f.Material, f.OtherSupplier)], Today)).IsSuccess);
        // One list in preparation at a time.
        Assert.Equal(ErrorType.Conflict, (await Lists().Create(First(f, Valid), preparer.User)).Errors[0].Type);
    }

    [Fact]
    public async Task The_validity_month_must_come_after_the_approval_month()
    {
        var f = await Masters();
        var preparer = await Person("Officer", PermissionKeys.CanCreateApprovedVendorList);
        actor.Current = preparer.User;
        var refused = await Lists().Create(First(f, new DateTime(2024, 6, 15)), preparer.User);
        Assert.Equal("ApprovedVendorList.Dates", refused.Errors[0].Code);
    }

    [Fact]
    public async Task A_draft_is_saved_as_often_as_needed_adding_and_removing_lines_then_submitted_for_review()
    {
        var f = await Masters();
        var preparer = await Person("Officer", PermissionKeys.CanCreateApprovedVendorList);
        var editor = await Person("Editor", PermissionKeys.CanEditApprovedVendorListDraft);
        var submitter = await Person("Submitter", PermissionKeys.CanSubmitApprovedVendorList,
            PermissionKeys.CanUpdateManufacturerDetails, PermissionKeys.CanUpdateVendorDetails);
        actor.Current = preparer.User;
        var id = (await Lists().Create(First(f, Valid), preparer.User)).Value;

        // Without the draft edit key the draft cannot be changed.
        var draft = First(f, Valid);
        draft.Lines.Add(new SaveApprovedVendorListLineRequest { MaterialId = f.OtherMaterial, ManufacturerId = f.Manufacturer, SupplierId = f.OtherSupplier,
            QuantityPerPack = 50m, ApprovalDate = new DateTime(2024, 6, 1), ValidityDate = Valid });
        var request = new SaveApprovedVendorListDraftRequest { AnnexureNumber = "QAD/SOP/007/A03-06", RevisionNumber = 18, SupersedesRevision = 17, DraftVersion = 0, Lines = draft.Lines };
        Assert.Equal(ErrorType.Forbidden, (await Lists().SaveDraft(id, request, preparer.User)).Errors[0].Type);

        // A line added, then the first one removed, over two saves.
        actor.Current = editor.User;
        Assert.True((await Lists().SaveDraft(id, request, editor.User)).IsSuccess);
        Assert.Equal(2, (await Lists().Get(id)).Value.Lines.Count);
        // A save made on an older version is refused rather than overwriting the newer one.
        Assert.Equal(ErrorType.Conflict, (await Lists().SaveDraft(id, request, editor.User)).Errors[0].Type);
        request.DraftVersion = 1;
        request.Lines = [draft.Lines[1]];
        Assert.True((await Lists().SaveDraft(id, request, editor.User)).IsSuccess);
        var saved = (await Lists().Get(id)).Value;
        Assert.Equal(f.OtherMaterial, Assert.Single(saved.Lines).MaterialId);
        Assert.Equal(18, saved.RevisionNumber);
        Assert.Empty(db.DocumentReviews);

        // Submitting needs its own key; it fixes the version and opens the review.
        Assert.Equal(ErrorType.Forbidden, (await Lists().Submit(id, new() { DraftVersion = 2 }, editor.User)).Errors[0].Type);
        actor.Current = submitter.User;
        var submitted = await Lists().Submit(id, new() { DraftVersion = 2 }, submitter.User);
        Assert.True(submitted.IsSuccess, submitted.IsFailure ? submitted.Errors[0].Description : null);
        var review = await db.DocumentReviews.AsNoTracking().SingleAsync(item => item.ModelId == id);
        Assert.Equal(DocumentReviewStatus.AwaitingReview, review.Status);
        Assert.Equal(preparer.User, review.PreparedById);
        Assert.Equal(ApprovedVendorListStatus.Pending, (await Lists().Get(id)).Value.Status);
        // A submitted list is no longer a draft.
        actor.Current = editor.User;
        request.DraftVersion = 3;
        Assert.Equal(ErrorType.Conflict, (await Lists().SaveDraft(id, request, editor.User)).Errors[0].Type);
    }

    [Fact]
    public async Task A_draft_names_new_records_without_creating_them_until_it_is_submitted()
    {
        var f = await Masters();
        var request = First(f, Valid);
        request.Lines[0].ManufacturerId = null;
        request.Lines[0].NewManufacturer = new NewApprovedManufacturerRequest { Name = "New Pharma", Address = "Hyderabad" };
        var preparer = await Person("Officer", PermissionKeys.CanCreateApprovedVendorList, PermissionKeys.CanSubmitApprovedVendorList);
        actor.Current = preparer.User;
        var id = (await Lists().Create(request, preparer.User)).Value;
        Assert.False(await db.Manufacturers.AnyAsync(item => item.Name == "New Pharma"));
        var line = Assert.Single((await Lists().Get(id)).Value.Lines);
        Assert.True(line.NewManufacturer);
        Assert.Equal("New Pharma", line.ManufacturerName);
        // Submitting creates it, which needs the manufacturer create key.
        Assert.Equal(ErrorType.Forbidden, (await Lists().Submit(id, new() { DraftVersion = 0 }, preparer.User)).Errors[0].Type);
        Assert.Equal(ApprovedVendorListStatus.Draft, (await db.ApprovedVendorLists.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task A_version_not_yet_approved_is_deleted_with_its_key_and_a_reason_and_the_current_list_stays()
    {
        var f = await Masters();
        var current = await ApprovedList(f, Valid);
        var reviser = await Person("Reviser", PermissionKeys.CanReviseApprovedVendorList, PermissionKeys.CanSubmitApprovedVendorList);
        var deleter = await Person("Deleter", PermissionKeys.CanDeleteApprovedVendorList);
        actor.Current = reviser.User;
        var revision = new ReviseApprovedVendorListRequest { AnnexureNumber = "QAD/SOP/007/A03-06", RevisionNumber = 18, Lines = First(f, Valid).Lines };
        var draft = (await Lists().Revise(revision, reviser.User)).Value;
        Assert.Equal(ErrorType.Forbidden, (await Lists().Delete(draft, reviser.User, "Started by mistake")).Errors[0].Type);
        actor.Current = deleter.User;
        Assert.Equal(ErrorType.Validation, (await Lists().Delete(draft, deleter.User, " ")).Errors[0].Type);
        Assert.True((await Lists().Delete(draft, deleter.User, "Started by mistake")).IsSuccess);
        Assert.Equal(ErrorType.NotFound, (await Lists().Get(draft)).Errors[0].Type);
        Assert.Equal(ApprovedVendorListStatus.Approved, (await Lists().Get(current)).Value.Status);

        // A submitted version in review can be deleted too; its review and approvals end with it.
        actor.Current = reviser.User;
        var submitted = (await Lists().Revise(revision, reviser.User)).Value;
        Assert.True((await Lists().Submit(submitted, new() { DraftVersion = 0 }, reviser.User)).IsSuccess);
        actor.Current = deleter.User;
        Assert.True((await Lists().Delete(submitted, deleter.User, "Wrong supplier on line 1")).IsSuccess);
        var deleted = await db.ApprovedVendorLists.IgnoreQueryFilters().AsNoTracking().SingleAsync(item => item.Id == submitted);
        Assert.Equal("Wrong supplier on line 1", deleted.DeletionReason);
        Assert.Equal(deleter.User, deleted.LastDeletedById);

        // With the versions gone, a new revision can start.
        actor.Current = reviser.User;
        Assert.True((await Lists().Revise(revision, reviser.User)).IsSuccess);
    }

    [Fact]
    public async Task Review_return_correction_and_workflow_approval_sign_checked_and_approved_by()
    {
        var f = await Masters();
        var preparer = await Person("Officer", PermissionKeys.CanEditApprovedVendorList);
        var checker = await Person("Deputy", PermissionKeys.CanReviewApprovedVendorList, PermissionKeys.CanReturnApprovedVendorList);
        var manager = await Person("Manager", PermissionKeys.CanApproveApprovedVendorList);
        db.Approvals.Add(new Approval { Id = Guid.NewGuid(), ItemType = ReviewDocumentTypes.ApprovedVendorList,
            ApprovalStages = [new ApprovalStage { Id = Guid.NewGuid(), RoleId = manager.Role, Order = 1, Required = true }] });
        await db.SaveChangesAsync();

        var id = await Submitted(First(f, Today.AddYears(2)));
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
    public async Task A_revision_starts_from_the_current_lines_adds_and_removes_and_supersedes_only_once_approved()
    {
        var f = await Masters();
        var first = await ApprovedList(f, Valid);
        var reviser = await Person("Reviser", PermissionKeys.CanReviseApprovedVendorList, PermissionKeys.CanSubmitApprovedVendorList,
            PermissionKeys.CanUpdateManufacturerDetails, PermissionKeys.CanUpdateVendorDetails);
        var checker = await Person("Checker", PermissionKeys.CanReviewApprovedVendorList);
        actor.Current = reviser.User;
        // The current line is dropped and a new one added for the other material.
        var added = new SaveApprovedVendorListLineRequest { MaterialId = f.OtherMaterial, ManufacturerId = f.Manufacturer, SupplierId = f.OtherSupplier,
            QuantityPerPack = 50m, ApprovalDate = new DateTime(2024, 6, 1), ValidityDate = Valid };
        var request = new ReviseApprovedVendorListRequest { AnnexureNumber = "QAD/SOP/007/A03-06", RevisionNumber = 17, Lines = [added] };
        Assert.Equal(ErrorType.Validation, (await Lists().Revise(request, reviser.User)).Errors[0].Type);
        request.RevisionNumber = 18;
        var second = await Lists().Revise(request, reviser.User);
        Assert.True(second.IsSuccess);
        Assert.Equal(ErrorType.Conflict, (await Lists().Revise(request, reviser.User)).Errors[0].Type);
        Assert.True((await Lists().Submit(second.Value, new() { DraftVersion = 0 }, reviser.User)).IsSuccess);

        // Until it is approved the current list stays in force: its material is still restricted to its own line.
        Assert.Equal("ApprovedVendorList.SupplierNotApproved", (await ApprovedVendorRules.CheckSuppliers(db, [(f.Material, f.OtherSupplier)], Today)).Errors[0].Code);
        Assert.True((await ApprovedVendorRules.CheckSuppliers(db, [(f.OtherMaterial, f.Supplier)], Today)).IsSuccess);

        actor.Current = checker.User;
        Assert.True((await Reviews().Confirm(ReviewDocumentTypes.ApprovedVendorList, second.Value, new(), checker.User)).IsSuccess);
        db.ChangeTracker.Clear();
        Assert.Equal(ApprovedVendorListStatus.Superseded, (await db.ApprovedVendorLists.SingleAsync(item => item.Id == first)).Status);
        var current = await db.ApprovedVendorLists.SingleAsync(item => item.Id == second.Value);
        Assert.Equal(ApprovedVendorListStatus.Approved, current.Status);
        Assert.Equal(17, current.SupersedesRevision);
        Assert.Equal(first, current.PreviousRevisionId);
        // The removed line no longer restricts anything; the added one now does.
        Assert.True((await ApprovedVendorRules.CheckSuppliers(db, [(f.Material, f.OtherSupplier)], Today)).IsSuccess);
        Assert.Equal("ApprovedVendorList.SupplierNotApproved", (await ApprovedVendorRules.CheckSuppliers(db, [(f.OtherMaterial, f.Supplier)], Today)).Errors[0].Code);
    }

    [Fact]
    public async Task A_new_manufacturer_is_created_and_mapped_on_submit_with_the_create_key()
    {
        var f = await Masters();
        var request = First(f, Valid);
        request.Lines[0].ManufacturerId = null;
        request.Lines[0].NewManufacturer = new NewApprovedManufacturerRequest { Name = "New Pharma", Address = "Hyderabad" };
        await Submitted(request, PermissionKeys.CanCreateManufacturer, PermissionKeys.CanUpdateVendorDetails);
        var manufacturer = await db.Manufacturers.SingleAsync(item => item.Name == "New Pharma");
        Assert.True(await db.ManufacturerMaterials.AnyAsync(item => item.ManufacturerId == manufacturer.Id && item.MaterialId == f.Material));
        Assert.True(await db.SupplierManufacturers.AnyAsync(item => item.ManufacturerId == manufacturer.Id && item.SupplierId == f.Supplier && item.MaterialId == f.Material));
        Assert.Equal(manufacturer.Id, Assert.Single(await db.ApprovedVendorListLines.ToListAsync()).ManufacturerId);
    }

    [Fact]
    public async Task Mappings_offer_the_materials_manufacturers_and_their_suppliers()
    {
        var f = await Masters();
        var mapping = Assert.Single((await Lists().Mappings(f.Material)).Value);
        Assert.Equal(f.Manufacturer, mapping.ManufacturerId);
        Assert.Equal(25m, mapping.QuantityPerPack);
        Assert.Equal(f.Supplier, Assert.Single(mapping.Suppliers).SupplierId);
        var options = await Lists().Options();
        Assert.Equal(2, options.Materials.Count);
        Assert.Equal(2, options.Suppliers.Count);
    }

    [Fact]
    public async Task Only_listed_materials_are_restricted_and_only_to_their_active_lines()
    {
        var f = await Masters();
        await ApprovedList(f, Valid);
        Assert.True((await ApprovedVendorRules.CheckSuppliers(db, [(f.Material, f.Supplier)], Today)).IsSuccess);
        // An unlisted material is unrestricted.
        Assert.True((await ApprovedVendorRules.CheckSuppliers(db, [(f.OtherMaterial, f.OtherSupplier)], Today)).IsSuccess);
        var notListed = await ApprovedVendorRules.CheckSuppliers(db, [(f.Material, f.OtherSupplier)], Today);
        Assert.Equal("ApprovedVendorList.SupplierNotApproved", notListed.Errors[0].Code);
        Assert.Contains("RMA002", notListed.Errors[0].Description);
        // Procurement can use the line up to the last day before its validity month, and not from that month's first day.
        var lapsesOn = ApprovedVendorRules.LapsesOn(Valid);
        Assert.Equal(ApprovedVendorRules.Month(Valid), lapsesOn);
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
    public async Task A_submitted_list_restricts_nothing_until_it_is_approved()
    {
        var f = await Masters();
        await Submitted(First(f, Valid));
        Assert.True((await ApprovedVendorRules.CheckSuppliers(db, [(f.Material, f.OtherSupplier)], Today)).IsSuccess);
    }

    [Fact]
    public async Task Purchase_orders_and_invoices_refuse_a_supplier_or_manufacturer_off_the_list()
    {
        var f = await Masters();
        await ApprovedList(f, Valid);
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

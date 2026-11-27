using System.Reflection;
using API.Authorization;
using API.Controllers;
using APP.Repository;
using APP.Services.Authorization;
using APP.Services.QcWorksheets;
using APP.Services.Reviews;
using APP.Services.StpRevisions;
using APP.Tests.ApprovedVendorLists;
using APP.Tests.Repository;
using APP.Utils;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.MaterialStandardTestProcedures;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.ProductStandardTestProcedures;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.StpRevisions;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED;
using Xunit;

namespace APP.Tests.StpRevisions;

public partial class StpRevisionTests
{
    private const string Number = "QCD/STP/FP/009";
    private readonly AvlActor actor = new();
    private readonly ApplicationDbContext db;
    private int claimId;

    public StpRevisionTests()
    {
        db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, actor);
        var product = new Product { Id = Guid.NewGuid(), Code = "FP009", Name = "X'Zole suspension" };
        var material = new Material { Id = Guid.NewGuid(), Code = "RM020", Name = "Ibuprofen", Kind = MaterialKind.Raw };
        db.Products.Add(product);
        db.Materials.Add(material);
        db.ProductStandardTestProcedures.Add(new ProductStandardTestProcedure { Id = Guid.NewGuid(), StpNumber = Number, ProductId = product.Id });
        db.MaterialStandardTestProcedures.Add(new MaterialStandardTestProcedure { Id = Guid.NewGuid(), StpNumber = "QCD/STP/RM/020", MaterialId = material.Id });
        db.SaveChanges();
    }

    private ApprovalRepository Approvals() => new(db, null!, null!, null!, NullLogger<ApprovalRepository>.Instance, null!,
        new NoOpProductionActivityStepEventPublisher());
    private StpRevisionService Service() => new(db, Approvals(), new StpDocxImportService(), null!);
    private DocumentReviewService Reviews() => new(db, Approvals(), null!);

    private async Task<(Guid User, Guid Role)> Person(string name, params string[] keys)
    {
        var id = Guid.NewGuid();
        var role = new Role { Id = Guid.NewGuid(), Name = name + " role" };
        db.Users.Add(new User { Id = id, UserName = name + id.ToString("N")[..4], FirstName = name, LastName = "Tester" });
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

    private static CreateStpRevisionRequest First(int revision = 2) => new()
    {
        Kind = StpRevisionKind.Product, StpNumber = Number, RevisionNumber = revision, SupersedesRevision = revision - 1,
        Title = "X'ZOLE & METRONIDAZOLE SUSPENSION", Area = "Quality Control", Reference = "BP",
        Sections =
        [
            new() { Kind = StpSectionKind.Purpose, Text = "Analyse X'Zole suspension." },
            new() { Kind = StpSectionKind.Scope, Text = "All batches." },
            new() { Kind = StpSectionKind.Responsibility, Text = "QC Analyst" },
            new() { Kind = StpSectionKind.Accountability, Text = "QC Manager" },
            new() { Kind = StpSectionKind.Reagents, Text = "Methanol\nAcetone" },
        ],
        Tests =
        [
            new() { Number = "5.1", Title = "Description", Steps = [new() { Kind = StpStepKind.Step, Number = "5.1.1", Text = "Note observations." }] },
            new() { Number = "5.2", Title = "Assay", Steps =
            [
                new() { Kind = StpStepKind.Conditions, Heading = "Chromatographic Conditions", Text = "Flowrate: 1.0ml/min\nColumn: C18" },
                new() { Kind = StpStepKind.Step, Number = "5.2.1", Text = "Inject 20 µl." },
            ] },
        ],
    };

    private static SaveStpRevisionDraftRequest Draft(StpRevisionDto dto) => new()
    {
        Version = dto.Version, RevisionNumber = dto.RevisionNumber, SupersedesRevision = dto.SupersedesRevision, Title = dto.Title,
        Area = dto.Area, Reference = dto.Reference, ReasonForChange = dto.ReasonForChange,
        Sections = dto.Sections.Select(item => new StpSectionRequest { Kind = item.Kind, Text = item.Text }).ToList(),
        Tests = dto.Tests.Select(test => new StpTestRequest
        {
            TestKey = test.TestKey, Number = test.Number, Title = test.Title,
            Steps = test.Steps.Select(step => new StpStepRequest { Kind = step.Kind, Number = step.Number, Heading = step.Heading, Text = step.Text, ReferenceCode = step.ReferenceCode }).ToList(),
        }).ToList(),
    };

    private static readonly string[] PreparerKeys =
    [
        PermissionKeys.CanCreateProductStpRevision, PermissionKeys.CanEditProductStpRevisionDraft, PermissionKeys.CanSubmitProductStpRevision,
        PermissionKeys.CanViewProductStpRevisions, PermissionKeys.CanEditProductStpRevision, PermissionKeys.CanReviseProductStpRevision,
    ];

    /// <summary>Created, submitted, checked and (with no workflow configured) approved automatically.</summary>
    private async Task<Guid> Approved((Guid User, Guid Role) preparer, (Guid User, Guid Role) checker)
    {
        actor.Current = preparer.User;
        var id = (await Service().Create(First(), preparer.User)).Value;
        Assert.True((await Service().Submit(id, new() { Version = 0 }, preparer.User)).IsSuccess);
        actor.Current = checker.User;
        var confirmed = await Reviews().Confirm(ReviewDocumentTypes.StpRevision, id, new(), checker.User);
        Assert.True(confirmed.IsSuccess, confirmed.IsFailure ? confirmed.Errors[0].Description : null);
        db.ChangeTracker.Clear();
        return id;
    }

    private static string Policy(string action) => string.Join(",", StpRevisionPermissions.KeysFor(Task(action)));

    private static StpRevisionTask Task(string action)
    {
        var method = typeof(StpRevisionController).GetMethod(action) ?? throw new InvalidOperationException(action);
        Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());
        return Assert.Single(method.GetCustomAttributes<StpRevisionPermissionAttribute>()).Task;
    }

    [Fact]
    public void Every_endpoint_names_its_own_task_and_none_is_anonymous()
    {
        Assert.Equal(StpRevisionTask.View, Task(nameof(StpRevisionController.GetStpRevisions)));
        Assert.Equal(StpRevisionTask.View, Task(nameof(StpRevisionController.GetStpRevisionNumbers)));
        Assert.Equal(StpRevisionTask.View, Task(nameof(StpRevisionController.GetStpRevision)));
        Assert.Equal(StpRevisionTask.Import, Task(nameof(StpRevisionController.ImportStpRevision)));
        Assert.Equal(StpRevisionTask.Import, Task(nameof(StpRevisionController.ImportStpRevisionFromDocument)));
        Assert.Equal(StpRevisionTask.Create, Task(nameof(StpRevisionController.CreateStpRevision)));
        Assert.Equal(StpRevisionTask.Revise, Task(nameof(StpRevisionController.ReviseStpRevision)));
        Assert.Equal(StpRevisionTask.EditDraft, Task(nameof(StpRevisionController.SaveStpRevisionDraft)));
        Assert.Equal(StpRevisionTask.Submit, Task(nameof(StpRevisionController.SubmitStpRevision)));
        Assert.Equal(StpRevisionTask.Edit, Task(nameof(StpRevisionController.UpdateStpRevision)));
        Assert.Equal(StpRevisionTask.Delete, Task(nameof(StpRevisionController.DeleteStpRevision)));
        Assert.Equal(PermissionKeys.CanViewRawMaterialStpRevisions + "," + PermissionKeys.CanViewPackagingMaterialStpRevisions + ","
            + PermissionKeys.CanViewProductStpRevisions, Policy(nameof(StpRevisionController.GetStpRevision)));
    }

    [Fact]
    public void Every_key_is_registered_exactly_once_and_each_task_and_kind_has_its_own()
    {
        var registered = PermissionUtils.GeneratePermissions().Select(permission => permission.Key).ToList();
        var keys = StpRevisionPermissions.All.ToList();
        Assert.Equal(42, keys.Distinct().Count());
        foreach (var key in keys) Assert.Single(registered, item => item == key);
    }

    [Fact]
    public async Task Without_the_kinds_key_nothing_is_read_or_written()
    {
        var raw = await Person("Raw", PermissionKeys.CanCreateRawMaterialStpRevision, PermissionKeys.CanViewRawMaterialStpRevisions);
        actor.Current = raw.User;
        // A raw-material key does not reach a product STP.
        Assert.Equal(ErrorType.Forbidden, (await Service().Create(First(), raw.User)).Errors[0].Type);
        Assert.Equal(ErrorType.Forbidden, (await Service().List(StpRevisionKind.Product, null, raw.User)).Errors[0].Type);
        var preparer = await Person("Preparer", PreparerKeys);
        actor.Current = preparer.User;
        var id = (await Service().Create(First(), preparer.User)).Value;
        actor.Current = raw.User;
        Assert.Equal(ErrorType.Forbidden, (await Service().Get(id, raw.User)).Errors[0].Type);
        Assert.Equal(ErrorType.Forbidden, (await Service().SaveDraft(id, new() { Version = 0, RevisionNumber = 2, Title = "X" }, raw.User)).Errors[0].Type);
        Assert.Equal(ErrorType.Forbidden, (await Service().Delete(id, raw.User, "wrong")).Errors[0].Type);
        Assert.True((await Service().Get(id, preparer.User)).IsSuccess);
    }

    [Fact]
    public async Task A_draft_is_saved_with_its_version_keeps_test_keys_and_a_stale_save_is_refused()
    {
        var preparer = await Person("Preparer", PreparerKeys);
        actor.Current = preparer.User;
        // The STP number must exist as a product STP.
        Assert.Equal(ErrorType.Validation, (await Service().Create(new CreateStpRevisionRequest { Kind = StpRevisionKind.Product, StpNumber = "QCD/STP/FP/999" }, preparer.User)).Errors[0].Type);
        var id = (await Service().Create(First(), preparer.User)).Value;
        Assert.Equal(ErrorType.Conflict, (await Service().Create(First(), preparer.User)).Errors[0].Type);
        var dto = (await Service().Get(id, preparer.User)).Value;
        Assert.Equal(["description", "assay"], dto.Tests.Select(test => test.TestKey));
        Assert.Equal(StpRevisionStatus.Draft, dto.Status);
        Assert.Empty(db.DocumentReviews);

        var draft = Draft(dto);
        draft.Tests[1].Title = "Assay (HPLC)";
        draft.Tests.Insert(1, new StpTestRequest { Number = "5.2", Title = "pH", Steps = [new() { Kind = StpStepKind.Step, Number = "5.2.1", Text = "Determine the pH." }] });
        // A plain calendar date (no time zone) is stored as that day in UTC, which PostgreSQL's timestamptz requires.
        draft.EffectiveDate = new DateTime(2026, 11, 27);
        Assert.True((await Service().SaveDraft(id, draft, preparer.User)).IsSuccess);
        Assert.Equal(DateTimeKind.Utc, db.StpRevisions.Local.Single(revision => revision.Id == id).EffectiveDate!.Value.Kind);
        Assert.Equal(new DateTime(2026, 11, 27, 0, 0, 0, DateTimeKind.Utc), db.StpRevisions.Local.Single(revision => revision.Id == id).EffectiveDate);
        db.ChangeTracker.Clear();
        var saved = (await Service().Get(id, preparer.User)).Value;
        Assert.Equal(1, saved.Version);
        // A renamed test keeps its key; a new one gets its own.
        Assert.Equal(["description", "ph", "assay"], saved.Tests.Select(test => test.TestKey));
        Assert.Equal("Assay (HPLC)", saved.Tests[2].Title);
        Assert.Equal(StpStepKind.Conditions, saved.Tests[2].Steps[0].Kind);

        // Saved again with the version read before the last save.
        Assert.Equal("StpRevision.Stale", (await Service().SaveDraft(id, draft, preparer.User)).Errors[0].Code);
        draft.Version = 1;
        draft.Tests[0].TestKey = "assay";
        Assert.Equal("StpRevision.TestKey", (await Service().SaveDraft(id, draft, preparer.User)).Errors[0].Code);
    }

    [Fact]
    public async Task Submit_needs_complete_content_then_review_return_correction_and_workflow_approval_sign_the_revision()
    {
        var preparer = await Person("Preparer", PreparerKeys);
        var checker = await Person("Deputy", PermissionKeys.CanReviewProductStpRevision, PermissionKeys.CanReturnProductStpRevision);
        var manager = await Person("Manager", PermissionKeys.CanApproveProductStpRevision);
        var outsider = await Person("Outsider", PermissionKeys.CanApproveRawMaterialStpRevision);
        db.Approvals.Add(new Approval { Id = Guid.NewGuid(), ItemType = ReviewDocumentTypes.StpRevision,
            ApprovalStages = [new ApprovalStage { Id = Guid.NewGuid(), RoleId = manager.Role, Order = 1, Required = true }] });
        await db.SaveChangesAsync();

        actor.Current = preparer.User;
        var incomplete = First();
        incomplete.Sections.RemoveAll(item => item.Kind == StpSectionKind.Scope);
        var id = (await Service().Create(incomplete, preparer.User)).Value;
        Assert.Equal("StpRevision.Sections", (await Service().Submit(id, new() { Version = 0 }, preparer.User)).Errors[0].Code);
        var draft = Draft((await Service().Get(id, preparer.User)).Value);
        draft.Sections.Add(new StpSectionRequest { Kind = StpSectionKind.Scope, Text = "All batches." });
        Assert.True((await Service().SaveDraft(id, draft, preparer.User)).IsSuccess);
        Assert.True((await Service().Submit(id, new() { Version = 1 }, preparer.User)).IsSuccess);
        Assert.Equal("StpRevision.NotDraft", (await Service().SaveDraft(id, draft, preparer.User)).Errors[0].Code);

        actor.Current = checker.User;
        Assert.True((await Reviews().Return(ReviewDocumentTypes.StpRevision, id, new() { Comments = "Add the injection volume" }, checker.User)).IsSuccess);

        actor.Current = preparer.User;
        db.ChangeTracker.Clear();
        var pending = (await Service().Get(id, preparer.User)).Value;
        Assert.Equal(DOMAIN.Entities.Reviews.DocumentReviewStatus.Returned, pending.ReviewStatus);
        var correction = Draft(pending);
        var update = new UpdateStpRevisionRequest
        {
            Version = 0, RevisionNumber = correction.RevisionNumber, SupersedesRevision = correction.SupersedesRevision, Title = correction.Title,
            Sections = correction.Sections, Tests = correction.Tests,
        };
        update.Tests[1].Steps[1].Text = "Inject 20 µl of each solution.";
        Assert.Equal("StpRevision.Stale", (await Service().Update(id, update, preparer.User)).Errors[0].Code);
        update.Version = pending.Version;
        Assert.True((await Service().Update(id, update, preparer.User)).IsSuccess);
        Assert.True((await Reviews().Resubmit(ReviewDocumentTypes.StpRevision, id, new(), preparer.User)).IsSuccess);

        actor.Current = checker.User;
        Assert.True((await Reviews().Confirm(ReviewDocumentTypes.StpRevision, id, new(), checker.User)).IsSuccess);
        Assert.Equal(StpRevisionStatus.Pending, (await db.StpRevisions.AsNoTracking().SingleAsync()).Status);

        // The decision key follows the stored kind: a raw-material approver cannot approve a product STP.
        Assert.Equal(ErrorType.Forbidden, (await Approvals().ApproveItem(ReviewDocumentTypes.StpRevision, id, outsider.User, [manager.Role], "OK", null)).Errors[0].Type);
        actor.Current = manager.User;
        var approved = await Approvals().ApproveItem(ReviewDocumentTypes.StpRevision, id, manager.User, [manager.Role], "OK", null);
        Assert.True(approved.IsSuccess, approved.IsFailure ? approved.Errors[0].Description : null);
        db.ChangeTracker.Clear();
        var revision = (await Service().Get(id, preparer.User)).Value;
        Assert.Equal(StpRevisionStatus.Approved, revision.Status);
        Assert.Equal("Inject 20 µl of each solution.", revision.Tests[1].Steps[1].Text);
        Assert.Equal("Deputy Tester", revision.CheckedBy.Name);
        Assert.Equal("Manager Tester", revision.ApprovedBy.Name);
        actor.Current = preparer.User;
        Assert.Equal("StpRevision.NotInReview", (await Service().Update(id, update, preparer.User)).Errors[0].Code);
    }

    [Fact]
    public async Task A_revision_copies_the_approved_content_and_supersedes_it_only_once_approved()
    {
        var preparer = await Person("Preparer", PreparerKeys);
        var checker = await Person("Deputy", PermissionKeys.CanReviewProductStpRevision);
        var first = await Approved(preparer, checker);
        actor.Current = preparer.User;
        Assert.Equal("StpRevision.Exists", (await Service().Create(First(3), preparer.User)).Errors[0].Code);
        Assert.Equal(ErrorType.Validation, (await Service().Revise(new() { Kind = StpRevisionKind.Product, StpNumber = Number, RevisionNumber = 2, ReasonForChange = "x" }, preparer.User)).Errors[0].Type);
        Assert.Equal(ErrorType.Validation, (await Service().Revise(new() { Kind = StpRevisionKind.Product, StpNumber = Number, RevisionNumber = 3 }, preparer.User)).Errors[0].Type);
        var second = (await Service().Revise(new() { Kind = StpRevisionKind.Product, StpNumber = Number, RevisionNumber = 3, ReasonForChange = "BP 2026 update" }, preparer.User)).Value;
        Assert.Equal(ErrorType.Conflict, (await Service().Revise(new() { Kind = StpRevisionKind.Product, StpNumber = Number, RevisionNumber = 4, ReasonForChange = "again" }, preparer.User)).Errors[0].Type);

        var copy = (await Service().Get(second, preparer.User)).Value;
        Assert.Equal(2, copy.SupersedesRevision);
        Assert.Equal(first, copy.RevisedFromId);
        Assert.Equal(["description", "assay"], copy.Tests.Select(test => test.TestKey));
        var draft = Draft(copy);
        draft.Tests.RemoveAt(0);
        draft.Tests.Add(new StpTestRequest { Number = "5.3", Title = "Description", Steps = [new() { Kind = StpStepKind.Step, Number = "5.3.1", Text = "Look." }] });
        Assert.True((await Service().SaveDraft(second, draft, preparer.User)).IsSuccess);
        db.ChangeTracker.Clear();
        // A test re-added under an old title gets a new key: "description" was another test of this STP.
        Assert.Equal(["assay", "description-2"], (await Service().Get(second, preparer.User)).Value.Tests.Select(test => test.TestKey));

        Assert.True((await Service().Submit(second, new() { Version = 1 }, preparer.User)).IsSuccess);
        Assert.Equal(StpRevisionStatus.Approved, (await db.StpRevisions.AsNoTracking().SingleAsync(item => item.Id == first)).Status);
        actor.Current = checker.User;
        Assert.True((await Reviews().Confirm(ReviewDocumentTypes.StpRevision, second, new(), checker.User)).IsSuccess);
        db.ChangeTracker.Clear();
        Assert.Equal(StpRevisionStatus.Superseded, (await db.StpRevisions.AsNoTracking().SingleAsync(item => item.Id == first)).Status);
        Assert.Equal(StpRevisionStatus.Approved, (await db.StpRevisions.AsNoTracking().SingleAsync(item => item.Id == second)).Status);
        var overview = Assert.Single((await Service().Numbers(StpRevisionKind.Product, preparer.User)).Value);
        Assert.Equal(second, overview.Current!.Id);
        Assert.Null(overview.InPreparation);
        Assert.Equal(2, overview.RevisionCount);
    }

    [Fact]
    public async Task A_draft_is_deleted_only_with_its_key_and_a_reason_and_the_slot_is_freed()
    {
        var preparer = await Person("Preparer", PreparerKeys);
        var deleter = await Person("Head", PermissionKeys.CanDeleteProductStpRevision, PermissionKeys.CanViewProductStpRevisions);
        actor.Current = preparer.User;
        var id = (await Service().Create(First(), preparer.User)).Value;
        Assert.Equal(ErrorType.Forbidden, (await Service().Delete(id, preparer.User, "duplicate")).Errors[0].Type);
        actor.Current = deleter.User;
        Assert.Equal("StpRevision.Reason", (await Service().Delete(id, deleter.User, " ")).Errors[0].Code);
        Assert.True((await Service().Delete(id, deleter.User, "Imported the wrong file")).IsSuccess);
        Assert.Empty((await Service().List(StpRevisionKind.Product, Number, deleter.User)).Value);
        var kept = await db.StpRevisions.IgnoreQueryFilters().AsNoTracking().SingleAsync(item => item.Id == id);
        Assert.Equal("Imported the wrong file", kept.DeletionReason);
        actor.Current = preparer.User;
        Assert.True((await Service().Create(First(), preparer.User)).IsSuccess);
    }

    [Fact]
    public async Task Import_needs_its_key_and_reuses_the_approved_test_keys()
    {
        var preparer = await Person("Preparer", [.. PreparerKeys, PermissionKeys.CanImportProductStpRevision]);
        var checker = await Person("Deputy", PermissionKeys.CanReviewProductStpRevision);
        var noImport = await Person("Viewer", PermissionKeys.CanViewProductStpRevisions);
        await using var file = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "StpRevisions", "Fixtures", "009 Xzole susp.docx"));
        var form = new Microsoft.AspNetCore.Http.FormFile(file, 0, file.Length, "file", "009 Xzole susp.docx");
        Assert.Equal(ErrorType.Forbidden, (await Service().Import(StpRevisionKind.Product, form, noImport.User)).Errors[0].Type);

        await Approved(preparer, checker);
        file.Position = 0;
        var proposal = (await Service().Import(StpRevisionKind.Product, form, preparer.User)).Value;
        Assert.True(proposal.Succeeded);
        Assert.Equal(Number, proposal.StpNumber.Value);
        Assert.Equal(StpImportConfidence.High, proposal.StpNumber.Confidence);
        // "Description" and "Assay" match tests of the approved revision, so they keep its keys.
        Assert.Equal(["description", "identification", "metronidazole", "ph", "assay"], proposal.Tests.Select(test => test.TestKey));
        Assert.Empty(db.StpRevisions.Where(item => item.Status == StpRevisionStatus.Draft));

        // As a raw-material STP the number is unknown, and the proposal says so.
        var asRaw = await Person("RawImporter", PermissionKeys.CanImportRawMaterialStpRevision);
        file.Position = 0;
        var raw = (await Service().Import(StpRevisionKind.RawMaterial, form, asRaw.User)).Value;
        Assert.Equal(StpImportConfidence.Low, raw.StpNumber.Confidence);
    }
}

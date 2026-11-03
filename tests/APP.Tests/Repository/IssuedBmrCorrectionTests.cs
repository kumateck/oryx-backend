using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;
using Xunit;
using Npgsql;

namespace APP.Tests.Repository;

public class IssuedBmrCorrectionTests
{
    internal const string Variable = "ORYX_ISSUED_BMR_CORRECTION_TEST_CONNECTION";

    [IssuedBmrFact]
    public async Task PostgreSQL_approves_a_revision_once_across_independent_sessions()
    {
        await using var f = await Fixture.Create(postgres: true);
        var proposed = await f.Repo.RequestRevision(f.Record.Id, f.Dates(), f.Actor);
        Assert.True(proposed.IsSuccess);
        async Task<Result> Approve()
        {
            await using var db = Fixture.Context(postgres: true);
            return await new IssuedBmrRepository(db).ReviewRevision(f.Record.Id, proposed.Value,
                new() { Approve = true, Reason = "Checked the recorded manufacturing evidence" }, f.Approver);
        }
        var results = await Task.WhenAll(Approve(), Approve());
        Assert.Single(results, result => result.IsSuccess);
        Assert.Equal("IssuedBmr.Reviewed", Assert.Single(results, result => result.IsFailure).Error.Code);
        await using var verify = Fixture.Context(postgres: true);
        Assert.Single(await verify.IssuedBmrChanges.ToListAsync());
        Assert.Equal(f.Expires.AddYears(1), (await verify.BatchManufacturingRecords.IgnoreQueryFilters().SingleAsync()).ExpiryDate);
        Assert.Equal(f.Expires, (await verify.Coas.IgnoreQueryFilters().SingleAsync(coa => coa.Status == CoaStatus.Issued)).ExpiryDate);
    }

    [IssuedBmrFact]
    public async Task PostgreSQL_audit_failure_rolls_back_every_propagated_record()
    {
        await using var f = await Fixture.Create(postgres: true);
        await f.Db.Database.ExecuteSqlRawAsync("CREATE FUNCTION reject_bmr_audit() RETURNS trigger LANGUAGE plpgsql AS 'BEGIN RAISE EXCEPTION ''audit fixture failure''; END'; CREATE TRIGGER reject_bmr_audit BEFORE INSERT ON \"IssuedBmrChanges\" FOR EACH ROW EXECUTE FUNCTION reject_bmr_audit()");
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Repo.CorrectNumber(f.Record.Id, f.Number("SHOULD-ROLLBACK"), f.Actor));
        await using var verify = Fixture.Context(postgres: true);
        Assert.Equal("BATCH-OLD", (await verify.BatchManufacturingRecords.IgnoreQueryFilters().SingleAsync()).BatchNumber);
        Assert.Equal("BATCH-OLD", (await verify.BatchPackagingRecords.IgnoreQueryFilters().SingleAsync()).BatchNumber);
        Assert.Equal("BATCH-OLD", (await verify.BinCardInformation.IgnoreQueryFilters().SingleAsync()).ProductBatchNumber);
        Assert.Empty(await verify.IssuedBmrChanges.ToListAsync());
    }

    [Fact]
    public async Task List_includes_only_issued_BMRs_and_enforces_live_list_permission()
    {
        await using var f = await Fixture.Create();
        var pending = new BatchManufacturingRecord { ProductionScheduleProductId = f.Record.ProductionScheduleProductId, BatchNumber = "NOT-ISSUED" };
        f.Db.Add(pending); await f.Db.SaveChangesAsync();
        var result = await f.Repo.List(f.Actor, 1, 50, "Zinvite");
        Assert.True(result.IsSuccess);
        Assert.Equal(f.Record.Id, Assert.Single(result.Value.Data).Id);
        Assert.Equal("IssuedBmr.Permission", (await f.Repo.List(Guid.NewGuid(), 1, 50, "")).Error.Code);
    }

    [Fact]
    public async Task Typo_correction_updates_linked_live_records_and_audit_without_changing_issued_certificates_or_status()
    {
        await using var f = await Fixture.Create();
        var result = await f.Repo.CorrectNumber(f.Record.Id, f.Number("BATCH-NEW"), f.Actor);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        Assert.Equal("BATCH-NEW", f.Record.BatchNumber);
        Assert.Equal("BATCH-NEW", f.Record.ProductionScheduleProduct.BatchNumber);
        Assert.Equal("BATCH-NEW", (await f.Db.BatchPackagingRecords.SingleAsync()).BatchNumber);
        Assert.Equal("BATCH-NEW", (await f.Db.BinCardInformation.SingleAsync()).ProductBatchNumber);
        Assert.Equal("BATCH-NEW", (await f.Db.QcTestRequestSubjects.SingleAsync()).SubjectRef);
        Assert.Equal("BATCH-OLD", (await f.Db.Coas.SingleAsync(coa => coa.Status == CoaStatus.Issued)).BatchNumber);
        Assert.Equal("BATCH-NEW", (await f.Db.Coas.SingleAsync(coa => coa.Status == CoaStatus.Draft)).BatchNumber);
        Assert.Equal(f.Made, f.Record.ManufacturingDate);
        Assert.Equal(BatchManufacturingStatus.Approved, f.Record.Status);
        Assert.Equal(f.IssuedAt, f.Record.IssuedDate);
        var audit = await f.Db.IssuedBmrChanges.SingleAsync();
        Assert.Equal("BATCH-OLD", audit.PreviousBatchNumber);
        Assert.Equal("BATCH-NEW", audit.BatchNumber);
        Assert.Equal(f.Actor, audit.ActorId);
        Assert.Null(audit.RevisionId);
    }

    [Fact]
    public async Task Date_revision_leaves_live_dates_unchanged_until_someone_else_approves()
    {
        await using var f = await Fixture.Create();
        var request = f.Dates();
        var proposed = await f.Repo.RequestRevision(f.Record.Id, request, f.Actor);
        Assert.True(proposed.IsSuccess);
        Assert.Equal(f.Expires, f.Record.ExpiryDate);
        Assert.Empty(f.Db.IssuedBmrChanges);
        Assert.Equal("IssuedBmr.SelfApproval", (await f.Repo.ReviewRevision(f.Record.Id, proposed.Value, new() { Approve = true, Reason = "Reviewed source documents" }, f.Actor)).Error.Code);
        var approved = await f.Repo.ReviewRevision(f.Record.Id, proposed.Value, new() { Approve = true, Reason = "Verified controlled source" }, f.Approver);
        Assert.True(approved.IsSuccess, approved.IsFailure ? approved.Error.Description : null);
        Assert.Equal(request.ExpiryDate, f.Record.ExpiryDate);
        Assert.Equal(request.ExpiryDate, (await f.Db.BatchPackagingRecords.SingleAsync()).ExpiryDate);
        Assert.Equal(f.Expires, (await f.Db.Coas.SingleAsync(coa => coa.Status == CoaStatus.Issued)).ExpiryDate);
        Assert.Equal(request.ExpiryDate, (await f.Db.Coas.SingleAsync(coa => coa.Status == CoaStatus.Draft)).ExpiryDate);
        Assert.Equal(proposed.Value, (await f.Db.IssuedBmrChanges.SingleAsync()).RevisionId);
        Assert.Equal(f.Approver, (await f.Db.IssuedBmrRevisions.SingleAsync()).ReviewedById);
        Assert.Equal("IssuedBmr.Reviewed", (await f.Repo.ReviewRevision(f.Record.Id, proposed.Value, new() { Approve = true, Reason = "Duplicate review" }, f.Approver)).Error.Code);
    }

    [Fact]
    public async Task Rejecting_a_revision_changes_no_batch_information_and_keeps_review_history()
    {
        await using var f = await Fixture.Create();
        var proposed = await f.Repo.RequestRevision(f.Record.Id, f.Dates(), f.Actor);
        Assert.True((await f.Repo.ReviewRevision(f.Record.Id, proposed.Value, new() { Approve = false, Reason = "Source date was correct" }, f.Approver)).IsSuccess);
        Assert.Equal(f.Expires, f.Record.ExpiryDate);
        Assert.Empty(f.Db.IssuedBmrChanges);
        Assert.Equal(IssuedBmrRevisionStatus.Rejected, (await f.Db.IssuedBmrRevisions.SingleAsync()).Status);
    }

    [Fact]
    public async Task Stale_duplicate_unissued_and_unauthorized_corrections_fail_without_writes()
    {
        await using var f = await Fixture.Create();
        var stale = f.Number("NEW"); stale.ExpectedBatchNumber = "STALE";
        Assert.Equal("IssuedBmr.Changed", (await f.Repo.CorrectNumber(f.Record.Id, stale, f.Actor)).Error.Code);
        Assert.Equal("IssuedBmr.Permission", (await f.Repo.CorrectNumber(f.Record.Id, f.Number("NEW"), f.Viewer)).Error.Code);
        f.Db.BatchManufacturingRecords.Add(new() { ProductionScheduleProductId = f.Record.ProductionScheduleProductId, BatchNumber = "TAKEN" });
        await f.Db.SaveChangesAsync();
        Assert.Equal("Bmr.DuplicateBatchNumber", (await f.Repo.CorrectNumber(f.Record.Id, f.Number("taken"), f.Actor)).Error.Code);
        f.Record.IssuedDate = null; await f.Db.SaveChangesAsync();
        Assert.Equal("IssuedBmr.NotFound", (await f.Repo.CorrectNumber(f.Record.Id, f.Number("NEW"), f.Actor)).Error.Code);
        Assert.Empty(f.Db.IssuedBmrChanges);
    }

    [Fact]
    public async Task Pending_duplicate_and_stale_date_revisions_are_refused()
    {
        await using var f = await Fixture.Create();
        var proposed = await f.Repo.RequestRevision(f.Record.Id, f.Dates(), f.Actor);
        Assert.Equal("IssuedBmr.Pending", (await f.Repo.RequestRevision(f.Record.Id, f.Dates(), f.Actor)).Error.Code);
        f.Record.ExpiryDate = f.Expires.AddYears(1); await f.Db.SaveChangesAsync();
        Assert.Equal("IssuedBmr.Changed", (await f.Repo.ReviewRevision(f.Record.Id, proposed.Value, new() { Approve = true, Reason = "Verify proposed date" }, f.Approver)).Error.Code);
        Assert.Empty(f.Db.IssuedBmrChanges);
        Assert.Equal(IssuedBmrRevisionStatus.Pending, (await f.Db.IssuedBmrRevisions.SingleAsync()).Status);
    }

    [Fact]
    public async Task Production_department_scope_and_separate_revision_permissions_are_enforced()
    {
        await using var f = await Fixture.Create();
        var department = new Department { Name = "Other production", Type = DepartmentType.Production };
        f.Db.Departments.Add(department);
        (await f.Db.Users.SingleAsync(user => user.Id == f.Actor)).Department = department;
        await f.Db.SaveChangesAsync();
        Assert.Empty((await f.Repo.List(f.Actor, 1, 50, "")).Value.Data);
        Assert.Equal("IssuedBmr.NotFound", (await f.Repo.CorrectNumber(f.Record.Id, f.Number("NEW"), f.Actor)).Error.Code);
        Assert.Equal("IssuedBmr.Permission", (await f.Repo.RequestRevision(f.Record.Id, f.Dates(), f.Viewer)).Error.Code);
        Assert.Empty(f.Db.IssuedBmrChanges);
        Assert.Empty(f.Db.IssuedBmrRevisions);
    }

    [Fact]
    public async Task Legacy_issue_endpoints_cannot_modify_or_reissue_an_already_issued_BMR()
    {
        await using var f = await Fixture.Create();
        var repository = new ProductionScheduleRepository(f.Db, null!, null!, null!, null!, null!, new NoOpProductionActivityStepEventPublisher());
        var edit = await repository.UpdateBatchManufacturingRecord(new() { BatchNumber = "BYPASS", ManufacturingDate = f.Made.AddYears(1), ExpiryDate = f.Expires }, f.Record.Id);
        Assert.Equal("Bmr.AlreadyIssued", edit.Error.Code);
        Assert.Equal("Bmr.AlreadyIssued", (await repository.IssueBatchManufacturingRecord(f.Record.Id, f.Actor)).Error.Code);
        var packaging = await f.Db.BatchPackagingRecords.IgnoreQueryFilters().SingleAsync();
        Assert.Equal("Bmr.AlreadyIssued", (await repository.UpdateBatchPackagingRecord(new() { BatchNumber = "BYPASS-BPR" }, packaging.Id)).Error.Code);
        Assert.Equal("BATCH-OLD", f.Record.BatchNumber);
        Assert.Equal(f.Made, f.Record.ManufacturingDate);
        Assert.Empty(f.Db.IssuedBmrChanges);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public ApplicationDbContext Db { get; }
        private Fixture(bool postgres) { Db = Context(postgres); }
        public static ApplicationDbContext Context(bool postgres)
        {
            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
            if (postgres)
            {
                var connection = Environment.GetEnvironmentVariable(Variable) ?? throw new InvalidOperationException("A disposable local test connection is required.");
                var parsed = new NpgsqlConnectionStringBuilder(connection);
                if (parsed.Host is not ("127.0.0.1" or "localhost") || parsed.Database != "oryx_issued_bmr_test")
                    throw new InvalidOperationException("Only the dedicated local issued-BMR fixture is permitted.");
                builder.UseNpgsql(connection);
            }
            else builder.UseInMemoryDatabase(Guid.NewGuid().ToString());
            return new(builder.Options, new CurrentUser());
        }
        public IssuedBmrRepository Repo => new(Db);
        public Guid Actor { get; } = Guid.NewGuid();
        public Guid Approver { get; } = Guid.NewGuid();
        public Guid Viewer { get; } = Guid.NewGuid();
        public BatchManufacturingRecord Record { get; private set; } = null!;
        public DateTime Made { get; } = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public DateTime Expires { get; } = new(2028, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public DateTime IssuedAt { get; } = new(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        public CorrectIssuedBmrNumber Number(string number) => new() { BatchNumber = number, ExpectedBatchNumber = Record.BatchNumber,
            ExpectedManufacturingDate = Made, ExpectedExpiryDate = Expires, Reason = "Correct transcription from source" };
        public RequestIssuedBmrRevision Dates() => new() { ExpectedBatchNumber = Record.BatchNumber,
            ExpectedManufacturingDate = Made, ExpectedExpiryDate = Expires, ManufacturingDate = Made, ExpiryDate = Expires.AddYears(1), Reason = "Controlled expiry correction" };
        public static async Task<Fixture> Create(bool postgres = false)
        {
            var f = new Fixture(postgres);
            if (postgres)
            {
                await f.Db.Database.EnsureDeletedAsync(); await f.Db.Database.EnsureCreatedAsync();
                await f.Db.Database.OpenConnectionAsync();
                await f.Db.Database.ExecuteSqlRawAsync("SET session_replication_role = replica");
            }
            var department = new Department { Name = "QA", Type = DepartmentType.NonProduction };
            f.Db.Departments.Add(department);
            foreach (var user in new[] { f.Actor, f.Approver, f.Viewer })
            {
                var role = new Role { Name = user.ToString() }; f.Db.Roles.Add(role);
                f.Db.Users.Add(new() { Id = user, UserName = user.ToString(), FirstName = "QA", LastName = "Operator", Department = department });
                f.Db.UserRoles.Add(new() { UserId = user, RoleId = role.Id });
                foreach (var permission in new[] { PermissionKeys.CanViewIssuedBmrRecords, PermissionKeys.CanCorrectIssuedBmrBatchNumber,
                             PermissionKeys.CanRequestIssuedBmrRevision, PermissionKeys.CanApproveIssuedBmrRevision }.Where(permission => user != f.Viewer || permission == PermissionKeys.CanViewIssuedBmrRecords))
                {
                    var claim = new IdentityRoleClaim<Guid> { Id = f.Db.RoleClaims.Local.Count + 1, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = permission };
                    f.Db.RoleClaims.Add(claim); f.Db.PermissionTypes.Add(new() { Key = permission, RoleClaimId = claim.Id, Type = "assigned" });
                }
            }
            var product = new Product { Name = "Zinvite", Code = "P1", Department = department };
            var batch = new ProductionScheduleProduct { Product = product, BatchNumber = "BATCH-OLD", ProductionSchedule = new() { Code = "PS1", Department = department } };
            var step = new ProductionActivityStep { Operation = new Operation { Name = "BMR" }, ProductionActivity = new() { ProductionScheduleProduct = batch } };
            f.Record = new() { BatchNumber = "BATCH-OLD", ManufacturingDate = f.Made, ExpiryDate = f.Expires, IssuedDate = f.IssuedAt, ProductionActivityStep = step,
                ProductionScheduleProduct = batch, Status = BatchManufacturingStatus.Approved };
            f.Db.Add(f.Record);
            f.Db.BatchPackagingRecords.Add(new() { ProductionActivityStep = step, ProductionScheduleProduct = batch, BatchNumber = "BATCH-OLD", ManufacturingDate = f.Made, ExpiryDate = f.Expires });
            var requisition = new Requisition { ProductionScheduleProduct = batch };
            f.Db.Requisitions.Add(requisition);
            f.Db.BinCardInformation.Add(new() { RequisitionId = requisition.Id, ProductBatchNumber = "BATCH-OLD" });
            var subject = new TestRequestSubject { BatchManufacturingRecord = f.Record, SubjectRef = "BATCH-OLD" };
            var test = new TestRequest { ArNumber = "TEST-AR", Subjects = [subject] }; f.Db.Add(test);
            foreach (var status in new[] { CoaStatus.Draft, CoaStatus.Issued })
                f.Db.Coas.Add(new() { CertificateCode = $"COA-{status}", TestRequest = test, Status = status, BatchNumber = "BATCH-OLD", ManufacturingDate = f.Made, ExpiryDate = f.Expires });
            await f.Db.SaveChangesAsync();
            if (postgres) await f.Db.Database.ExecuteSqlRawAsync("SET session_replication_role = DEFAULT");
            return f;
        }
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
    private sealed class CurrentUser : ICurrentUserService
    { public Guid? UserId => null; public Guid? DepartmentId => null; public string DepartmentType => string.Empty; }
}

public sealed class IssuedBmrFactAttribute : FactAttribute
{
    public IssuedBmrFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(IssuedBmrCorrectionTests.Variable)))
            Skip = "Set the dedicated local issued-BMR PostgreSQL fixture connection to run.";
    }
}

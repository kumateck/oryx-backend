using System.Reflection;
using API.Controllers;
using APP.Repository;
using APP.Services.Forms;
using APP.Utils;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
using APP.Services.Formulas;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.MaterialSampling;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;
namespace APP.Tests.Repository;
public sealed class TestCorrectionTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? DepartmentId => null;
        public string DepartmentType => "QualityControl";
    }
    private sealed class Fixture : IAsyncDisposable
    {
        public Actor Actor { get; } = new();
        public ApplicationDbContext Db { get; }
        public TestCorrectionService Service { get; }
        public Guid Role { get; } = Guid.NewGuid();
        public Response Response { get; private set; }
        public Fixture(string connection = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>();
            if (connection == null) options.UseInMemoryDatabase(Guid.NewGuid().ToString(), o => o.EnableNullChecks(false));
            else options.UseNpgsql(connection);
            Db = new(options.Options, Actor); Service = new(Db, Actor);
        }
        public async Task Grant(params string[] keys)
        {
            var id = await Db.RoleClaims.Select(c => (int?)c.Id).MaxAsync() ?? 0;
            foreach (var key in keys)
            {
                Db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = ++id, RoleId = Role, ClaimType = AppConstants.Permission, ClaimValue = key });
                Db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = id, Key = key, Type = "Action" });
            }
            await Db.SaveChangesAsync();
        }
        public async Task Seed(TemplateRepairTestKind kind, bool retest = false)
        {
            Db.Users.Add(new User { Id = Actor.UserId.Value, FirstName = "QC", LastName = "Checker" });
            Db.Roles.Add(new Role { Id = Role, Name = "QC" });
            Db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = Actor.UserId.Value, RoleId = Role });
            var form = new Form { Id = Guid.NewGuid(), Name = "Assay" };
            var section = new FormSection { Id = Guid.NewGuid(), FormId = form.Id, Name = "Assay" };
            var question = new Question { Id = Guid.NewGuid(), Label = "Weight", Type = QuestionType.ShortAnswer };
            var field = new FormField { Id = Guid.NewGuid(), FormSectionId = section.Id, QuestionId = question.Id };
            Response = new Response { Id = Guid.NewGuid(), FormId = form.Id,
                FormResponses = [new FormResponse { Id = Guid.NewGuid(), FormFieldId = field.Id, Value = "61.0224" }] };
            Db.AddRange(form, section, question, field);
            if (kind == TemplateRepairTestKind.Product)
            {
                Response.BatchManufacturingRecordId = Guid.NewGuid(); Response.ProductionActivityStepId = Guid.NewGuid();
                Db.BatchManufacturingRecords.Add(new BatchManufacturingRecord { Id = Response.BatchManufacturingRecordId.Value, Status = BatchManufacturingStatus.TestTaken });
                Db.AnalyticalTestRequests.Add(new AnalyticalTestRequest { Id = Guid.NewGuid(), BatchManufacturingRecordId = Response.BatchManufacturingRecordId.Value,
                    ProductionActivityStepId = Response.ProductionActivityStepId.Value, Status = AnalyticalTestStatus.TestTaken, TestedAt = DateTime.UtcNow, TestedById = Actor.UserId });
            }
            else
            {
                var material = new Material { Id = Guid.NewGuid(), Name = "Test", Kind = kind == TemplateRepairTestKind.RawMaterial ? MaterialKind.Raw : MaterialKind.Package };
                var unit = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Gram", Symbol = "g" };
                Db.UnitOfMeasures.Add(unit);
                var batch = new MaterialBatch { Id = Guid.NewGuid(), MaterialId = material.Id, UoMId = unit.Id, Status = retest ? BatchStatus.Frozen : BatchStatus.TestTaken };
                Response.MaterialBatchId = batch.Id; Db.AddRange(material, batch);
                if (retest)
                {
                    var request = new MaterialRetestRequest { Id = Guid.NewGuid(), MaterialBatchId = batch.Id, Status = MaterialRetestStatus.TestTaken };
                    var sample = new MaterialSampling { Id = Guid.NewGuid(), MaterialBatchId = batch.Id, MaterialRetestRequestId = request.Id, SampleDate = DateTime.UtcNow };
                    Response.MaterialSamplingId = sample.Id; Db.AddRange(request, sample);
                }
            }
            Db.Responses.Add(Response); await Db.SaveChangesAsync();
        }
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
    [Theory]
    [InlineData(TemplateRepairTestKind.Product, false)]
    [InlineData(TemplateRepairTestKind.RawMaterial, false)]
    [InlineData(TemplateRepairTestKind.PackagingMaterial, false)]
    [InlineData(TemplateRepairTestKind.RawMaterial, true)]
    [InlineData(TemplateRepairTestKind.PackagingMaterial, true)]
    public async Task Permission_reason_populated_draft_history_and_resubmission_gate(TemplateRepairTestKind kind, bool retest)
    {
        await using var f = new Fixture(); await f.Seed(kind, retest);
        Assert.True((await f.Service.Return(f.Response.Id, kind, new("Correct the weight"))).IsFailure);
        Assert.True((await f.Service.History(f.Response.Id, kind)).IsFailure);
        await f.Grant(TestCorrectionService.ReturnKey(kind), TestCorrectionService.HistoryKey(kind));
        Assert.True((await f.Service.Return(f.Response.Id, kind, new("  "))).IsFailure);
        Assert.True((await f.Service.History(f.Response.Id, kind)).Value.CanReturn);
        var result = await f.Service.Return(f.Response.Id, kind, new(" Correct the weight "));
        Assert.True(result.IsSuccess, result.Error?.Description);
        var copy = await f.Db.Responses.Include(r => r.FormResponses).SingleAsync(r => r.Id == result.Value);
        Assert.Equal("61.0224", Assert.Single(copy.FormResponses).Value);
        Assert.Equal(f.Response.FormResponses[0].CreatedById, copy.FormResponses[0].CreatedById);
        Assert.Equal(f.Response.CreatedById, copy.CreatedById);
        Assert.True((await TestCorrectionGuard.Writer(f.Db, copy, Guid.NewGuid())).IsFailure);
        Assert.Equal(1, copy.ReworkSequence);
        Assert.Equal("61.0224", f.Response.FormResponses[0].Value);
        Assert.True((await TestCorrectionGuard.CanGenerate(f.Db, f.Response)).IsFailure);
        Assert.True((await TestCorrectionGuard.CanGenerate(f.Db, copy)).IsFailure);
        Assert.True((await f.Service.Return(f.Response.Id, kind, new("Duplicate"))).IsFailure);
        var history = (await f.Service.History(copy.Id, kind)).Value;
        Assert.False(history.CanReturn); Assert.Equal("Correct the weight", Assert.Single(history.History).Reason);
        if (kind == TemplateRepairTestKind.Product)
        {
            var test = await f.Db.AnalyticalTestRequests.SingleAsync(); Assert.Equal(AnalyticalTestStatus.Testing, test.Status); Assert.Null(test.TestedAt);
            test.Status = AnalyticalTestStatus.TestTaken;
            (await f.Db.BatchManufacturingRecords.SingleAsync()).Status = BatchManufacturingStatus.TestTaken;
        }
        else if (retest)
        {
            Assert.Equal(BatchStatus.Frozen, (await f.Db.MaterialBatches.SingleAsync()).Status);
            var test = await f.Db.MaterialRetestRequests.SingleAsync(); Assert.Equal(MaterialRetestStatus.Testing, test.Status); test.Status = MaterialRetestStatus.TestTaken;
        }
        else (await f.Db.MaterialBatches.SingleAsync()).Status = BatchStatus.TestTaken;
        await f.Db.SaveChangesAsync();
        Assert.True((await TestCorrectionGuard.CanGenerate(f.Db, copy)).IsSuccess);
        Assert.True((await f.Service.Return(copy.Id, kind, new("Second review correction"))).IsSuccess);
        Assert.Equal(2, await f.Db.TestCorrectionReturns.CountAsync());
    }
    [Theory]
    [InlineData(TemplateRepairTestKind.Product)]
    [InlineData(TemplateRepairTestKind.RawMaterial)]
    [InlineData(TemplateRepairTestKind.PackagingMaterial)]
    public async Task Wrong_kind_and_checked_results_are_refused(TemplateRepairTestKind kind)
    {
        await using var f = new Fixture(); await f.Seed(kind);
        await f.Grant(TestCorrectionService.ReturnKey(kind), TestCorrectionService.ReturnKey(kind == TemplateRepairTestKind.Product ? TemplateRepairTestKind.RawMaterial : TemplateRepairTestKind.Product));
        var wrong = kind == TemplateRepairTestKind.Product ? TemplateRepairTestKind.RawMaterial : TemplateRepairTestKind.Product;
        Assert.True((await f.Service.Return(f.Response.Id, wrong, new("Must not cross variants"))).IsFailure);
        f.Response.CheckedAt = DateTime.UtcNow; await f.Db.SaveChangesAsync();
        Assert.True((await f.Service.Return(f.Response.Id, kind, new("Already certified"))).IsFailure);
        Assert.Empty(await f.Db.TestCorrectionReturns.ToListAsync());
    }
    [Fact]
    public async Task Repaired_formula_definition_is_preserved_but_execution_must_be_recalculated()
    {
        await using var f = new Fixture(); await f.Seed(TemplateRepairTestKind.RawMaterial);
        await f.Grant(PermissionKeys.CanReturnRawMaterialTestForCorrection);
        var pin = new LegacyFormulaConfigurationPin { Id = Guid.NewGuid(), ResponseId = f.Response.Id,
            FormFieldId = f.Response.FormResponses[0].FormFieldId, Sequence = 2, RepairId = Guid.NewGuid(),
            Definition = "{\"expression\":\":C-:A\"}", DefinitionHash = "captured-definition", CapturedAt = DateTime.UtcNow };
        f.Db.LegacyFormulaConfigurationPins.Add(pin); await f.Db.SaveChangesAsync();
        var result = await f.Service.Return(f.Response.Id, TemplateRepairTestKind.RawMaterial, new("Correct the reading"));
        Assert.True(result.IsSuccess);
        var copied = await f.Db.LegacyFormulaConfigurationPins.SingleAsync(p => p.ResponseId == result.Value);
        Assert.Equal(pin.Definition, copied.Definition); Assert.Equal(pin.DefinitionHash, copied.DefinitionHash);
        Assert.True(copied.RequiresRecalculation);
        Assert.True(await LegacyFormulaConfigurationReader.Pending(f.Db, await f.Db.Responses.SingleAsync(r => r.Id == result.Value)));
        Assert.Empty(await f.Db.LegacyFormulaRepairFinalizations.ToListAsync());
    }
    [Fact]
    public async Task Corrected_draft_requires_an_assigned_analyst_with_current_variant_permission()
    {
        await using var f = new Fixture(); await f.Seed(TemplateRepairTestKind.RawMaterial);
        await f.Grant(PermissionKeys.CanReturnRawMaterialTestForCorrection);
        var returned = await f.Service.Return(f.Response.Id, TemplateRepairTestKind.RawMaterial, new("Correct readings"));
        var draft = await f.Db.Responses.SingleAsync(r => r.Id == returned.Value);
        Assert.True((await TestCorrectionGuard.Writer(f.Db, draft, f.Actor.UserId.Value)).IsFailure);
        await f.Grant(PermissionKeys.CanStartPackagingMaterialTest);
        Assert.True((await TestCorrectionGuard.Writer(f.Db, draft, f.Actor.UserId.Value)).IsFailure);
        await f.Grant(PermissionKeys.CanStartRawMaterialTest);
        Assert.True((await TestCorrectionGuard.Writer(f.Db, draft, f.Actor.UserId.Value)).IsFailure);
        f.Db.FormAssignees.Add(new FormAssignee { Id = Guid.NewGuid(), FormId = draft.FormId, MaterialBatchId = draft.MaterialBatchId,
            FieldAssignees = [new FormFieldAssignee { Id = Guid.NewGuid(), FormFieldId = f.Response.FormResponses[0].FormFieldId, AssigneeId = f.Actor.UserId }] });
        await f.Db.SaveChangesAsync();
        Assert.True((await TestCorrectionGuard.Writer(f.Db, draft, f.Actor.UserId.Value)).IsSuccess);
        f.Db.RoleClaims.RemoveRange(f.Db.RoleClaims.Where(c => c.ClaimValue == PermissionKeys.CanStartRawMaterialTest)); await f.Db.SaveChangesAsync();
        Assert.True((await TestCorrectionGuard.Writer(f.Db, draft, f.Actor.UserId.Value)).IsFailure);
    }
    private sealed class CorrectionPostgresFactAttribute : FactAttribute
    {
        public CorrectionPostgresFactAttribute()
        {
            if (Environment.GetEnvironmentVariable("ORYX_TEMPLATE_REPAIR_TEST_CONNECTION") == null)
                Skip = "Set ORYX_TEMPLATE_REPAIR_TEST_CONNECTION to a disposable PostgreSQL server.";
        }
    }
    [CorrectionPostgresFact]
    public async Task Postgres_retains_original_evidence_and_rejects_duplicate_returns()
    {
        var admin = new Npgsql.NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ORYX_TEMPLATE_REPAIR_TEST_CONNECTION"));
        var database = "oryx_test_corrections_" + Guid.NewGuid().ToString("N");
        await using var connection = new Npgsql.NpgsqlConnection(admin.ConnectionString); await connection.OpenAsync();
        await using (var command = new Npgsql.NpgsqlCommand($"CREATE DATABASE \"{database}\"", connection)) await command.ExecuteNonQueryAsync();
        var target = new Npgsql.NpgsqlConnectionStringBuilder(admin.ConnectionString) { Database = database }.ConnectionString;
        try
        {
            await using var f = new Fixture(target); await f.Db.Database.MigrateAsync();
            await using (var seed = await f.Db.Database.BeginTransactionAsync())
            {
                await f.Db.Database.ExecuteSqlRawAsync("SET LOCAL session_replication_role = replica");
                await f.Seed(TemplateRepairTestKind.RawMaterial); await seed.CommitAsync();
            }
            await f.Grant(PermissionKeys.CanReturnRawMaterialTestForCorrection, PermissionKeys.CanViewRawMaterialTestCorrectionHistory);
            f.Db.ChangeTracker.Clear();
            var sourceId = f.Response.Id;
            var result = await f.Service.Return(sourceId, TemplateRepairTestKind.RawMaterial, new("Verify the entered weight"));
            Assert.True(result.IsSuccess, result.Error?.Description);
            var history = (await f.Service.History(result.Value, TemplateRepairTestKind.RawMaterial)).Value;
            Assert.Single(history.History);
            foreach (var sql in new[] {
                "UPDATE \"TestCorrectionReturns\" SET \"Reason\" = 'tampered'", "DELETE FROM \"TestCorrectionReturns\"", "TRUNCATE \"TestCorrectionReturns\" CASCADE",
                $"UPDATE \"Responses\" SET \"Approved\" = true WHERE \"Id\" = '{sourceId}'",
                $"UPDATE \"FormResponses\" SET \"Value\" = 'tampered' WHERE \"ResponseId\" = '{sourceId}'" })
                await Assert.ThrowsAsync<Npgsql.PostgresException>(() => f.Db.Database.ExecuteSqlRawAsync(sql));
            Assert.True((await f.Service.Return(sourceId, TemplateRepairTestKind.RawMaterial, new("Duplicate"))).IsFailure);
            Assert.Equal("61.0224", await f.Db.FormResponses.Where(a => a.ResponseId == sourceId).Select(a => a.Value).SingleAsync());
            var batch = await f.Db.MaterialBatches.IgnoreAutoIncludes().SingleAsync(); batch.Status = BatchStatus.TestTaken; await f.Db.SaveChangesAsync();
            async Task<bool> Attempt()
            {
                await using var other = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(target).Options, f.Actor);
                try { return (await new TestCorrectionService(other, f.Actor).Return(result.Value, TemplateRepairTestKind.RawMaterial, new("Concurrent review"))).IsSuccess; }
                catch (InvalidOperationException e) when (e.InnerException is DbUpdateException { InnerException: Npgsql.PostgresException { SqlState: "40001" or "23505" } }) { return false; }
                catch (Npgsql.PostgresException e) when (e.SqlState is "40001" or "40P01") { return false; }
                catch (DbUpdateException e) when (e.InnerException is Npgsql.PostgresException { SqlState: "40001" or "23505" }) { return false; }
            }
            var attempts = await Task.WhenAll(Attempt(), Attempt());
            Assert.Single(attempts.Where(ok => ok));
            Assert.Equal(2, await f.Db.TestCorrectionReturns.CountAsync());
        }
        finally
        {
            Npgsql.NpgsqlConnection.ClearAllPools();
            await using var drop = new Npgsql.NpgsqlCommand($"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)", connection); await drop.ExecuteNonQueryAsync();
        }
    }
    [Theory]
    [InlineData("ProductReturn", "CanReturnProductTestForCorrection")]
    [InlineData("RawMaterialReturn", "CanReturnRawMaterialTestForCorrection")]
    [InlineData("PackagingMaterialReturn", "CanReturnPackagingMaterialTestForCorrection")]
    [InlineData("ProductHistory", "CanViewProductTestCorrectionHistory")]
    [InlineData("RawMaterialHistory", "CanViewRawMaterialTestCorrectionHistory")]
    [InlineData("PackagingMaterialHistory", "CanViewPackagingMaterialTestCorrectionHistory")]
    public void Endpoints_name_the_specific_permission(string action, string key)
    {
        var method = typeof(TestCorrectionController).GetMethod(action);
        Assert.Equal(key, Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }
}

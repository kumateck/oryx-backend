using APP.Repository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.ProductAnalyticalRawData;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.ProductStandardTestProcedures;
using DOMAIN.Entities.QualityRoutines;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Routes;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;
using Xunit;
using Route = DOMAIN.Entities.Routes.Route;

namespace APP.Tests.Repository;

public sealed class ProductProcedureCopyTests
{
    private sealed class Actor(Guid id) : ICurrentUserService { public Guid? UserId => id; public Guid? DepartmentId => null; public string DepartmentType => ""; }
    private readonly Guid actor = Guid.NewGuid();
    private ApplicationDbContext db;
    private readonly IMapper mapper = new MapperConfiguration(c => {
        c.CreateMap<CreateRouteRequest, Route>(); c.CreateMap<CreateRouteResponsibleUser, RouteResponsibleUser>();
        c.CreateMap<CreateRouteResponsibleRole, RouteResponsibleRole>(); c.CreateMap<CreateRouteResource, RouteResource>(); c.CreateMap<CreateRouteWorkCenter, RouteWorkCenter>();
    }).CreateMapper();
    public ProductProcedureCopyTests() => db = new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor(actor));
    private ProductRepository Repository => new(db, mapper);
    private async Task<(Product Target, ProductAnalyticalRawData Source, ProductAnalyticalRawData Destination, Guid ResponsibleRole, Guid Operation)> Seed(params string[] keys)
    {
        var role = new Role { Id = Guid.NewGuid(), Name = "QC officer" }; var operation = new Operation { Id = Guid.NewGuid(), Name = "Bulk analysis" };
        var person = new User { Id = actor, UserName = "qc.operator" };
        db.AddRange(role, person, operation); db.UserRoles.Add(new() { UserId = actor, RoleId = role.Id }); var claimId = 1;
        foreach (var key in keys) { var claim = new IdentityRoleClaim<Guid> { Id = claimId++, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = key };
            db.RoleClaims.Add(claim); db.PermissionTypes.Add(new() { Id = Guid.NewGuid(), Key = key, RoleClaimId = claim.Id, Type = "Access" }); }
        var source = new Product { Id = Guid.NewGuid(), Name = "Source", Code = "SRC" }; var target = new Product { Id = Guid.NewGuid(), Name = "Target", Code = "DST" };
        var sourceStp = new ProductStandardTestProcedure { Id = Guid.NewGuid(), Product = source, StpNumber = "STP-SRC" };
        var targetStp = new ProductStandardTestProcedure { Id = Guid.NewGuid(), Product = target, StpNumber = "STP-DST" };
        var form = new Form { Id = Guid.NewGuid(), Name = "Analysis worksheet" };
        var sourceArd = new ProductAnalyticalRawData { Id = Guid.NewGuid(), ProductStandardTestProcedure = sourceStp, Stage = TestStage.Bulk, AnalysisType = AnalysisType.Chemical, Form = form };
        var destinationArd = new ProductAnalyticalRawData { Id = Guid.NewGuid(), ProductStandardTestProcedure = targetStp, Stage = TestStage.Bulk, AnalysisType = AnalysisType.Chemical, Form = form };
        db.AddRange(source, target, sourceStp, targetStp, form, sourceArd, destinationArd); await db.SaveChangesAsync();
        return (target, sourceArd, destinationArd, role.Id, operation.Id);
    }
    private List<CreateRouteRequest> Rows(Guid operation, Guid role, Guid? ard) => [new() { OperationId = operation, Order = 1, IsCritical = true,
        ResponsibleUsers = [new() { UserId = actor, Action = OperationAction.StockRequisitionOrIssue }, new() { UserId = actor, Action = OperationAction.Atr, ProductAnalyticalRawDataId = ard }],
        ResponsibleRoles = [new() { RoleId = role, Action = OperationAction.Atr, ProductAnalyticalRawDataId = ard }, new() { RoleId = role, Action = OperationAction.FullReturn }] }];

    [Theory]
    [InlineData(TestStage.Intermediate)] [InlineData(TestStage.Bulk)] [InlineData(TestStage.Finished)]
    public async Task Copy_selects_the_target_ARD_at_the_same_stage_and_preserves_every_user_and_role_action(TestStage stage)
    {
        var f = await Seed(PermissionKeys.CanAddProductionProductProcedure); f.Source.Stage = stage; f.Destination.Stage = stage;
        db.ProductAnalyticalRawData.Add(new() { Id = Guid.NewGuid(), Stage = stage, AnalysisType = AnalysisType.Microbial, ProductStandardTestProcedure = f.Destination.ProductStandardTestProcedure, Form = f.Destination.Form });
        await db.SaveChangesAsync(); var request = Rows(f.Operation, f.ResponsibleRole, f.Source.Id);
        var result = await Repository.CreateRoute(request, f.Target.Id, actor);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        db.ChangeTracker.Clear(); var route = await db.Routes.Include(r => r.ResponsibleUsers).Include(r => r.ResponsibleRoles).SingleAsync();
        Assert.Equal(new[] { OperationAction.StockRequisitionOrIssue, OperationAction.Atr }, route.ResponsibleUsers.Select(p => p.Action).Order());
        Assert.Equal(new[] { OperationAction.FullReturn, OperationAction.Atr }, route.ResponsibleRoles.Select(p => p.Action).Order());
        Assert.Equal(f.Destination.Id, route.ResponsibleUsers.Single(p => p.Action == OperationAction.Atr).ProductAnalyticalRawDataId);
        Assert.Equal(f.Destination.Id, route.ResponsibleRoles.Single(p => p.Action == OperationAction.Atr).ProductAnalyticalRawDataId);
        Assert.Equal(f.Source.Id, request[0].ResponsibleRoles[0].ProductAnalyticalRawDataId); // no source request mutation
    }
    [Theory]
    [InlineData("missing")] [InlineData("ambiguous")] [InlineData("deleted-source")] [InlineData("unselected")]
    public async Task Invalid_target_stage_keeps_the_existing_procedure(string problem)
    {
        var f = await Seed(PermissionKeys.CanEditProductionProductProcedure);
        var old = new Route { Id = Guid.NewGuid(), ProductId = f.Target.Id, OperationId = f.Operation, IsCritical = true, Order = 1 };
        db.Routes.Add(old);
        if (problem == "missing") f.Destination.DeletedAt = DateTime.UtcNow;
        if (problem == "deleted-source") f.Source.DeletedAt = DateTime.UtcNow;
        if (problem == "ambiguous") db.ProductAnalyticalRawData.Add(new() { Id = Guid.NewGuid(), Stage = TestStage.Bulk, AnalysisType = AnalysisType.Chemical,
            ProductStandardTestProcedure = f.Destination.ProductStandardTestProcedure, Form = f.Destination.Form });
        await db.SaveChangesAsync();
        var result = await Repository.CreateRoute(Rows(f.Operation, f.ResponsibleRole, problem == "unselected" ? null : f.Source.Id), f.Target.Id, actor);
        Assert.True(result.IsFailure); Assert.StartsWith("Route.Atr", result.Error.Code);
        Assert.Equal(old.Id, (await db.Routes.SingleAsync()).Id); Assert.Empty(db.RouteResponsibleUsers);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Copy_uses_only_the_permissions_for_the_current_product_page(bool qa)
    {
        var key = qa ? PermissionKeys.CanAddQaProductProcedure : PermissionKeys.CanAddProductionProductProcedure;
        var f = await Seed(key); var request = Rows(f.Operation, f.ResponsibleRole, f.Source.Id);
        Assert.Equal(ErrorType.Forbidden, (await Repository.CreateRoute(request, f.Target.Id, actor, qa ? null : "qa")).Error.Type);
        Assert.Empty(db.Routes);
        Assert.True((await Repository.CreateRoute(request, f.Target.Id, actor, qa ? "qa" : null)).IsSuccess);
        db.RoleClaims.RemoveRange(db.RoleClaims); await db.SaveChangesAsync();
        Assert.Equal(ErrorType.Forbidden, (await Repository.CreateRoute(request, f.Target.Id, actor, qa ? "qa" : null)).Error.Type);
    }
    [Fact]
    public async Task Saving_the_same_products_selected_stage_does_not_replace_it_with_another_document()
    {
        var f = await Seed(PermissionKeys.CanAddProductionProductProcedure);
        db.ProductAnalyticalRawData.Add(new() { Id = Guid.NewGuid(), Stage = TestStage.Bulk, AnalysisType = AnalysisType.Chemical,
            ProductStandardTestProcedure = f.Destination.ProductStandardTestProcedure, Form = f.Destination.Form });
        await db.SaveChangesAsync();
        Assert.True((await Repository.CreateRoute(Rows(f.Operation, f.ResponsibleRole, f.Destination.Id), f.Target.Id, actor)).IsSuccess);
        Assert.Equal(f.Destination.Id, (await db.RouteResponsibleRoles.SingleAsync(p => p.Action == OperationAction.Atr)).ProductAnalyticalRawDataId);
    }
    [ProcedureCopyPostgresFact]
    public async Task PostgreSQL_maps_ATR_stages_and_rolls_back_the_whole_replacement_if_a_write_fails()
    {
        var connection = Environment.GetEnvironmentVariable("ORYX_PROCEDURE_COPY_TEST_CONNECTION")!;
        var name = "oryx_procedure_copy_" + Guid.NewGuid().ToString("N");
        await using var admin = new Npgsql.NpgsqlConnection(connection); await admin.OpenAsync();
        await new Npgsql.NpgsqlCommand($"CREATE DATABASE {name}", admin).ExecuteNonQueryAsync();
        try
        {
            await db.DisposeAsync();
            var testConnection = new Npgsql.NpgsqlConnectionStringBuilder(connection) { Database = name, Pooling = false }.ConnectionString;
            db = new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(testConnection).Options, new Actor(actor));
            await db.Database.EnsureCreatedAsync();
            var f = await Seed(PermissionKeys.CanAddProductionProductProcedure, PermissionKeys.CanEditProductionProductProcedure);
            Assert.True((await Repository.CreateRoute(Rows(f.Operation, f.ResponsibleRole, f.Source.Id), f.Target.Id, actor)).IsSuccess);
            db.ChangeTracker.Clear();
            var original = await db.Routes.Include(r => r.ResponsibleUsers).SingleAsync();
            Assert.Equal(f.Destination.Id, original.ResponsibleUsers.Single(p => p.Action == OperationAction.Atr).ProductAnalyticalRawDataId);
            await db.Database.ExecuteSqlRawAsync("""
                CREATE FUNCTION reject_test_procedure_insert() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN IF NEW."Order" = 99 THEN RAISE EXCEPTION 'deliberate procedure write failure'; END IF; RETURN NEW; END $$;
                CREATE TRIGGER reject_test_procedure_insert BEFORE INSERT ON "Routes" FOR EACH ROW EXECUTE FUNCTION reject_test_procedure_insert();
                """);
            var replacement = Rows(f.Operation, f.ResponsibleRole, f.Source.Id); replacement[0].Order = 99;
            await Assert.ThrowsAsync<DbUpdateException>(() => Repository.CreateRoute(replacement, f.Target.Id, actor));
            db.ChangeTracker.Clear();
            var retained = await db.Routes.Include(r => r.ResponsibleUsers).Include(r => r.ResponsibleRoles).SingleAsync();
            Assert.Equal(original.Id, retained.Id); Assert.Equal(1, retained.Order);
            Assert.Equal(2, retained.ResponsibleUsers.Count); Assert.Equal(2, retained.ResponsibleRoles.Count);
        }
        finally { await db.DisposeAsync(); await new Npgsql.NpgsqlCommand($"DROP DATABASE {name} WITH (FORCE)", admin).ExecuteNonQueryAsync(); }
    }

}

public sealed class ProcedureCopyPostgresFactAttribute : FactAttribute
{
    public ProcedureCopyPostgresFactAttribute() { if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORYX_PROCEDURE_COPY_TEST_CONNECTION"))) Skip = "Requires a disposable PostgreSQL connection."; }
}

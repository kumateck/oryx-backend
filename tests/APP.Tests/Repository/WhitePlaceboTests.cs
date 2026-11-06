using System.Reflection;
using API.Controllers;
using APP.Repository;
using APP.Services.Materials;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Materials.WhitePlacebo;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Requisitions.Request;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using SHARED;
using Xunit;

namespace APP.Tests.Repository;

public sealed class WhitePlaceboTests
{
    private static readonly string[] AllKeys = [PermissionKeys.CanViewWhitePlacebo, PermissionKeys.CanEditWhitePlaceboRecipe,
        PermissionKeys.CanRequestWhitePlaceboIngredients, PermissionKeys.CanIssueWhitePlaceboIngredients, PermissionKeys.CanRecordWhitePlaceboOutput];

    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? DepartmentId { get; } = Guid.NewGuid();
        public string DepartmentType => nameof(SHARED.DepartmentType.Production);
    }
    private sealed class Fixture : IAsyncDisposable
    {
        public Actor Actor { get; } = new();
        public ApplicationDbContext Db { get; }
        public WhitePlaceboService Service { get; }
        public Guid Output { get; } = Guid.NewGuid();
        public Guid Ingredient { get; } = Guid.NewGuid();
        public Guid Shelf { get; } = Guid.NewGuid();
        public Guid Unit { get; } = Guid.NewGuid();
        public Guid Batch { get; } = Guid.NewGuid();
        public Fixture()
        {
            Db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString(), options => options.EnableNullChecks(false)).Options, Actor);
            var approvals = new ApprovalRepository(Db, null!, null!, new MemoryCache(new MemoryCacheOptions()),
                NullLogger<ApprovalRepository>.Instance, null!, null!);
            Service = new WhitePlaceboService(Db, Actor, approvals);
        }
        public async Task Seed()
        {
            var role = Guid.NewGuid();
            Db.Departments.Add(new Department { Id = Actor.DepartmentId!.Value, Type = DepartmentType.Production, Name = "Tablets" });
            Db.Users.Add(new User { Id = Actor.UserId!.Value, DepartmentId = Actor.DepartmentId, UserName = "production" });
            Db.Roles.Add(new Role { Id = role, Name = "Test operator" });
            Db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = Actor.UserId.Value, RoleId = role });
            var grants = AllKeys;
            for (var index = 0; index < grants.Length; index++)
            {
                Db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = index + 1, RoleId = role, ClaimType = AppConstants.Permission, ClaimValue = grants[index] });
                Db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = index + 1, Key = grants[index], Type = "Action" });
            }
            Db.UnitOfMeasures.Add(new UnitOfMeasure { Id = Unit, Name = "Milligram", Symbol = "mg", Category = UnitOfMeasureCategory.Weight });
            Db.Materials.AddRange(new Material { Id = Output, Name = "White Placebo", Code = "WP001", Kind = MaterialKind.Raw, IsWhitePlacebo = true },
                new Material { Id = Ingredient, Name = "Illustrative excipient", Code = "RM001", Kind = MaterialKind.Raw });
            Db.MaterialDepartments.AddRange(new MaterialDepartment { Id = Guid.NewGuid(), DepartmentId = Actor.DepartmentId.Value, MaterialId = Output, UoMId = Unit },
                new MaterialDepartment { Id = Guid.NewGuid(), DepartmentId = Actor.DepartmentId.Value, MaterialId = Ingredient, UoMId = Unit });
            var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Raw store", DepartmentId = Actor.DepartmentId, Type = WarehouseType.RawMaterialStorage };
            var location = new WarehouseLocation { Id = Guid.NewGuid(), Name = "Room", Warehouse = warehouse };
            var rack = new WarehouseLocationRack { Id = Guid.NewGuid(), Name = "Rack", WarehouseLocation = location };
            var shelf = new WarehouseLocationShelf { Id = Guid.NewGuid(), Name = "Shelf A", WarehouseLocationRack = rack };
            Db.WarehouseLocationShelves.Add(shelf);
            Db.MaterialBatches.Add(new MaterialBatch { Id = Batch, MaterialId = Ingredient, UoMId = Unit, BatchNumber = "EXC-01",
                TotalQuantity = 100, QuantityAssigned = 100, Status = BatchStatus.Available, ExpiryDate = DateTime.UtcNow.AddDays(30) });
            Db.ShelfMaterialBatches.Add(new ShelfMaterialBatch { Id = Shelf, WarehouseLocationShelf = shelf, MaterialBatchId = Batch, UoMId = Unit, Quantity = 100 });
            Db.WhitePlaceboRecipes.Add(new WhitePlaceboRecipe { Id = Guid.NewGuid(), DepartmentId = Actor.DepartmentId.Value,
                MaterialId = Output, UoMId = Unit, Revision = 1, Reference = "Approved site reference", Instructions = "Site instructions",
                StorageConditions = "Site-approved conditions", HoldHours = 24, ExpectedQuantity = 10,
                Ingredients = [new WhitePlaceboRecipeIngredient { Id = Guid.NewGuid(), MaterialId = Ingredient, UoMId = Unit, Quantity = 10 }] });
            await Db.SaveChangesAsync();
        }
        public async Task Revoke(string key)
        {
            var claim = await Db.RoleClaims.SingleAsync(item => item.ClaimValue == key);
            Db.PermissionTypes.RemoveRange(await Db.PermissionTypes.Where(item => item.RoleClaimId == claim.Id).ToListAsync());
            Db.RoleClaims.Remove(claim);
            await Db.SaveChangesAsync();
        }
        public SaveWhitePlaceboRecipe Recipe() => new() { MaterialId = Output, Revision = 1, Reference = "Approved revision two",
            Instructions = "Revised site instructions", StorageConditions = "Revised site conditions", HoldHours = 48,
            ExpectedQuantity = 20, Ingredients = [new WhitePlaceboIngredientRequest(Ingredient, 20)] };
        public CreateWhitePlaceboPreparation Request() => new() { Id = Guid.NewGuid(), BatchNumber = $"WP-{Guid.NewGuid():N}", RecipeRevision = 1,
            ExpectedQuantity = 10, Inputs = [new WhitePlaceboInputRequest(Shelf, 10)], Notes = "" };
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    [Fact]
    public async Task CompleteJourney_ConsumesInputsOnce_PostsActualOutputInQuarantine()
    {
        await using var fixture = new Fixture(); await fixture.Seed();
        var request = fixture.Request();
        var created = await fixture.Service.Create(request); Assert.True(created.IsSuccess);
        Assert.Equal(100, (await fixture.Db.ShelfMaterialBatches.SingleAsync()).Quantity);
        Assert.True((await fixture.Service.Create(request)).IsSuccess);
        Assert.Single(await fixture.Db.Requisitions.ToListAsync());
        Assert.True((await fixture.Service.Issue(created.Value)).IsSuccess);
        Assert.True((await fixture.Service.Issue(created.Value)).IsSuccess);
        Assert.Equal(90, (await fixture.Db.ShelfMaterialBatches.SingleAsync()).Quantity);
        Assert.Equal(10, (await fixture.Db.MaterialBatches.SingleAsync()).ConsumedQuantity);
        Assert.Single(await fixture.Db.MaterialBatchEvents.ToListAsync());
        Assert.Single(await fixture.Db.StockRequisitionIssues.ToListAsync());
        var output = new RecordWhitePlaceboOutput { Quantity = 9.8m, PreparedAt = DateTime.UtcNow, Notes = "Net weight checked; handling loss recorded" };
        var posted = await fixture.Service.RecordOutput(created.Value, output); Assert.True(posted.IsSuccess);
        Assert.True((await fixture.Service.RecordOutput(created.Value, output)).IsSuccess);
        var batch = await fixture.Db.MaterialBatches.SingleAsync(item => item.MaterialId == fixture.Output);
        Assert.Equal(9.8m, batch.TotalQuantity); Assert.Equal(BatchStatus.Quarantine, batch.Status);
        Assert.Equal(output.PreparedAt.AddHours(24), batch.ExpiryDate);
        Assert.NotNull(batch.GrnId); Assert.Single(await fixture.Db.Grns.IgnoreQueryFilters().ToListAsync());
        Assert.Equal(0, batch.QuantityAssigned);
        output.Quantity = 12;
        Assert.True((await fixture.Service.RecordOutput(created.Value, output)).IsFailure);
    }

    [Fact]
    public async Task ApprovalAndStockChanges_BlockIssueWithoutPartialConsumption()
    {
        await using var fixture = new Fixture(); await fixture.Seed();
        var created = await fixture.Service.Create(fixture.Request());
        var request = await fixture.Db.Requisitions.SingleAsync(); request.Approved = false; await fixture.Db.SaveChangesAsync();
        Assert.True((await fixture.Service.Issue(created.Value)).IsFailure);
        request.Approved = true; (await fixture.Db.MaterialBatches.SingleAsync()).Status = BatchStatus.Frozen; await fixture.Db.SaveChangesAsync();
        Assert.True((await fixture.Service.Issue(created.Value)).IsFailure);
        Assert.Equal(100, (await fixture.Db.ShelfMaterialBatches.SingleAsync()).Quantity);
        Assert.Empty(await fixture.Db.MaterialBatchEvents.ToListAsync());
        Assert.Empty(await fixture.Db.StockRequisitionIssues.ToListAsync());
    }

    [Fact]
    public async Task DuplicatePayloadAndMissingOrReservedStock_AreRejected()
    {
        await using var fixture = new Fixture(); await fixture.Seed();
        var request = fixture.Request(); Assert.True((await fixture.Service.Create(request)).IsSuccess);
        request.ExpectedQuantity = 20; Assert.True((await fixture.Service.Create(request)).IsFailure);
        var second = fixture.Request(); second.Inputs = [new WhitePlaceboInputRequest(Guid.NewGuid(), 10)];
        Assert.True((await fixture.Service.Create(second)).IsFailure);
        fixture.Db.MaterialBatchReservedQuantities.Add(new MaterialBatchReservedQuantity { Id = Guid.NewGuid(), MaterialBatchId = fixture.Batch, Quantity = 95 });
        await fixture.Db.SaveChangesAsync();
        Assert.True((await fixture.Service.Issue(request.Id)).IsFailure);
        Assert.Equal(100, (await fixture.Db.ShelfMaterialBatches.SingleAsync()).Quantity);
    }

    [Fact]
    public async Task RevokedPermissionAndForeignDepartment_AreRefused()
    {
        await using var fixture = new Fixture(); await fixture.Seed();
        var request = fixture.Request(); Assert.True((await fixture.Service.Create(request)).IsSuccess);
        var claim = await fixture.Db.RoleClaims.SingleAsync(item => item.ClaimValue == PermissionKeys.CanIssueWhitePlaceboIngredients);
        fixture.Db.RoleClaims.Remove(claim); await fixture.Db.SaveChangesAsync();
        Assert.True((await fixture.Service.Issue(request.Id)).IsFailure);
        (await fixture.Db.Users.SingleAsync()).DepartmentId = Guid.NewGuid(); await fixture.Db.SaveChangesAsync();
        Assert.True((await fixture.Service.Get(request.Id)).IsFailure);
    }

    [Fact]
    public async Task OutputCannotBePostedBeforeIssue_OrThroughGenericBatchCreation()
    {
        await using var fixture = new Fixture(); await fixture.Seed();
        var request = fixture.Request(); Assert.True((await fixture.Service.Create(request)).IsSuccess);
        Assert.True((await fixture.Service.RecordOutput(request.Id, new RecordWhitePlaceboOutput {
            Quantity = 10, PreparedAt = DateTime.UtcNow, Notes = "Checked" })).IsFailure);
        fixture.Db.MaterialBatches.Add(new MaterialBatch { Id = Guid.NewGuid(), MaterialId = fixture.Output,
            BatchNumber = "UNLINKED", UoMId = fixture.Unit, TotalQuantity = 10, ExpiryDate = DateTime.UtcNow.AddDays(1) });
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task PreparationRequest_CannotBeEditedDeletedOrIssuedWithDifferentApprovedQuantities()
    {
        await using var fixture = new Fixture(); await fixture.Seed();
        var request = fixture.Request(); Assert.True((await fixture.Service.Create(request)).IsSuccess);
        var preparation = await fixture.Db.WhitePlaceboPreparations.SingleAsync();
        var repository = new RequisitionRepository(fixture.Db, null!, null!, null!, null!, null!, null!, null!, null!, null!, identity: fixture.Actor);
        Assert.True((await repository.UpdateRequisition(new CreateRequisitionRequest(), preparation.RequisitionId, fixture.Actor.UserId!.Value)).IsFailure);
        Assert.True((await repository.DeleteRequisition(preparation.RequisitionId, fixture.Actor.UserId.Value)).IsFailure);
        (await fixture.Db.Requisitions.Include(item => item.Items).SingleAsync()).Items[0].Quantity = 11;
        await fixture.Db.SaveChangesAsync();
        Assert.True((await fixture.Service.Issue(request.Id)).IsFailure);
        Assert.Equal(100, (await fixture.Db.ShelfMaterialBatches.SingleAsync()).Quantity);
    }

    [Fact]
    public async Task RecipeRevisionChanges_PreserveExistingPreparationSnapshot()
    {
        await using var fixture = new Fixture(); await fixture.Seed();
        var request = fixture.Request(); Assert.True((await fixture.Service.Create(request)).IsSuccess);
        var saved = await fixture.Service.SaveRecipe(new SaveWhitePlaceboRecipe { MaterialId = fixture.Output, Revision = 1,
            Reference = "Approved revision two", Instructions = "Revised site instructions", StorageConditions = "Revised site conditions",
            HoldHours = 48, ExpectedQuantity = 20, Ingredients = [new WhitePlaceboIngredientRequest(fixture.Ingredient, 20)] });
        Assert.True(saved.IsSuccess);
        fixture.Db.ChangeTracker.Clear();
        var current = await fixture.Service.Workspace();
        Assert.Single(current.Value.Recipe.Ingredients);
        Assert.Equal(20, current.Value.Recipe.Ingredients[0].Quantity);
        var history = await fixture.Service.Get(request.Id);
        Assert.Equal(1, history.Value.RecipeRevision); Assert.Equal(24, history.Value.HoldHours);
        Assert.Equal("Approved site reference", history.Value.RecipeReference);
        Assert.True((await fixture.Service.Create(fixture.Request())).IsFailure);
    }

    [Fact]
    public async Task PurchasingWhitePlacebo_IsRefusedByBackend()
    {
        await using var fixture = new Fixture(); await fixture.Seed();
        var repository = new RequisitionRepository(fixture.Db, null!, null!, null!, null!, null!, null!, null!, null!, null!);
        var result = await repository.CreateRequisition(new CreateRequisitionRequest { RequisitionType = RequisitionType.Purchase,
            Items = [new CreateRequisitionItemRequest { MaterialId = fixture.Output, Quantity = 10 }] }, fixture.Actor.UserId!.Value);
        Assert.True(result.IsFailure); Assert.Equal("WhitePlacebo.InternalOnly", result.Error.Code);
        Assert.Empty(await fixture.Db.Requisitions.ToListAsync());
    }

    [Theory]
    [InlineData("Workspace", null)]
    [InlineData("List", null)]
    [InlineData("Get", null)]
    [InlineData("SaveRecipe", PermissionKeys.CanEditWhitePlaceboRecipe)]
    [InlineData("Create", PermissionKeys.CanRequestWhitePlaceboIngredients)]
    [InlineData("Issue", PermissionKeys.CanIssueWhitePlaceboIngredients)]
    [InlineData("Output", PermissionKeys.CanRecordWhitePlaceboOutput)]
    public void Each_endpoint_needs_the_view_key_and_its_own_task_key(string action, string key)
    {
        Assert.Equal(PermissionKeys.CanViewWhitePlacebo,
            Assert.Single(typeof(WhitePlaceboController).GetCustomAttributes<AuthorizeAttribute>()).Policy);
        var method = typeof(WhitePlaceboController).GetMethod(action)!;
        var policies = method.GetCustomAttributes<AuthorizeAttribute>().Select(attribute => attribute.Policy).ToList();
        if (key is null) Assert.Empty(policies); else Assert.Equal(key, Assert.Single(policies));
    }

    [Fact]
    public void White_placebo_keys_are_registered_once()
    {
        var catalog = PermissionUtils.GeneratePermissions().Select(permission => permission.Key).ToList();
        Assert.All(AllKeys, key => Assert.Single(catalog, registered => registered == key));
    }

    [Fact]
    public async Task Without_the_view_key_every_action_is_refused()
    {
        await using var fixture = new Fixture(); await fixture.Seed();
        await fixture.Revoke(PermissionKeys.CanViewWhitePlacebo);
        Assert.Equal(ErrorType.Forbidden, (await fixture.Service.Workspace()).Errors[0].Type);
        Assert.Equal(ErrorType.Forbidden, (await fixture.Service.List()).Errors[0].Type);
        Assert.Equal(ErrorType.Forbidden, (await fixture.Service.SaveRecipe(fixture.Recipe())).Errors[0].Type);
        Assert.Equal(ErrorType.Forbidden, (await fixture.Service.Create(fixture.Request())).Errors[0].Type);
        Assert.Empty(await fixture.Db.WhitePlaceboPreparations.ToListAsync());
    }

    [Fact]
    public async Task Recipe_edit_is_allowed_with_its_key_and_refused_without_it()
    {
        await using var fixture = new Fixture(); await fixture.Seed();
        Assert.True((await fixture.Service.Workspace()).IsSuccess);
        await fixture.Revoke(PermissionKeys.CanEditWhitePlaceboRecipe);
        Assert.Equal(ErrorType.Forbidden, (await fixture.Service.SaveRecipe(fixture.Recipe())).Errors[0].Type);
        Assert.Equal(1, (await fixture.Db.WhitePlaceboRecipes.SingleAsync()).Revision);
        Assert.True((await fixture.Service.Workspace()).IsSuccess);
    }

    [Fact]
    public async Task Requesting_ingredients_is_refused_without_its_key()
    {
        await using var fixture = new Fixture(); await fixture.Seed();
        await fixture.Revoke(PermissionKeys.CanRequestWhitePlaceboIngredients);
        Assert.Equal(ErrorType.Forbidden, (await fixture.Service.Create(fixture.Request())).Errors[0].Type);
        Assert.Empty(await fixture.Db.Requisitions.ToListAsync());
    }

    [Fact]
    public async Task Issuing_is_refused_without_its_key_and_leaves_stock_untouched()
    {
        await using var fixture = new Fixture(); await fixture.Seed();
        var request = fixture.Request(); Assert.True((await fixture.Service.Create(request)).IsSuccess);
        await fixture.Revoke(PermissionKeys.CanIssueWhitePlaceboIngredients);
        Assert.Equal(ErrorType.Forbidden, (await fixture.Service.Issue(request.Id)).Errors[0].Type);
        Assert.Equal(100, (await fixture.Db.ShelfMaterialBatches.SingleAsync()).Quantity);
        Assert.Null((await fixture.Db.WhitePlaceboPreparations.SingleAsync()).IssuedAt);
    }

    [Fact]
    public async Task Recording_output_is_refused_without_its_key()
    {
        await using var fixture = new Fixture(); await fixture.Seed();
        var request = fixture.Request(); Assert.True((await fixture.Service.Create(request)).IsSuccess);
        Assert.True((await fixture.Service.Issue(request.Id)).IsSuccess);
        await fixture.Revoke(PermissionKeys.CanRecordWhitePlaceboOutput);
        var output = new RecordWhitePlaceboOutput { Quantity = 9.8m, PreparedAt = DateTime.UtcNow, Notes = "Weighed" };
        Assert.Equal(ErrorType.Forbidden, (await fixture.Service.RecordOutput(request.Id, output)).Errors[0].Type);
        Assert.Null((await fixture.Db.WhitePlaceboPreparations.SingleAsync()).OutputBatchId);
        Assert.DoesNotContain(await fixture.Db.MaterialBatches.ToListAsync(), batch => batch.MaterialId == fixture.Output);
    }

    [Fact]
    public async Task Preparation_requests_are_hidden_from_the_requisition_view_without_the_white_placebo_key()
    {
        await using var fixture = new Fixture(); await fixture.Seed();
        var request = fixture.Request(); Assert.True((await fixture.Service.Create(request)).IsSuccess);
        var preparation = await fixture.Db.WhitePlaceboPreparations.SingleAsync();
        var repository = new RequisitionRepository(fixture.Db, null!, null!, null!, null!, null!, null!, null!, null!, null!, identity: fixture.Actor);
        await fixture.Revoke(PermissionKeys.CanViewWhitePlacebo);
        var refused = await repository.GetRequisition(preparation.RequisitionId, fixture.Actor.UserId!.Value);
        Assert.Equal(ErrorType.Forbidden, refused.Errors[0].Type);
    }
}

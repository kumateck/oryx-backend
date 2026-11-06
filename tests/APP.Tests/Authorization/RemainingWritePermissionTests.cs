using System.Reflection;
using API.Authorization;
using API.Controllers;
using APP.Services.Stores;
using APP.Utils;
using DOMAIN.Entities.DamagedStocks;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>The last fixed-key writes, the per-store item checks, and the actions marked as checking their own keys (audit phase 4).</summary>
public sealed class RemainingWritePermissionTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => Guid.Empty;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    private static ApplicationDbContext Db() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor());

    [Theory]
    [InlineData(typeof(CustomerController), "DeleteCustomer", "CanDeleteCustomer")]
    [InlineData(typeof(StockAdjustmentController), "CreateStockAdjustment", "CanCreateMaterialStockAdjustment")]
    [InlineData(typeof(TicketController), "CreateTicket", "CanCreateTicket")]
    [InlineData(typeof(MicrobialRequirementController), "Create", "CanManageMicrobialRequirements")]
    [InlineData(typeof(CompanyWorkingDaysController), "CreateWorkingDays", "CanCreateWorkingDays")]
    [InlineData(typeof(CommercialCertificateController), "GenerateForProduct", "CanCheckProductTest")]
    [InlineData(typeof(ProductAnalyticalRawDataController), "StartTestForMaterialBatch", "CanStartProductTest")]
    [InlineData(typeof(ItemGrnController), "DeleteItemGrn", PermissionKeys.CanDeleteItemGrn)]
    public void A_write_needs_its_task_key(Type controller, string action, string key) =>
        Assert.Equal(key, Assert.Single(controller.GetMethods().Where(method => method.Name == action).SelectMany(method => method.GetCustomAttributes<AuthorizeAttribute>())).Policy);

    [Fact]
    public async Task Damaged_stock_uses_the_key_for_the_item_store()
    {
        var db = Db();
        var item = new Item { Id = Guid.NewGuid(), Name = "Reagent A", Store = Store.ReagentStore };
        var damaged = new DamagedStock { Id = Guid.NewGuid(), ItemId = item.Id };
        db.Items.Add(item);
        db.DamagedStocks.Add(damaged);
        await db.SaveChangesAsync();
        Assert.Equal([Store.ReagentStore], await StoreItemPermissionAttribute.Stores(db, new Dictionary<string, object?> { ["id"] = damaged.Id }));
        Assert.Equal([Store.ReagentStore], await StoreItemPermissionAttribute.Stores(db, new Dictionary<string, object?> { ["request"] = new CreateDamagedStockRequest { ItemId = item.Id } }));
        var attribute = Assert.Single(typeof(DamagedStocksController).GetMethod("DeleteDamagedStock")!.GetCustomAttributes<StoreItemPermissionAttribute>());
        Assert.Equal((StoreWorkflow.DamageMissing, StoreWorkflowAction.Delete), (attribute.Workflow, attribute.Action));
        Assert.Contains("CanDeleteReagentStoreDamageMissingItemRecord", attribute.AllKeys);
    }

    [Fact]
    public async Task A_sampling_names_the_kind_of_its_batch()
    {
        var db = Db();
        var foil = new Material { Id = Guid.NewGuid(), Code = "PM-1", Name = "Foil", Kind = MaterialKind.Package };
        var batch = new MaterialBatch { Id = Guid.NewGuid(), MaterialId = foil.Id };
        var sampling = new DOMAIN.Entities.MaterialSampling.MaterialSampling { Id = Guid.NewGuid(), MaterialBatchId = batch.Id };
        db.Materials.Add(foil);
        db.MaterialBatches.Add(batch);
        db.MaterialSamplings.Add(sampling);
        await db.SaveChangesAsync();
        Assert.Equal([MaterialKind.Package], await MaterialKindPermissionAttribute.Kinds(db, new Dictionary<string, object> { ["materialSamplingId"] = sampling.Id }));
    }

    [Fact]
    public void Every_action_marked_as_checking_its_own_keys_says_how()
    {
        var marked = typeof(ApprovalController).Assembly.GetTypes().SelectMany(type => type.GetMethods())
            .Select(method => (Method: method, Reason: method.GetCustomAttribute<PermissionCheckedInActionAttribute>()?.Reason ?? method.GetCustomAttribute<SignedInUserAttribute>()?.Reason))
            .Where(entry => entry.Reason is not null).ToList();
        Assert.NotEmpty(marked);
        Assert.All(marked, entry => Assert.False(string.IsNullOrWhiteSpace(entry.Reason), entry.Method.Name));
        // A marker never sits beside a key: then the key is the check.
        Assert.All(marked, entry => Assert.Empty(entry.Method.GetCustomAttributes<AuthorizeAttribute>().Where(attribute => attribute.Policy is not null)));
    }
}

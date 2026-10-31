using APP.Repository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Products;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

/// <summary>List prices are set in Finance costing; the product forms no longer need to carry one.</summary>
public class ProductListPriceTests
{
    [Fact]
    public async Task CreateProduct_WithoutAPrice_LeavesItUnpriced()
    {
        await using var context = CreateContext();
        var result = await Repository(context).CreateProduct(new CreateProductRequest { Name = "Blopen", Code = "BLO-1" }, Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Empty((await context.Products.SingleAsync()).Prices);
    }

    [Fact]
    public async Task UpdateProduct_WithoutAPrice_KeepsTheCurrentListPrice()
    {
        await using var context = CreateContext();
        var cedi = new Currency { Id = Guid.NewGuid(), Name = "Cedi", Symbol = "GH₵", IsBaseCurrency = true };
        var product = new Product { Id = Guid.NewGuid(), Name = "Blopen", Code = "BLO-1", Prices = [new ProductPrices { Price = 10m, CurrencyId = cedi.Id, Date = DateTime.UtcNow.AddDays(-1) }] };
        context.AddRange(cedi, product);
        await context.SaveChangesAsync();

        var result = await Repository(context).UpdateProduct(new UpdateProductRequest { Name = "Blopen", Code = "BLO-1" }, product.Id, Guid.NewGuid());

        Assert.True(result.IsSuccess);
        var saved = await context.Products.SingleAsync();
        Assert.Equal(10m, Assert.Single(saved.Prices).Price);
    }

    [Fact]
    public void Finance_section_holds_payments_cashflow_and_costing_once_each()
    {
        var permissions = PermissionUtils.GeneratePermissions().ToList();
        foreach (var key in new[] { PermissionKeys.CanViewPayments, PermissionKeys.CanRecordPayment, PermissionKeys.CanApprovePayment,
                     PermissionKeys.CanViewCashflowReports, PermissionKeys.CanManageCosting })
            Assert.Equal(PermissionModules.Finance, Assert.Single(permissions, item => item.Key == key).Module);
        Assert.Equal(PermissionSubmodules.Costing, permissions.Single(item => item.Key == PermissionKeys.CanManageCosting).SubModule);
        // Customer profile and contacts keep their own key in the customer section.
        Assert.Equal(PermissionModules.FinishedGoodsWarehouse, permissions.Single(item => item.Key == PermissionKeys.CanManageCustomerContracts).Module);
    }

    private static ProductRepository Repository(ApplicationDbContext context)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSingleton(context);
        services.AddAutoMapper(cfg => { }, typeof(APP.Mapper.OryxMapper));
        return new ProductRepository(context, services.BuildServiceProvider().GetRequiredService<IMapper>());
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ApplicationDbContext(options, new TestCurrentUser());
    }

    private sealed class TestCurrentUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}

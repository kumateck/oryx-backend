using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class NoUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class ProductDeleteTests
{
    private static (ApplicationDbContext Db, ProductRepository Repository) Create()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new NoUser());
        var services = new ServiceCollection();
        services.AddLogging(); services.AddHttpContextAccessor(); services.AddSingleton(db);
        services.AddAutoMapper(cfg => { }, typeof(APP.Mapper.OryxMapper));
        return (db, new ProductRepository(db, services.BuildServiceProvider().GetRequiredService<IMapper>()));
    }

    [Fact]
    public async Task An_unused_product_is_deleted_by_the_actor()
    {
        var (db, repository) = Create(); await using var _ = db;
        var product = new Product { Id = Guid.NewGuid(), Code = "PDT-1", Name = "Unused" };
        db.Products.Add(product); await db.SaveChangesAsync();
        var actor = Guid.NewGuid();

        var result = await repository.DeleteProduct(product.Id, actor);

        Assert.True(result.IsSuccess);
        var stored = await db.Products.IgnoreQueryFilters().SingleAsync(item => item.Id == product.Id);
        Assert.NotNull(stored.DeletedAt);
        Assert.Equal(actor, stored.LastDeletedById);
    }

    [Fact]
    public async Task A_product_in_use_is_kept_and_the_refusal_names_what_uses_it()
    {
        var (db, repository) = Create(); await using var _ = db;
        var product = new Product { Id = Guid.NewGuid(), Code = "PDT-2", Name = "Scheduled" };
        db.Products.Add(product);
        var schedule = new ProductionSchedule { Id = Guid.NewGuid() };
        db.ProductionSchedules.Add(schedule);
        db.ProductionScheduleProducts.Add(new ProductionScheduleProduct { Id = Guid.NewGuid(), ProductionSchedule = schedule, ProductId = product.Id });
        var quotation = new CustomerQuotation { Id = Guid.NewGuid(), Code = "CQ-1", Customer = new Customer { Id = Guid.NewGuid(), Name = "Buyer" } };
        db.CustomerQuotations.Add(quotation);
        db.CustomerQuotationItems.Add(new CustomerQuotationItem { Id = Guid.NewGuid(), CustomerQuotation = quotation, ProductId = product.Id });
        await db.SaveChangesAsync();

        var result = await repository.DeleteProduct(product.Id, Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Products.InUse", result.Error.Code);
        Assert.Contains("1 production schedule", result.Error.Description);
        Assert.Contains("1 customer quotation", result.Error.Description);
        Assert.Null((await db.Products.IgnoreQueryFilters().SingleAsync(item => item.Id == product.Id)).DeletedAt);

        // Once its schedule is deleted and its quotation removed, nothing uses it and it can go.
        schedule.DeletedAt = DateTime.UtcNow;
        quotation.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        Assert.True((await repository.DeleteProduct(product.Id, Guid.NewGuid())).IsSuccess);
    }
}

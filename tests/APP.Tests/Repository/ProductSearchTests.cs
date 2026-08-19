using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.ProductStandardTestProcedures;
using DOMAIN.Entities.Products;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class FakeCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class ProductSearchTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options, new FakeCurrentUserService());
    }

    private static IMapper CreateMapper(ApplicationDbContext context)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSingleton(context);
        services.AddAutoMapper(cfg => { }, typeof(APP.Mapper.OryxMapper));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    [Fact]
    public async Task GetProducts_SearchByCode_FindsProduct()
    {
        await using var context = CreateContext();
        var mapper = CreateMapper(context);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Code = "PDT045",
            Name = "Totally Unrelated Product Name",
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var repository = new ProductRepository(context, mapper);
        var result = await repository.GetProducts(
            page: 1,
            pageSize: 50,
            searchQuery: "PDT045",
            departmentId: null,
            division: null,
            category: null
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalRecordCount);
        Assert.Equal(product.Id, Assert.Single(result.Value.Data).Id);
    }

    [Fact]
    public async Task GetProducts_SearchByUnrelatedTerm_FindsNothing()
    {
        await using var context = CreateContext();
        var mapper = CreateMapper(context);

        context.Products.Add(
            new Product { Id = Guid.NewGuid(), Code = "PDT045", Name = "Some Product" }
        );
        await context.SaveChangesAsync();

        var repository = new ProductRepository(context, mapper);
        var result = await repository.GetProducts(
            page: 1,
            pageSize: 50,
            searchQuery: "NoSuchCodeOrName",
            departmentId: null,
            division: null,
            category: null
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TotalRecordCount);
    }

    [Fact]
    public async Task GetProductStandardTestProcedures_SearchByProductCode_FindsStp()
    {
        await using var context = CreateContext();
        var mapper = CreateMapper(context);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Code = "PDT045",
            Name = "Totally Unrelated Product Name",
        };
        context.Products.Add(product);

        var stp = new ProductStandardTestProcedure
        {
            Id = Guid.NewGuid(),
            StpNumber = "QCD/STP/PD/045",
            ProductId = product.Id,
        };
        context.ProductStandardTestProcedures.Add(stp);
        await context.SaveChangesAsync();

        var repository = new ProductStandardTestProcedureRepository(context, mapper);
        var result = await repository.GetProductStandardTestProcedures(
            page: 1,
            pageSize: 50,
            searchQuery: "PDT045"
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalRecordCount);
        Assert.Equal(stp.Id, Assert.Single(result.Value.Data).Id);
    }
}

using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.ProductAnalyticalRawData;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.ProductStandardTestProcedures;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class ArdListCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class ProductArdListTemplateTests
{
    [Fact]
    public async Task The_product_ard_list_names_each_records_template()
    {
        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            new ArdListCurrentUserService());
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSingleton(context);
        services.AddAutoMapper(_ => { }, typeof(OryxMapper));
        var mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();

        var product = new Product { Id = Guid.NewGuid(), Name = "Blopen 100MG", Code = "NT023" };
        var stp = new ProductStandardTestProcedure { Id = Guid.NewGuid(), StpNumber = "QCD/STP/FP/020", Product = product };
        var form = new Form { Id = Guid.NewGuid(), Name = "Blopen bulk worksheet" };
        context.AddRange(product, stp, form,
            new ProductAnalyticalRawData { Id = Guid.NewGuid(), SpecNumber = "NMP/B/SPC/014", Stage = TestStage.Bulk, ProductStandardTestProcedure = stp, Form = form });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await new ProductAnalyticalRawDataRepository(context, mapper).GetAnalyticalRawData(1, 10, null);

        Assert.True(result.IsSuccess);
        var row = Assert.Single(result.Value.Data);
        Assert.Equal("Blopen bulk worksheet", row.Form?.Name);
    }
}

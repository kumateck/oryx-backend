using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class NoCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class RequisitionSupplierQuotationRequestTests
{
    private static IMapper CreateMapper(ApplicationDbContext context)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSingleton(context);
        services.AddAutoMapper(cfg => { }, typeof(OryxMapper));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static RequisitionRepository CreateRepository(ApplicationDbContext context) =>
        new(
            context,
            CreateMapper(context),
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            new NoOpProductionActivityStepEventPublisher()
        );

    [Fact]
    public async Task GetSuppliersWithSourceRequisitionItems_returns_not_found_when_no_request_exists()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options, new NoCurrentUserService());
        var repository = CreateRepository(context);

        Result<SupplierQuotationRequest> result = await repository.GetSuppliersWithSourceRequisitionItems(
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Supplier.QuotationRequest.NotFound", error.Code);
        Assert.Equal(ErrorType.NotFound, error.Type);
    }

    [Fact]
    public async Task GetSuppliersWithSourceRequisitionItems_keeps_sent_request_readable()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options, new NoCurrentUserService());
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Supplier" };
        context.SourceRequisitions.Add(
            new SourceRequisition
            {
                Id = Guid.NewGuid(),
                SupplierId = supplier.Id,
                Supplier = supplier,
                CreatedAt = DateTime.UtcNow.AddMinutes(-1),
                SentQuotationRequestAt = DateTime.UtcNow,
            }
        );
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).GetSuppliersWithSourceRequisitionItems(supplier.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.SentQuotationRequest);
    }

    [Fact]
    public async Task GetSuppliersWithSourceRequisitionItems_prefers_the_next_unsent_request()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options, new NoCurrentUserService());
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Supplier" };
        context.SourceRequisitions.AddRange(
            new SourceRequisition
            {
                Id = Guid.NewGuid(),
                SupplierId = supplier.Id,
                Supplier = supplier,
                CreatedAt = DateTime.UtcNow.AddMinutes(-2),
                SentQuotationRequestAt = DateTime.UtcNow.AddMinutes(-1),
            },
            new SourceRequisition
            {
                Id = Guid.NewGuid(),
                SupplierId = supplier.Id,
                Supplier = supplier,
                CreatedAt = DateTime.UtcNow,
            }
        );
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).GetSuppliersWithSourceRequisitionItems(supplier.Id);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.SentQuotationRequest);
    }

    [Fact]
    public async Task GetSupplierQuotation_returns_not_found_when_quotation_does_not_exist()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options, new NoCurrentUserService());

        var result = await CreateRepository(context).GetSupplierQuotation(Guid.NewGuid());

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Supplier.Quotation.NotFound", error.Code);
        Assert.Equal(ErrorType.NotFound, error.Type);
    }

    [Fact]
    public async Task ReceiveQuotationFromSupplier_returns_not_found_when_quotation_does_not_exist()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options, new NoCurrentUserService());

        var result = await CreateRepository(context).ReceiveQuotationFromSupplier([], Guid.NewGuid());

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Supplier.Quotation.NotFound", error.Code);
        Assert.Equal(ErrorType.NotFound, error.Type);
    }
}

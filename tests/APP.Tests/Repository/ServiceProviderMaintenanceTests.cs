using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.JobRequests;
using DOMAIN.Entities.ServiceProviders;
using DOMAIN.Entities.Services;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class ServiceProviderMaintenanceTests
{
    private static readonly Guid Actor = Guid.NewGuid();

    [Fact]
    public async Task UpdatePersistsReplacementServiceAssociationsAcrossFreshContexts()
    {
        var options = Options();
        var providerId = Guid.NewGuid();
        var oldId = Guid.NewGuid();
        var newId = Guid.NewGuid();
        await using (var context = new ApplicationDbContext(options, new CurrentUser()))
        {
            var oldService = new Service { Id = oldId, Name = "Old service" };
            var newService = new Service { Id = newId, Name = "New service" };
            context.Services.AddRange(oldService, newService);
            context.ServiceProviders.Add(new ServiceProvider
            {
                Id = providerId, Name = "Provider", Services = [oldService]
            });
            await context.SaveChangesAsync();
        }
        await using (var context = new ApplicationDbContext(options, new CurrentUser()))
        {
            var result = await Repository(context).UpdateServiceProvider(providerId,
                new CreateServiceProviderRequest
                {
                    Name = "Updated provider", Address = "Address", Phone = "1234567890",
                    Email = "provider@example.invalid", ServiceIds = [newId]
                });
            Assert.True(result.IsSuccess);
        }
        await using var verify = new ApplicationDbContext(options, new CurrentUser());
        var saved = await verify.ServiceProviders.Include(p => p.Services).SingleAsync();
        Assert.Equal("Updated provider", saved.Name);
        Assert.Equal(newId, Assert.Single(saved.Services).Id);
    }

    [Fact]
    public async Task MissingServicesDoNotRemoveTheExistingAssociations()
    {
        await using var context = new ApplicationDbContext(Options(), new CurrentUser());
        var service = new Service { Id = Guid.NewGuid(), Name = "Existing service" };
        var provider = new ServiceProvider { Id = Guid.NewGuid(), Name = "Provider", Services = [service] };
        context.Add(provider);
        await context.SaveChangesAsync();
        var result = await Repository(context).UpdateServiceProvider(provider.Id,
            new CreateServiceProviderRequest { ServiceIds = [Guid.NewGuid()] });
        Assert.False(result.IsSuccess);
        Assert.Equal(service.Id, Assert.Single(provider.Services).Id);
    }

    [Fact]
    public async Task MissingProviderReturnsNotFoundInsteadOfDereferencingNull()
    {
        await using var context = new ApplicationDbContext(Options(), new CurrentUser());
        var result = await Repository(context).DeleteServiceProvider(Guid.NewGuid(), Actor);
        Assert.False(result.IsSuccess);
        Assert.Empty(await context.ServiceProviders.ToListAsync());
    }

    [Fact]
    public async Task UnlinkedProviderIsSoftDeletedWithAuditIdentity()
    {
        await using var context = new ApplicationDbContext(Options(), new CurrentUser());
        var provider = new ServiceProvider { Id = Guid.NewGuid(), Name = "Unlinked provider" };
        context.Add(provider);
        await context.SaveChangesAsync();
        var result = await Repository(context).DeleteServiceProvider(provider.Id, Actor);
        Assert.True(result.IsSuccess);
        Assert.Empty(await context.ServiceProviders.ToListAsync());
        var historical = await context.ServiceProviders.IgnoreQueryFilters().SingleAsync();
        Assert.NotNull(historical.DeletedAt);
        Assert.Equal(Actor, historical.LastDeletedById);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public async Task HistoricalJobAndFinancialReferencesPreventDeletion(int linkType)
    {
        await using var context = new ApplicationDbContext(Options(), new CurrentUser());
        var provider = new ServiceProvider { Id = Guid.NewGuid(), Name = "Referenced provider" };
        context.Add(provider);
        var historical = DateTime.UtcNow.AddDays(-1);
        if (linkType == 0) context.JobOrderServiceProviders.Add(new JobOrderServiceProvider { Id = Guid.NewGuid(), ServiceProviderId = provider.Id, DeletedAt = historical });
        if (linkType == 1) context.JobOrderExecutions.Add(new JobOrderExecution { Id = Guid.NewGuid(), ServiceProviderId = provider.Id, DeletedAt = historical });
        if (linkType == 2) context.ServiceQuotations.Add(new ServiceQuotation { Id = Guid.NewGuid(), ServiceProviderId = provider.Id, DeletedAt = historical });
        if (linkType == 3) context.ServiceProformaInvoices.Add(new ServiceProformaInvoice { Id = Guid.NewGuid(), ServiceProviderId = provider.Id, DeletedAt = historical });
        if (linkType == 4) context.ServiceMemos.Add(new ServiceMemo { Id = Guid.NewGuid(), ServiceProviderId = provider.Id, DeletedAt = historical });
        await context.SaveChangesAsync();
        var result = await Repository(context).DeleteServiceProvider(provider.Id, Actor);
        Assert.False(result.IsSuccess);
        Assert.Null(provider.DeletedAt);
    }

    private static DbContextOptions<ApplicationDbContext> Options() =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    private static ServiceProviderRepository Repository(ApplicationDbContext context)
    {
        var config = new MapperConfiguration(c => c.CreateMap<CreateServiceProviderRequest, ServiceProvider>());
        return new ServiceProviderRepository(context, config.CreateMapper());
    }

    private sealed class CurrentUser : ICurrentUserService
    {
        public Guid? UserId => Actor;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}

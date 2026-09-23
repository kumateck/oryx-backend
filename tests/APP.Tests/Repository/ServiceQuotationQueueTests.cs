using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.JobRequests;
using ERPServiceProvider = DOMAIN.Entities.ServiceProviders.ServiceProvider;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Services;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class QueueTestCurrentUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class ServiceQuotationQueueTests
{
    [Fact]
    public async Task Pending_queues_filter_before_pagination_and_project_job_order_stage()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new ApplicationDbContext(options, new QueueTestCurrentUser());
        var service = new Service { Id = Guid.NewGuid(), Name = "Maintenance" };
        var provider = new ERPServiceProvider { Id = Guid.NewGuid(), Name = "Provider" };
        var currency = new Currency { Id = Guid.NewGuid(), Name = "Ghana Cedi" };
        context.Services.Add(service);
        context.ServiceProviders.Add(provider);
        context.Currencies.Add(currency);
        var comparison = new JobOrder { Id = Guid.NewGuid(), Status = JobOrderStatus.QuotationsReceived, Service = service, ServiceId = service.Id };
        var selected = new JobOrder { Id = Guid.NewGuid(), Status = JobOrderStatus.QuotationSelected, Service = service, ServiceId = service.Id };
        var requested = new JobOrder { Id = Guid.NewGuid(), Status = JobOrderStatus.ProformaInvoiceRequested, Service = service, ServiceId = service.Id };
        context.JobOrders.AddRange(comparison, selected, requested);
        context.ServiceQuotations.AddRange(
            new ServiceQuotation { Id = Guid.NewGuid(), JobOrderId = comparison.Id, JobOrder = comparison, ServiceProvider = provider, ServiceProviderId = provider.Id, Currency = currency, CurrencyId = currency.Id },
            new ServiceQuotation { Id = Guid.NewGuid(), JobOrderId = selected.Id, JobOrder = selected, IsSelected = true, ServiceProvider = provider, ServiceProviderId = provider.Id, Currency = currency, CurrencyId = currency.Id },
            new ServiceQuotation { Id = Guid.NewGuid(), JobOrderId = selected.Id, JobOrder = selected, ServiceProvider = provider, ServiceProviderId = provider.Id, Currency = currency, CurrencyId = currency.Id },
            new ServiceQuotation { Id = Guid.NewGuid(), JobOrderId = requested.Id, JobOrder = requested, IsSelected = true, ServiceProvider = provider, ServiceProviderId = provider.Id, Currency = currency, CurrencyId = currency.Id }
        );
        await context.SaveChangesAsync();
        Assert.Equal(4, await context.ServiceQuotations.CountAsync());
        Assert.Equal(1, await context.ServiceQuotations.CountAsync(q => q.JobOrder.Status == JobOrderStatus.QuotationsReceived));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSingleton(context);
        services.AddAutoMapper(cfg => { }, typeof(OryxMapper));
        var mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();
        var repository = new ServiceQuotationRepository(context, mapper);

        var compareQueue = await repository.GetServiceQuotations(1, 10, jobOrderStatus: JobOrderStatus.QuotationsReceived);
        var proformaQueue = await repository.GetServiceQuotations(1, 10, proformaPending: true);

        Assert.True(compareQueue.IsSuccess);
        Assert.Single(compareQueue.Value.Data);
        Assert.Equal(JobOrderStatus.QuotationsReceived, compareQueue.Value.Data.Single().JobOrderStatus);
        Assert.True(proformaQueue.IsSuccess);
        Assert.Equal(2, proformaQueue.Value.Data.Count());
        Assert.DoesNotContain(proformaQueue.Value.Data, quotation => quotation.JobOrderStatus == JobOrderStatus.ProformaInvoiceRequested);
    }
}

using APP.Repository;
using APP.Services.Stores;
using DOMAIN.Entities.JobRequests;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class ServiceWorkflowIntegrityTests
{
    private static readonly Guid Actor = Guid.NewGuid();
    private static ApplicationDbContext Context() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new CurrentUser());
    // The status steps exercised here do not use the user manager, approvals or notifications.
    private static JobRequestRepository Requests(ApplicationDbContext c) => new(c, null!, null!, null!, null!);

    private static async Task<JobRequest> Seed(ApplicationDbContext c, JobRequestStatus status = JobRequestStatus.Pending, bool approved = true)
    {
        var request = new JobRequest { Id = Guid.NewGuid(), DescriptionOfWork = "Fix the fume hood", Status = status, Approved = approved, IssuedById = Guid.NewGuid() };
        c.JobRequests.Add(request); await c.SaveChangesAsync();
        return request;
    }

    [Fact]
    public async Task OnlyAcknowledgeStartAndCancelAreSetDirectlyAndInOrder()
    {
        await using var c = Context(); var request = await Seed(c); var repository = Requests(c);
        Assert.Equal("JobRequest.Status", (await repository.ChangeJobRequestStatus(request.Id, JobRequestStatus.Completed, Actor)).Error.Code);
        Assert.Equal("JobRequest.Status", (await repository.ChangeJobRequestStatus(request.Id, JobRequestStatus.Approved, Actor)).Error.Code);
        Assert.True((await repository.ChangeJobRequestStatus(request.Id, JobRequestStatus.Acknowledged, Actor)).IsSuccess);
        Assert.Equal("JobRequest.Status", (await repository.ChangeJobRequestStatus(request.Id, JobRequestStatus.Acknowledged, Actor)).Error.Code);
        Assert.True((await repository.ChangeJobRequestStatus(request.Id, JobRequestStatus.Cancelled, Actor)).IsSuccess);
        Assert.Equal(JobRequestStatus.Cancelled, (await c.JobRequests.SingleAsync()).Status);
    }

    [Fact]
    public async Task AnUnapprovedRequestCanBeCancelledButNotAcknowledged()
    {
        await using var c = Context(); var request = await Seed(c, approved: false); var repository = Requests(c);
        Assert.False((await repository.ChangeJobRequestStatus(request.Id, JobRequestStatus.Acknowledged, Actor)).IsSuccess);
        Assert.True((await repository.ChangeJobRequestStatus(request.Id, JobRequestStatus.Cancelled, Actor)).IsSuccess);
    }

    [Theory]
    [InlineData(JobRequestStatus.Assigned)] [InlineData(JobRequestStatus.JobStarted)] [InlineData(JobRequestStatus.SentToExternal)]
    public async Task WorkInProgressCannotBeEditedDeletedOrCancelled(JobRequestStatus status)
    {
        await using var c = Context(); var request = await Seed(c, status); var repository = Requests(c);
        Assert.Equal("JobRequest.InProgress", (await repository.UpdateJobRequest(request.Id, new UpdateJobRequestRequest())).Error.Code);
        Assert.Equal("JobRequest.InProgress", (await repository.DeleteJobRequest(request.Id, Actor)).Error.Code);
        Assert.False((await repository.ChangeJobRequestStatus(request.Id, JobRequestStatus.Cancelled, Actor)).IsSuccess);
    }

    [Fact]
    public async Task AJobRequestCanBeSentOutOnlyOnceAndOnlyWhenApproved()
    {
        await using var c = Context();
        var unapproved = await Seed(c, approved: false);
        var orders = new JobOrderRepository(c, null!, null!, Requests(c));
        var create = (Guid id) => new CreateJobOrderRequest { JobRequestId = id, IssuedById = Actor, IssuedDate = DateTime.UtcNow, ServiceProviderIds = [Guid.NewGuid()] };
        Assert.False((await orders.CreateJobOrder(create(unapproved.Id))).IsSuccess);
        var assigned = await Seed(c, JobRequestStatus.Assigned);
        Assert.Equal("JobRequest.AlreadyHandled", (await orders.CreateJobOrder(create(assigned.Id))).Error.Code);
        var duplicateProviders = await Seed(c);
        var provider = Guid.NewGuid();
        Assert.Equal("JobOrder.Providers", (await orders.CreateJobOrder(new CreateJobOrderRequest
            { JobRequestId = duplicateProviders.Id, IssuedById = Actor, IssuedDate = DateTime.UtcNow, ServiceProviderIds = [provider, provider] })).Error.Code);
        Assert.Empty(await c.JobOrders.ToListAsync());
    }

    [Fact]
    public async Task AJobOrderGetsOneMemoForTheQuotationItsProformaWasRaisedAgainst()
    {
        await using var c = Context();
        var order = new JobOrder { Id = Guid.NewGuid(), Code = "JO-1", Description = "Repair", IssuedBySignature = "", IssuedById = Actor, JobRequestId = Guid.NewGuid(), Status = JobOrderStatus.ProformaInvoiceReceived };
        var provider = Guid.NewGuid();
        ServiceQuotation Quote(Guid owner) => new() { Id = Guid.NewGuid(), JobOrderId = order.Id, ServiceProviderId = owner, QuotationNumber = "Q", Notes = "", NegotiationNotes = "", IsSelected = true, CurrencyId = Guid.NewGuid() };
        var chosen = Quote(provider); var other = Quote(Guid.NewGuid());
        var proforma = new ServiceProformaInvoice { Id = Guid.NewGuid(), JobOrderId = order.Id, ServiceQuotationId = chosen.Id, ServiceProviderId = provider, InvoiceNumber = "P", Notes = "", ResponseNotes = "",
            CurrencyId = chosen.CurrencyId, RequestedById = Actor, Status = ServiceProformaInvoiceStatus.Approved };
        order.ServiceProformaInvoiceId = proforma.Id;
        c.AddRange(order, chosen, other, proforma); await c.SaveChangesAsync();
        var memos = new ServiceMemoRepository(c, null!, null!, null!);
        CreateServiceMemoRequest Memo(Guid quotation, Guid owner) => new() { JobOrderId = order.Id, ServiceQuotationId = quotation, ServiceProviderId = owner, IssuedById = Actor,
            IssuedDate = DateTime.UtcNow, ExpectedStartDate = DateTime.UtcNow, ExpectedCompletionDate = DateTime.UtcNow.AddDays(3), AgreedServiceCharge = 10 };
        Assert.Equal("Quotation.Mismatch", (await memos.CreateServiceMemo(Memo(other.Id, other.ServiceProviderId))).Error.Code);
        Assert.Equal("ServiceProvider.Mismatch", (await memos.CreateServiceMemo(Memo(chosen.Id, Guid.NewGuid()))).Error.Code);
        var backwards = Memo(chosen.Id, provider); backwards.ExpectedCompletionDate = backwards.ExpectedStartDate.AddDays(-1);
        Assert.Equal("ServiceMemo.InvalidDates", (await memos.CreateServiceMemo(backwards)).Error.Code);
        order.ServiceMemoId = Guid.NewGuid(); await c.SaveChangesAsync();
        Assert.Equal("ServiceMemo.Exists", (await memos.CreateServiceMemo(Memo(chosen.Id, provider))).Error.Code);
    }

    [Fact]
    public async Task AProformaResponseOnlyChangesItsOwnLinesToSensibleValues()
    {
        await using var c = Context();
        var order = new JobOrder { Id = Guid.NewGuid(), Code = "JO-2", Description = "Repair", IssuedBySignature = "", IssuedById = Actor, JobRequestId = Guid.NewGuid(), Status = JobOrderStatus.ProformaInvoiceRequested };
        var line = new ServiceProformaInvoiceItem { Id = Guid.NewGuid(), Quantity = 2, UnitPrice = 5, UnitOfMeasureId = Guid.NewGuid() };
        var proforma = new ServiceProformaInvoice { Id = Guid.NewGuid(), JobOrderId = order.Id, ServiceQuotationId = Guid.NewGuid(), ServiceProviderId = Guid.NewGuid(), InvoiceNumber = "", Notes = "", ResponseNotes = "",
            CurrencyId = Guid.NewGuid(), RequestedById = Actor, Items = [line] };
        c.AddRange(order, proforma); await c.SaveChangesAsync();
        var repository = new ServiceProformaInvoiceRepository(c, null!, null!, null!, null!);
        RespondServiceProformaInvoiceRequest Respond(Guid id, decimal? price, decimal? quantity) => new() { ServiceProformaInvoiceId = proforma.Id, InvoiceNumber = "PI-1", ResponseDate = DateTime.UtcNow,
            UpdatedItems = [new UpdateProformaInvoiceItemRequest { ServiceProformaInvoiceItemId = id, UnitPrice = price, Quantity = quantity }] };
        Assert.Equal("ProformaInvoice.UnknownItem", (await repository.RespondServiceProformaInvoice(Respond(Guid.NewGuid(), 4, null))).Error.Code);
        Assert.Equal("ProformaInvoice.InvalidItem", (await repository.RespondServiceProformaInvoice(Respond(line.Id, -1, null))).Error.Code);
        Assert.Equal("ProformaInvoice.InvalidItem", (await repository.RespondServiceProformaInvoice(Respond(line.Id, null, 0))).Error.Code);
        Assert.Equal(ServiceProformaInvoiceStatus.Requested, (await c.ServiceProformaInvoices.SingleAsync()).Status);
        Assert.True((await repository.RespondServiceProformaInvoice(Respond(line.Id, 4, 3))).IsSuccess);
        Assert.Equal(12, (await c.ServiceProformaInvoices.Include(p => p.Items).SingleAsync()).Items.Single().TotalPrice);
    }

    [Fact]
    public async Task AServiceInUseCannotBeDeleted()
    {
        await using var c = Context();
        var service = new DOMAIN.Entities.Services.Service { Id = Guid.NewGuid(), Name = "HVAC", Code = "S1", Description = "" };
        var spare = new DOMAIN.Entities.Services.Service { Id = Guid.NewGuid(), Name = "Painting", Code = "S2", Description = "" };
        c.AddRange(service, spare);
        c.JobRequests.Add(new JobRequest { Id = Guid.NewGuid(), DescriptionOfWork = "Fix", IssuedById = Actor, ServiceId = service.Id });
        await c.SaveChangesAsync();
        var repository = new ServiceRepository(c, null!);
        Assert.Equal("Service.InUse", (await repository.DeleteService(service.Id, Actor)).Error.Code);
        Assert.True((await repository.DeleteService(spare.Id, Actor)).IsSuccess);
    }

    [Fact]
    public async Task ServiceKeysAreCurrentGrantsAndNeverRoleNames()
    {
        await using var c = Context();
        Assert.False(await ServiceWorkflowAccess.All(c, Actor, ServiceKeys.ViewJobRequests));
        Assert.False(await ServiceWorkflowAccess.Any(c, null, ServiceKeys.ViewJobRequests));
    }

    private sealed class CurrentUser : ICurrentUserService
    {
        public Guid? UserId => Actor; public Guid? DepartmentId => null; public string DepartmentType => string.Empty;
    }
}

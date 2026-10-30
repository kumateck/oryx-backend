using APP.Repository;
using APP.Services.Email;
using APP.Services.Pdf;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.PurchaseOrders.Request;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class SendOrderCurrentUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

file class StubPdf : IPdfService
{
    public byte[] GeneratePdfFromHtml(string htmlContent) => [];
}

file class StubEmail(bool fail) : IEmailService
{
    public bool IsConfigured => true;
    public int Sent { get; private set; }

    public void SendMail(string name, string to, string subject, string body,
        List<(byte[] fileContent, string fileName, string fileType)> attachments)
    {
        if (fail)
            throw new InvalidOperationException("SMTP server unavailable");
        Sent++;
    }

    public void ProcessNotificationData(NotificationDto data) { }
}

/// <summary>
/// Sending an approved order emails it to the supplier and moves it off the Approved purchase
/// orders page. Before the fix the order was marked sent before the email went out, so a failed
/// email left it hidden from that page although the supplier never received it.
/// </summary>
public class SendPurchaseOrderToSupplierTests
{
    public SendPurchaseOrderToSupplierTests() => EnsureLogo();

    // The order PDF embeds the logo the API ships in wwwroot, read relative to the working directory.
    private static void EnsureLogo()
    {
        var target = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "entrance-logo.png");
        if (File.Exists(target))
            return;
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Oryx.sln")))
            dir = dir.Parent;
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(Path.Combine(dir!.FullName, "API", "wwwroot", "images", "entrance-logo.png"), target, overwrite: true);
    }

    [Fact]
    public async Task FailedEmail_LeavesOrderApproved()
    {
        await using var context = CreateContext();
        var order = await SeedApprovedOrderAsync(context);

        var result = await CreateProcurement(context, new StubEmail(fail: true))
            .SendPurchaseOrderToSupplier(new SendPurchaseOrderRequest { ExpectedDeliveryDate = DateTime.UtcNow }, order.Id);

        Assert.True(result.IsFailure);
        context.ChangeTracker.Clear();
        var stored = await context.PurchaseOrders.SingleAsync(po => po.Id == order.Id);
        Assert.Equal(PurchaseOrderStatus.Approved, stored.Status);
    }

    [Fact]
    public async Task SuccessfulEmail_MarksOrderSent()
    {
        await using var context = CreateContext();
        var order = await SeedApprovedOrderAsync(context);
        var email = new StubEmail(fail: false);

        var result = await CreateProcurement(context, email)
            .SendPurchaseOrderToSupplier(new SendPurchaseOrderRequest { ExpectedDeliveryDate = DateTime.UtcNow }, order.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, email.Sent);
        context.ChangeTracker.Clear();
        var stored = await context.PurchaseOrders.SingleAsync(po => po.Id == order.Id);
        Assert.Equal(PurchaseOrderStatus.Completed, stored.Status);
    }

    private static async Task<PurchaseOrder> SeedApprovedOrderAsync(ApplicationDbContext context)
    {
        // PurchaseOrder.Supplier is auto-included and required, so the InMemory
        // provider hides the order from queries unless the supplier exists.
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Supplier", Email = "supplier@example.com", Type = SupplierType.Foreign };
        var order = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            Code = "EPRC-SEND",
            SourceRequisitionId = Guid.NewGuid(),
            SupplierId = supplier.Id,
            Status = PurchaseOrderStatus.Approved,
            Approved = true,
        };
        context.AddRange(supplier, order);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return order;
    }

    private static ProcurementRepository CreateProcurement(ApplicationDbContext context, IEmailService email) =>
        new(context, null!, email, new StubPdf(), null!, null!, null!);

    private static ApplicationDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new SendOrderCurrentUser()
        );
}

using APP.Extensions;
using APP.IRepository;
using APP.Services.Email;
using APP.Services.Pdf;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.JobRequests;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ServiceProformaInvoiceRepository(
    ApplicationDbContext context,
    IMapper mapper,
    UserManager<User> userManager,
    IEmailService emailService,
    IPdfService pdfService) : IServiceProformaInvoiceRepository
{
    public async Task<Result<Guid>> RequestServiceProformaInvoice(RequestServiceProformaInvoiceRequest request, Guid requestedById)
    {
        var jobOrder = await context.JobOrders
            .AsSplitQuery()
            .Include(j => j.SelectedQuotation)
            .ThenInclude(q => q.Items)
            .Include(j => j.SelectedQuotation)
            .ThenInclude(q => q.ServiceProvider)
            .Include(j => j.JobRequest).Include(jobOrder => jobOrder.Service)
            .FirstOrDefaultAsync(j => j.Id == request.JobOrderId);

        if (jobOrder is null)
            return Error.NotFound("JobOrder.NotFound", "Job order not found");

        if (jobOrder.Status != JobOrderStatus.QuotationSelected)
            return Error.Validation("JobOrder.InvalidStatus", "A quotation must be selected before requesting proforma invoice");

        var quotation = await context.ServiceQuotations
            .AsSplitQuery()
            .Include(q => q.Items)
            .Include(q => q.ServiceProvider)
            .Include(q => q.Currency)
            .FirstOrDefaultAsync(q => q.Id == request.ServiceQuotationId && q.JobOrderId == request.JobOrderId);

        if (quotation is null)
            return Error.NotFound("ServiceQuotation.NotFound", "Service quotation not found");

        if (!quotation.IsSelected)
            return Error.Validation("Quotation.NotSelected", "This quotation has not been selected");

        var requestedBy = await userManager.FindByIdAsync(requestedById.ToString());
        if (requestedBy is null) return Error.Validation("User.Invalid", "User Invalid");

        // Check if proforma invoice already requested
        var existingProforma = await context.ServiceProformaInvoices
            .FirstOrDefaultAsync(p => p.JobOrderId == request.JobOrderId && p.ServiceQuotationId == request.ServiceQuotationId);

        if (existingProforma != null)
            return Error.Validation("ProformaInvoice.AlreadyRequested", "Proforma invoice has already been requested for this quotation");

        // Create proforma invoice request
        var proformaInvoice = new ServiceProformaInvoice
        {
            JobOrderId = request.JobOrderId,
            ServiceQuotationId = request.ServiceQuotationId,
            ServiceProviderId = quotation.ServiceProviderId,
            RequestedDate = DateTime.UtcNow,
            RequestedById = requestedById,
            Notes = request.Notes,
            ServiceCharge = quotation.NegotiatedServiceCharge ?? quotation.TotalServiceCharge,
            CurrencyId = quotation.CurrencyId,
            Status = ServiceProformaInvoiceStatus.Requested,
            // Copy items from quotation
            Items = quotation.Items.Select(item => new ServiceProformaInvoiceItem
            {
                ItemId = item.ItemId,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitOfMeasureId = item.UnitOfMeasureId,
                UnitPrice = item.NegotiatedUnitPrice ?? item.UnitPrice,
            }).ToList()
        };

        await context.ServiceProformaInvoices.AddAsync(proformaInvoice);

        // Update job order
        jobOrder.ServiceProformaInvoiceId = proformaInvoice.Id;
        jobOrder.Status = JobOrderStatus.ProformaInvoiceRequested;
        context.JobOrders.Update(jobOrder);

        await context.SaveChangesAsync();

        // Send email to service provider requesting proforma invoice
        try
        {
            var emailBody = $@"
                <html>
                <body>
                    <h2>Proforma Invoice Request</h2>
                    <p>Dear {quotation.ServiceProvider.Name},</p>
                    <p>We are requesting a proforma invoice for the following service:</p>
                    <p><strong>Job Order:</strong> {jobOrder.Code}</p>
                    <p><strong>Service:</strong> {jobOrder.Service?.Name ?? "N/A"}</p>
                    <p><strong>Description:</strong> {jobOrder.Description}</p>
                    <p>Please provide your proforma invoice at your earliest convenience.</p>
                    {(!string.IsNullOrEmpty(request.Notes) ? $"<p><strong>Notes:</strong> {request.Notes}</p>" : "")}
                    <p>Thank you.</p>
                </body>
                </html>";

            emailService.SendMail(
                quotation.ServiceProvider.Name,
                quotation.ServiceProvider.Email,
                $"Proforma Invoice Request - {jobOrder.Code}",
                emailBody,
                new List<(byte[] fileContent, string fileName, string fileType)>());
        }
        catch (Exception ex)
        {
            // Log error but don't fail the operation
            // In production, you might want to log this to a logging service
        }

        return proformaInvoice.Id;
    }

    public async Task<Result> RespondServiceProformaInvoice(RespondServiceProformaInvoiceRequest request)
    {
        var proformaInvoice = await context.ServiceProformaInvoices
            .AsSplitQuery()
            .Include(p => p.JobOrder)
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.ServiceProformaInvoiceId);

        if (proformaInvoice is null)
            return Error.NotFound("ServiceProformaInvoice.NotFound", "Service proforma invoice not found");

        if (proformaInvoice.Status != ServiceProformaInvoiceStatus.Requested)
            return Error.Validation("ProformaInvoice.InvalidStatus", "Proforma invoice has already been responded to");

        proformaInvoice.InvoiceNumber = request.InvoiceNumber;
        proformaInvoice.ResponseReceivedDate = request.ResponseDate;
        proformaInvoice.ResponseNotes = request.ResponseNotes;
        proformaInvoice.ProformaInvoiceDocumentUrl = request.ProformaInvoiceDocumentUrl;
        proformaInvoice.Status = ServiceProformaInvoiceStatus.ResponseReceived;

        // Update items if provided
        if (request.UpdatedItems != null && request.UpdatedItems.Any())
        {
            foreach (var updatedItem in request.UpdatedItems)
            {
                var item = proformaInvoice.Items.FirstOrDefault(i => i.Id == updatedItem.ServiceProformaInvoiceItemId);
                if (item != null)
                {
                    if (updatedItem.UnitPrice.HasValue)
                        item.UnitPrice = updatedItem.UnitPrice.Value;
                    if (updatedItem.Quantity.HasValue)
                        item.Quantity = updatedItem.Quantity.Value;
                }
            }
        }

        // Update job order status
        proformaInvoice.JobOrder.Status = JobOrderStatus.ProformaInvoiceReceived;
        context.ServiceProformaInvoices.Update(proformaInvoice);
        context.JobOrders.Update(proformaInvoice.JobOrder);

        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> ApproveServiceProformaInvoice(ApproveServiceProformaInvoiceRequest request)
    {
        var proformaInvoice = await context.ServiceProformaInvoices
            .AsSplitQuery()
            .Include(p => p.JobOrder)
            .FirstOrDefaultAsync(p => p.Id == request.ServiceProformaInvoiceId);

        if (proformaInvoice is null)
            return Error.NotFound("ServiceProformaInvoice.NotFound", "Service proforma invoice not found");

        if (proformaInvoice.Status != ServiceProformaInvoiceStatus.ResponseReceived)
            return Error.Validation("ProformaInvoice.InvalidStatus", "Proforma invoice must have a response before approval");

        var approvedBy = await userManager.FindByIdAsync(request.ApprovedById.ToString());
        if (approvedBy is null) return Error.Validation("User.Invalid", "User Invalid");

        proformaInvoice.Status = ServiceProformaInvoiceStatus.Approved;
        context.ServiceProformaInvoices.Update(proformaInvoice);

        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<Paginateable<IEnumerable<ServiceProformaInvoiceDto>>>> GetServiceProformaInvoices(int page, int pageSize,
        ServiceProformaInvoiceStatus? status = null, Guid? jobOrderId = null, Guid? serviceProviderId = null)
    {
        var query = context.ServiceProformaInvoices
            .AsSplitQuery()
            .Include(p => p.JobOrder)
            .Include(p => p.ServiceQuotation)
            .Include(p => p.ServiceProvider)
            .Include(p => p.Currency)
            .Include(p => p.Items).ThenInclude(i => i.Item)
            .Include(p => p.Items).ThenInclude(i => i.UnitOfMeasure)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        if (jobOrderId.HasValue)
        {
            query = query.Where(p => p.JobOrderId == jobOrderId.Value);
        }

        if (serviceProviderId.HasValue)
        {
            query = query.Where(p => p.ServiceProviderId == serviceProviderId.Value);
        }

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize,
            entity => mapper.Map<ServiceProformaInvoiceDto>(entity));
    }

    public async Task<Result<ServiceProformaInvoiceDto>> GetServiceProformaInvoice(Guid id)
    {
        var proformaInvoice = await context.ServiceProformaInvoices
            .AsSplitQuery()
            .Include(p => p.JobOrder).ThenInclude(j => j.JobRequest)
            .Include(p => p.ServiceQuotation).ThenInclude(q => q.Items)
            .Include(p => p.ServiceProvider)
            .Include(p => p.Currency)
            .Include(p => p.Items).ThenInclude(i => i.Item)
            .Include(p => p.Items).ThenInclude(i => i.UnitOfMeasure)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (proformaInvoice is null)
            return Error.NotFound("ServiceProformaInvoice.NotFound", "Service proforma invoice not found");

        return mapper.Map<ServiceProformaInvoiceDto>(proformaInvoice);
    }
}


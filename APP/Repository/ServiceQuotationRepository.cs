using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.JobRequests;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ServiceQuotationRepository(ApplicationDbContext context, IMapper mapper) : IServiceQuotationRepository
{
    public async Task<Result<Guid>> CreateServiceQuotation(CreateServiceQuotationRequest request)
    {
        var jobOrder = await context.JobOrders
            .AsSplitQuery()
            .Include(j => j.ServiceProviders)
            .FirstOrDefaultAsync(j => j.Id == request.JobOrderId);

        if (jobOrder is null)
            return Error.NotFound("JobOrder.NotFound", "Job order not found");

        var serviceProvider = await context.ServiceProviders.FirstOrDefaultAsync(sp =>
            sp.Id == request.ServiceProviderId);
        if (serviceProvider is null)
            return Error.Validation("ServiceProvider.Invalid", "Invalid service provider");

        // Verify this provider was sent the job order
        var wasSentJobOrder = jobOrder.ServiceProviders.Any(sp =>
            sp.ServiceProviderId == request.ServiceProviderId);
        if (!wasSentJobOrder)
            return Error.Validation("ServiceProvider.NotSentJobOrder",
                "This service provider was not sent this job order");

        var currency = await context.Currencies.AnyAsync(c => c.Id == request.CurrencyId);
        if (!currency) return Error.Validation("Currency.Invalid", "Invalid currency");

        // Validate quotation items
        foreach (var item in request.Items)
        {
            if (item.ItemId.HasValue)
            {
                var exists = await context.Items.AnyAsync(i => i.Id == item.ItemId.Value);
                if (!exists) return Error.Validation("Item.Invalid", $"Invalid item: {item.ItemId}");
            }

            var uom = await context.UnitOfMeasures.AnyAsync(u => u.Id == item.UnitOfMeasureId);
            if (!uom) return Error.Validation("UnitOfMeasure.Invalid", "Invalid unit of measure");
        }

        var quotation = mapper.Map<ServiceQuotation>(request);

        // Map items
        quotation.Items = request.Items.Select(i => new QuotationItem
        {
            ItemId = i.ItemId,
            Quantity = i.Quantity,
            UnitOfMeasureId = i.UnitOfMeasureId,
            UnitPrice = i.UnitPrice,
        }).ToList();

        quotation.ServiceCharges = request.ServiceCharges.Select(i => new ServiceCharge
        {
            Name = i.Name,
            Cost = i.Cost
        }).ToList();

        await context.ServiceQuotations.AddAsync(quotation);

        // Update job order service provider response
        var jobOrderProvider = jobOrder.ServiceProviders.FirstOrDefault(sp 
            => sp.ServiceProviderId == request.ServiceProviderId);
        if (jobOrderProvider != null)
        {
            jobOrderProvider.ResponseReceived = true;
            jobOrderProvider.ResponseDate = DateTime.UtcNow;
        }

        // Update job order status if at least one quotation received
        if (jobOrder.Status == JobOrderStatus.SentToProviders)
        {
            jobOrder.Status = JobOrderStatus.QuotationsReceived;
            context.JobOrders.Update(jobOrder);
        }

        await context.SaveChangesAsync();

        return quotation.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<ServiceQuotationDto>>>> GetServiceQuotations(int page, 
        int pageSize,
        QuotationStatus? status = null, 
        Guid? jobOrderId = null, 
        Guid? serviceProviderId = null)
    {
        var query = context.ServiceQuotations
            .AsSplitQuery()
            .Include(q => q.JobOrder)
            .Include(q => q.ServiceProvider)
            .Include(q => q.Currency)
            .Include(q => q.Items)
                .ThenInclude(i => i.Item)
            .Include(q => q.Items)
                .ThenInclude(i => i.UnitOfMeasure)
            .Include(q => q.JobOrder)
                .ThenInclude(j => j.Service)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(q => q.Status == status.Value);
        }

        if (jobOrderId.HasValue)
        {
            query = query.Where(q => q.JobOrderId == jobOrderId.Value);
        }

        if (serviceProviderId.HasValue)
        {
            query = query.Where(q => q.ServiceProviderId == serviceProviderId.Value);
        }

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize,
            mapper.Map<ServiceQuotationDto>);
    }

    public async Task<Result<ServiceQuotationDto>> GetServiceQuotation(Guid id)
    {
        var quotation = await context.ServiceQuotations
            .AsSplitQuery()
            .Include(q => q.JobOrder).ThenInclude(j => j.JobRequest)
            .Include(q => q.ServiceProvider)
            .Include(q => q.Currency)
            .Include(q => q.Items).ThenInclude(i => i.Item)
            .Include(q => q.Items).ThenInclude(i => i.UnitOfMeasure)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quotation is null)
            return Error.NotFound("ServiceQuotation.NotFound", "Service quotation not found");

        return mapper.Map<ServiceQuotationDto>(quotation);
    }

    public async Task<Result> UpdateServiceQuotation(Guid id, UpdateServiceQuotationRequest request)
    {
        var quotation = await context.ServiceQuotations.FirstOrDefaultAsync(q => q.Id == id);
        if (quotation is null)
            return Error.NotFound("ServiceQuotation.NotFound", "Service quotation not found");

        mapper.Map(request, quotation);
        context.ServiceQuotations.Update(quotation);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> NegotiateQuotation(NegotiateQuotationRequest request)
    {
        var quotation = await context.ServiceQuotations
            .AsSplitQuery()
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.Id == request.QuotationId);

        if (quotation is null)
            return Error.NotFound("ServiceQuotation.NotFound", "Service quotation not found");

        quotation.Status = QuotationStatus.Negotiating;
        quotation.NegotiatedServiceCharge = request.NegotiatedServiceCharge;
        quotation.NegotiationNotes = request.NegotiationNotes;

        // Update negotiated item prices
        foreach (var negotiatedItem in request.NegotiatedItems)
        {
            var item = quotation.Items.FirstOrDefault(i => i.Id == negotiatedItem.QuotationItemId);
            if (item != null)
            {
                item.NegotiatedUnitPrice = negotiatedItem.NegotiatedUnitPrice;
            }
        }

        // Calculate negotiated total cost
        var negotiatedMaterialsCost = quotation.Items.Sum(i => i.NegotiatedTotalPrice ?? i.TotalPrice);
        quotation.NegotiatedTotalCost = (request.NegotiatedServiceCharge ?? quotation.TotalServiceCharge)
                                        + negotiatedMaterialsCost;

        context.ServiceQuotations.Update(quotation);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<List<ServiceQuotationDto>>> CompareQuotations(Guid jobOrderId)
    {
        var quotations = await context.ServiceQuotations
            .AsSplitQuery()
            .Include(q => q.ServiceProvider)
            .ThenInclude(q => q.Country)
            .Include(q => q.ServiceProvider)
            .ThenInclude(q => q.Currency)
            .Include(q => q.Currency)
            .Include(q => q.Items)
                .ThenInclude(i => i.Item)
            .Include(q => q.Items)
                .ThenInclude(i => i.UnitOfMeasure)
            .Include(q => q.Items)
                .ThenInclude(i => i.Item)
                    .ThenInclude(i => i.ItemCategory)
            .Include(q => q.JobOrder)
                .ThenInclude(j => j.Service)
            .Where(q => q.JobOrderId == jobOrderId)
            .ToListAsync();

        if (quotations.Count == 0)
            return Error.NotFound("Quotations.NotFound", "No quotations found for this job order");

        var quotationDto = mapper.Map<List<ServiceQuotationDto>>(quotations);
        return quotationDto.OrderBy(q => q.GrandTotal).ToList();
    }
}


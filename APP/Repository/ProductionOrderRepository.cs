using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Invoices;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.ProformaInvoices;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ProductionOrderRepository(ApplicationDbContext context, IMapper mapper) : IProductionOrderRepository
{
    public async Task<Result<Guid>> CreateProductionOrder(CreateProductionOrderRequest request)
    {
        var productionOrder = mapper.Map<ProductionOrder>(request);
        await context.AddAsync(productionOrder);
        await context.SaveChangesAsync();
        return productionOrder.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<ProductionOrderDto>>>> GetProductionOrders(int page, int pageSize, string searchQuery)
    {
        var query = context.ProductionOrders
            .IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(p => p.Customer)
            .Include(p => p.Products)
            .ThenInclude(p => p.Product)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Code);
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<ProductionOrderDto>);
    }

    public async Task<Result<ProductionOrderDto>> GetProductionOrder(Guid id)
    {
        var productionOrder = await context.ProductionOrders
            .IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(p => p.Products)
            .ThenInclude(p => p.Product)
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(po => po.Id == id);
        return productionOrder is null ? 
            Error.NotFound("ProductionOrder.NotFound", "Production Order not found") 
            : mapper.Map<ProductionOrderDto>(productionOrder);
    }

    public async Task<Result> UpdateProductionOrder(Guid id, CreateProductionOrderRequest request)
    {
        var productionOrder = await context.ProductionOrders.FirstOrDefaultAsync(p => p.Id == id);
        if(productionOrder is null) return Error.NotFound("ProductionOrder.NotFound", "Production Order not found");

        productionOrder.Products = mapper.Map<List<ProductionOrderProducts>>(request.Products);
        mapper.Map(request, productionOrder);
        context.ProductionOrders.Update(productionOrder);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteProductionOrder(Guid id, Guid userId)
    {
        var productionOrder = await context.ProductionOrders.FirstOrDefaultAsync(po => po.Id == id);
        if (productionOrder == null) return Error.NotFound("ProductionOrder.NotFound", "Production Order not found");
        
        productionOrder.DeletedAt = DateTime.Now;
        productionOrder.LastDeletedById = userId;
        context.ProductionOrders.Update(productionOrder);
            
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // -------------------------------
    // CRUD for Proforma Invoices
    // -------------------------------

    public async Task<Result<Guid>> CreateProformaInvoice(CreateProformaInvoice request)
    {
        var productionOrderExists = await context.ProductionOrders.AnyAsync(po => po.Id == request.ProductionOrderId);
        if (!productionOrderExists)
        {
            return Error.NotFound("ProductionOrder.NotFound", "Production Order not found");
        }

        var invoice = new ProformaInvoice
        {
            ProductionOrderId = request.ProductionOrderId,
            Products = request.Products.Select(p => new ProformaInvoiceProduct
            {
                ProductId = p.ProductId,
                Quantity = p.Quantity
            }).ToList()
        };

        await context.ProformaInvoices.AddAsync(invoice);
        await context.SaveChangesAsync();

        return invoice.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<ProformaInvoiceDto>>>> GetProformaInvoices(int page, int pageSize, string searchQuery)
    {
        var query = context.ProformaInvoices
            .AsSplitQuery()
            .Include(p => p.ProductionOrder).ThenInclude(p => p.Customer)
            .Include(p => p.Products).ThenInclude(p => p.Product)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.ProductionOrder.Code);
        }

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<ProformaInvoiceDto>);
    }

    public async Task<Result<ProformaInvoiceDto>> GetProformaInvoice(Guid id)
    {
        var invoice = await context.ProformaInvoices
            .AsSplitQuery()
            .Include(p => p.ProductionOrder).ThenInclude(p => p.Customer)
            .Include(p => p.Products).ThenInclude(p => p.Product)
            .FirstOrDefaultAsync(p => p.Id == id);

        return invoice is null
            ? Error.NotFound("ProformaInvoice.NotFound", "Proforma Invoice not found")
            : mapper.Map<ProformaInvoiceDto>(invoice);
    }

    public async Task<Result> UpdateProformaInvoice(Guid id, CreateProformaInvoice request)
    {
        var invoice = await context.ProformaInvoices
            .Include(p => p.Products)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (invoice is null)
            return Error.NotFound("ProformaInvoice.NotFound", "Proforma Invoice not found");

        invoice.ProductionOrderId = request.ProductionOrderId;

        context.ProformaInvoiceProducts.RemoveRange(invoice.Products);

        invoice.Products = request.Products.Select(p => new ProformaInvoiceProduct
        {
            ProductId = p.ProductId,
            Quantity = p.Quantity,
            ProformaInvoiceId = invoice.Id
        }).ToList();

        context.ProformaInvoices.Update(invoice);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeleteProformaInvoice(Guid id, Guid userId)
    {
        var invoice = await context.ProformaInvoices.FirstOrDefaultAsync(p => p.Id == id);
        if (invoice is null)
            return Error.NotFound("ProformaInvoice.NotFound", "Proforma Invoice not found");

        invoice.DeletedAt = DateTime.Now;
        invoice.LastDeletedById = userId;
        context.ProformaInvoices.Update(invoice);

        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result<Guid>> CreateInvoice(CreateInvoice request)
    {
        var proformaExists = await context.ProformaInvoices.AnyAsync(p => p.Id == request.ProformaInvoiceId);
        if (!proformaExists)
            return Error.NotFound("ProformaInvoice.NotFound", "Proforma Invoice not found");

        var invoice = mapper.Map<Invoice>(request);
        invoice.Status = InvoiceStatus.Pending;

        await context.Invoices.AddAsync(invoice);
        await context.SaveChangesAsync();
        return invoice.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<InvoiceDto>>>> GetInvoices(int page, int pageSize, string searchQuery)
    {
        var query = context.Invoices
            .Include(i => i.ProformaInvoice)
                .ThenInclude(p => p.ProductionOrder)
            .Include(i => i.ProformaInvoice.Products).ThenInclude(p => p.Product)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, i => i.CustomerPoNumber);
        }

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<InvoiceDto>);
    }

    public async Task<Result<InvoiceDto>> GetInvoice(Guid id)
    {
        var invoice = await context.Invoices
            .Include(i => i.ProformaInvoice)
                .ThenInclude(p => p.ProductionOrder)
            .Include(i => i.ProformaInvoice.Products).ThenInclude(p => p.Product)
            .FirstOrDefaultAsync(i => i.Id == id);

        return invoice is null
            ? Error.NotFound("Invoice.NotFound", "Invoice not found")
            : mapper.Map<InvoiceDto>(invoice);
    }

    public async Task<Result> UpdateInvoice(Guid id, CreateInvoice request)
    {
        var invoice = await context.Invoices.FirstOrDefaultAsync(i => i.Id == id);
        if (invoice is null)
            return Error.NotFound("Invoice.NotFound", "Invoice not found");

        invoice.CustomerPoNumber = request.CustomerPoNumber;
        invoice.ProformaInvoiceId = request.ProformaInvoiceId;

        context.Invoices.Update(invoice);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeleteInvoice(Guid id, Guid userId)
    {
        var invoice = await context.Invoices.FirstOrDefaultAsync(i => i.Id == id);
        if (invoice is null)
            return Error.NotFound("Invoice.NotFound", "Invoice not found");

        invoice.DeletedAt = DateTime.Now;
        invoice.LastDeletedById = userId;

        context.Invoices.Update(invoice);
        await context.SaveChangesAsync();

        return Result.Success();
    }
}

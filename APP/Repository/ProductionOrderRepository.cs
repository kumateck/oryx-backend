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

public class ProductionOrderRepository(ApplicationDbContext context, IMapper mapper, IApprovalRepository approvalRepository) : IProductionOrderRepository
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

    public async Task<Result<ProductionOrderDetailDto>> GetProductionOrder(Guid id)
    {
        var productionOrder = await context.ProductionOrders
            .IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(p => p.Products)
            .ThenInclude(p => p.Product)
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(po => po.Id == id);

        if (productionOrder == null)
            return Error.NotFound("ProductionOrder.NotFound", "Production Order not found");
        
        var productionOrderDto = mapper.Map<ProductionOrderDetailDto>(productionOrder);
        
        productionOrderDto.Invoice =
            mapper.Map<ProductionOrderInvoiceDto>(await context.Invoices
                .AsSplitQuery()
                .Include(p => p.ProformaInvoice)
                .FirstOrDefaultAsync(p => p.ProformaInvoice.ProductionOrderId == productionOrderDto.Id));
        
        return productionOrderDto;
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
        var productionOrder = await context.ProductionOrders.FirstOrDefaultAsync(po => po.Id == request.ProductionOrderId);
        if (productionOrder is null)
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
        productionOrder.Status = ProductionOrderStatus.Invoiced;
        context.ProductionOrders.Update(productionOrder);
        await context.SaveChangesAsync();
        
        await approvalRepository.CreateInitialApprovalsAsync(nameof(ProductionOrder), productionOrder.Id);

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
            .AsSplitQuery()
            .Include(i => i.ProformaInvoice)
                .ThenInclude(p => p.ProductionOrder)
            .Include(i => i.ProformaInvoice.Products)
            .ThenInclude(p => p.Product)
            .Include(i => i.Customer)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, 
                i => i.Customer.Name, i => i.Customer.Address, i => i.Customer.Email);
        }

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<InvoiceDto>);
    }

    public async Task<Result<InvoiceDto>> GetInvoice(Guid id)
    {
        var invoice = await context.Invoices
            .AsSplitQuery()
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

        invoice.CustomerId = request.CustomerId;
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

     public async Task<Result> AllocateProduct(AllocateProductionOrderRequest request)
    {
        var productionOrder = await context.ProductionOrders
            .AsSplitQuery()
            .Include(p => p.Products)
                .ThenInclude(p => p.FulfilledQuantities)
            .FirstOrDefaultAsync(f => f.Id == request.ProductionOrderId);

        if (productionOrder == null)
            return Error.NotFound("ProductionOrder.NotFound", "Production order not found");

        foreach (var product in request.Products)
        {
            var allocationProduct = productionOrder.Products.FirstOrDefault(p => p.ProductId == product.ProductId);
            if (allocationProduct == null)
                return Error.NotFound("ProductionOrder.ProductNotFound",
                    $"Product {product.ProductId} not found in this production order");

            if (allocationProduct.RemainingQuantity == 0)
                return Error.Validation("ProductionOrder.Product",
                    $"Product {product.ProductId} has already been allocated completely.");

            if (allocationProduct.Fulfilled)
                return Error.Validation("ProductionOrder.Product",
                    "Product has already been marked as fulfilled.");

            var totalToAllocate = product.FulfilledQuantities.Sum(q => q.Quantity);
            if (totalToAllocate > allocationProduct.RemainingQuantity)
            {
                return Error.Validation("ProductionOrder.Product",
                    $"Allocation quantity {totalToAllocate} is more than what is left to be fulfilled {allocationProduct.RemainingQuantity}");
            }

            foreach (var quantityToFulfill in product.FulfilledQuantities)
            {
                var finishedGoodsTransferNote = await context.FinishedGoodsTransferNotes
                    .FirstOrDefaultAsync(f => f.Id == quantityToFulfill.FinishedGoodsTransferNoteId);

                if (finishedGoodsTransferNote is null)
                    return Error.NotFound("ProductionOrder.FinishedGoodsTransferNoteNotFound",
                        $"Finished goods transfer note {quantityToFulfill.FinishedGoodsTransferNoteId} not found.");

                if (finishedGoodsTransferNote.RemainingQuantity == 0)
                    return Error.Validation("ProductionOrder.FinishedGoodsTransferNoteValidation",
                        $"The finished good transfer note {quantityToFulfill.FinishedGoodsTransferNoteId} does not have any remaining quantity.");

                if (quantityToFulfill.Quantity > finishedGoodsTransferNote.RemainingQuantity)
                    return Error.Validation("ProductionOrder.FinishedGoodsTransferNoteValidation",
                        $"Trying to allocate {quantityToFulfill.Quantity}, " +
                        $"but only {finishedGoodsTransferNote.RemainingQuantity} is left in transfer note {quantityToFulfill.FinishedGoodsTransferNoteId}.");

                // Check if an allocation for this note already exists
                var existingAllocationProductForNote = allocationProduct
                    .FulfilledQuantities
                    .FirstOrDefault(p => p.FinishedGoodsTransferNoteId == quantityToFulfill.FinishedGoodsTransferNoteId);

                if (existingAllocationProductForNote is not null)
                {
                    existingAllocationProductForNote.Quantity += quantityToFulfill.Quantity;
                }
                else
                {
                    allocationProduct.FulfilledQuantities.Add(new ProductionOrderProductQuantity
                    {
                        Quantity = quantityToFulfill.Quantity,
                        FinishedGoodsTransferNoteId = quantityToFulfill.FinishedGoodsTransferNoteId
                    });
                }

                finishedGoodsTransferNote.AllocatedQuantity += quantityToFulfill.Quantity;
            }
        }

        // Save all changes once
        await context.SaveChangesAsync();

        // Mark products as fulfilled if no remaining quantity
        foreach (var product in productionOrder.Products)
        {
            if (product.RemainingQuantity == 0 && !product.Fulfilled)
            {
                product.Fulfilled = true;
            }
        }

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> MarkProductionOrderAsDelivered(Guid id)
    {
        var productionOrder = await context.ProductionOrders
            .FirstOrDefaultAsync(p => p.Id == id);
        if (productionOrder == null) return Error.NotFound("Product.Order", "Product order not found");

        if (productionOrder.Status != ProductionOrderStatus.Invoiced && !productionOrder.Approved)
        {
            return Error.Validation("Product.Order.Approved", "Product order not approved");
        }
        productionOrder.DeliveredAt = DateTime.UtcNow;
        context.ProductionOrders.Update(productionOrder);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}

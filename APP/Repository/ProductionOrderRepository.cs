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
        if (request.Products.GroupBy(p => new {p.ProductId, p.ProductPackingId})
            .Any(g => g.Count() > 1))
        {
            return Error.Validation("Production.Order",
                "Production order product list " +
                "cannot contain more than one of the same product and product packing");
        }
            
        var productionOrder = mapper.Map<ProductionOrder>(request);
        await context.AddAsync(productionOrder);
        await context.SaveChangesAsync();
        return productionOrder.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<ProductionOrderDto>>>> GetProductionOrders(int page, 
        int pageSize, string searchQuery, ProductionOrderStatus? status)
    {
        var query = context.ProductionOrders
            .IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(p => p.Customer)
            .Include(p => p.Products)
            .ThenInclude(p => p.Product)
            .Include(p => p.Products)
            .ThenInclude(p => p.ProductPacking)
            // .ThenInclude(p => p.PackingLists)
            .Include(p => p.Products)
            .ThenInclude(p => p.ProductPacking)
            .ThenInclude(p => p.BasePackingUoM)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Code);
        }

        if (status.HasValue)
        {
            query = query.Where(q => q.Status == status.Value);
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
            .Include(p => p.Products)
            .ThenInclude(p => p.ProductPacking)
            .ThenInclude(p => p.PackingLists)
            .Include(p => p.Products)
            .ThenInclude(p => p.ProductPacking)
            .ThenInclude(p => p.BasePackingUoM)
            .FirstOrDefaultAsync(po => po.Id == id);

        if (productionOrder == null)
            return Error.NotFound("ProductionOrder.NotFound", "Production Order not found");

        var productionOrderDto = mapper.Map<ProductionOrderDetailDto>(productionOrder);

        productionOrderDto.Invoice =
            mapper.Map<ProductionOrderInvoiceDto>(await context.Invoices
                .AsSplitQuery()
                .Include(p => p.ProformaInvoice).ThenInclude(p => p.AllocateProductionOrder)
                .FirstOrDefaultAsync(p => p.ProformaInvoice.AllocateProductionOrder.ProductionOrderId == productionOrderDto.Id));

        return productionOrderDto;
    }

    public async Task<Result> UpdateProductionOrder(Guid id, CreateProductionOrderRequest request)
    {
        var productionOrder = await context.ProductionOrders
            .FirstOrDefaultAsync(p => p.Id == id);
        if (productionOrder is null) 
            return Error.NotFound("ProductionOrder.NotFound", 
            "Production Order not found");

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
        var productionOrder = await context.AllocateProductionOrders.FirstOrDefaultAsync(po => po.Id == request.AllocateProductionOrderId);
        if (productionOrder is null)
        {
            return Error.NotFound("Allocation.ProductionOrder.NotFound", "Allocation production Order not found");
        }

        var invoice = new ProformaInvoice
        {
            Code = request.Code,
            AllocateProductionOrderId = request.AllocateProductionOrderId,
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

    public async Task<Result> SendProformaInvoiceToCustomer(Guid proformaInvoiceId, Guid userId)
    {
        var proformaInvoice = await context.ProformaInvoices.FirstOrDefaultAsync(p => p.Id == proformaInvoiceId);
        if (proformaInvoice is null) return Error.NotFound("ProformaInvoice.notFound", "Proforma Invoice not found");

        proformaInvoice.Status = ProformaInvoiceStatus.SentToCustomer;
        proformaInvoice.LastUpdatedById = userId;
        //context.ProformaInvoices.Update(proformaInvoice);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<Paginateable<IEnumerable<ProformaInvoiceDto>>>> GetProformaInvoices(int page,
        int pageSize, 
        string searchQuery, 
        ProformaInvoiceStatus? status = null,
        bool? approved = null)
    {
        var query = context.ProformaInvoices
            .AsSplitQuery()
            .Include(p => p.AllocateProductionOrder)
            .ThenInclude(p => p.ProductionOrder)
            .ThenInclude(p => p.Customer)
            .Include(p => p.Products)
            .ThenInclude(p => p.Product)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.AllocateProductionOrder.ProductionOrder.Code);
        }

        if (approved.HasValue)
        {
            query = query.Where(q => q.AllocateProductionOrder.Approved == approved.Value);
        }

        if (status != null)
        {
            query = query.Where(q => q.Status == status);
        }

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<ProformaInvoiceDto>);
    }

    public async Task<Result<ProformaInvoiceDto>> GetProformaInvoice(Guid id)
    {
        var invoice = await context.ProformaInvoices
            .AsSplitQuery()
            .Include(p => p.AllocateProductionOrder)
            .ThenInclude(p => p.ProductionOrder)
            .ThenInclude(p => p.Customer)
            .Include(p => p.Products)
            .ThenInclude(p => p.Product)
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

        invoice.AllocateProductionOrderId = request.AllocateProductionOrderId;

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
                .ThenInclude(p => p.AllocateProductionOrder)
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
                .ThenInclude(p => p.AllocateProductionOrder)
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

        if (productionOrder.Products.All(p => p.Fulfilled))
        {
            productionOrder.Status = ProductionOrderStatus.FullPackingReady;

        }
        else if (productionOrder.Products.Any(p => p.Fulfilled))
        {
            productionOrder.Status = ProductionOrderStatus.PartialPackingReady;
        }

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> MarkAllocationProductionOrderAsLoaded(Guid id)
    {
        var productionOrder = await context.AllocateProductionOrders
            .FirstOrDefaultAsync(p => p.Id == id);
        if (productionOrder == null) return Error.NotFound("Product.Order", "Product order not found");

        productionOrder.LoadedAt = DateTime.UtcNow;
        productionOrder.Status = AllocateProductionOrderStatus.Loaded;
        context.AllocateProductionOrders.Update(productionOrder);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> CreateWaybillFromProductionOrder(CreateProductionOrderWaybill request, Guid id)
    {
        var productionOrder = await context.AllocateProductionOrders
            .FirstOrDefaultAsync(p => p.Id == id);
        if (productionOrder == null) return Error.NotFound("Product.Order", 
            "Product order not found");
        
        if(await context.ProductionOrderWaybills.AnyAsync(p => p.AllocateProductionOrderId == id))
            return Error.Validation("ProductionOrder.Waybill", 
                "ProductionOrder.Waybill already exists for this allocation");

        await context.ProductionOrderWaybills.AddAsync(new ProductionOrderWaybill
        {
            Code = request.Code,
            AllocateProductionOrderId = id,
            Comment = request.Comment,
        });

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<Paginateable<IEnumerable<ProductionOrderWaybillDto>>>> GetProductionOrderWaybills(
        int page,
        int pageSize,
        string searchQuery,
        Guid? allocateProductionOrderId = null)
    {
        var query = context.ProductionOrderWaybills
            .AsSplitQuery()
            .Include(w => w.AllocateProductionOrder)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery,
                q => q.Comment,
                q => q.AllocateProductionOrder.ProductionOrder.Code);
        }

        if (allocateProductionOrderId.HasValue)
        {
            query = query.Where(q => q.AllocateProductionOrderId == allocateProductionOrderId);
        }

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<ProductionOrderWaybillDto>);
    }

    public async Task<Result<ProductionOrderWaybillDto>> GetProductionOrderWaybill(Guid id)
    {
        return mapper.Map<ProductionOrderWaybillDto>(await context
            .ProductionOrderWaybills
            .Include(w => w.AllocateProductionOrder)
            .ThenInclude(w => w.Products)
            .Include(w => w.AllocateProductionOrder)
            .ThenInclude(w => w.ProductionOrder)
            .FirstOrDefaultAsync(p => p.Id == id));
    }

    public async Task<Result> SendWaybillToCustomer(Guid id)
    {
        var productionOrder = await context.AllocateProductionOrders
            .FirstOrDefaultAsync(p => p.Id == id);
        if (productionOrder == null)
            return Error.NotFound("Product.Order", "Product order not found");

        var waybill = await context.ProductionOrderWaybills
            .FirstOrDefaultAsync(p => p.AllocateProductionOrderId == productionOrder.Id);
        if (waybill == null)
            return Error.NotFound("Product.Order",
                "Waybill not created for production order {productionOrder.Id}");

        productionOrder.Status = AllocateProductionOrderStatus.WaybillSentToCustomer;
        productionOrder.WaybillSentToCustomerAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return Result.Success();
    }


    public async Task<Result> MarkAllocationProductionOrderAsDelivered(Guid id)
    {
        var productionOrder = await context.AllocateProductionOrders
            .FirstOrDefaultAsync(p => p.Id == id);
        if (productionOrder == null) return Error.NotFound("Product.Order", "Product order not found");

        productionOrder.DeliveredAt = DateTime.UtcNow;
        productionOrder.Status = AllocateProductionOrderStatus.Delivered;
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<Guid>> CreateProductOrderAllocation(AllocateProductionOrderRequest request)
    {
        var validation = await ValidateProductAllocation(request);
        if (!validation.IsSuccess) return validation.Errors;

        var allocationEntity = mapper.Map<AllocateProductionOrder>(request);
        await context.AllocateProductionOrders.AddAsync(allocationEntity);
        await context.SaveChangesAsync();

        var productionOrder = await context.ProductionOrders
            .AsSplitQuery()
            .Include(p => p.Products)
                .ThenInclude(p => p.FulfilledQuantities)
            .FirstOrDefaultAsync(p => p.Id == request.ProductionOrderId);

        if (productionOrder is null)
            return Error.NotFound("ProductionOrder.NotFound", "Production order not found");

        foreach (var reqProduct in request.Products)
        {
            var allocationProduct = productionOrder.Products
                .FirstOrDefault(p => p.ProductId == reqProduct.ProductId);

            if (allocationProduct is null)
                return Error.NotFound("ProductionOrder.ProductNotFound",
                    $"Product {reqProduct.ProductId} not found in this production order");

            foreach (var quantityToFulfill in reqProduct.FulfilledQuantities)
            {
                var finishedGoodsTransferNote = await context.FinishedGoodsTransferNotes
                    .FirstOrDefaultAsync(f => f.Id == quantityToFulfill.FinishedGoodsTransferNoteId);

                if (finishedGoodsTransferNote is null)
                    return Error.NotFound("FinishedGoodsTransferNote.NotFound",
                        $"Finished goods transfer note {quantityToFulfill.FinishedGoodsTransferNoteId} not found.");

                // Update product fulfilled quantities
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

                // Update transfer note allocation
                finishedGoodsTransferNote.AllocatedQuantity += quantityToFulfill.Quantity;
            }
        }

        foreach (var product in productionOrder.Products)
        {
            if (product.RemainingQuantity == 0 && !product.Fulfilled)
            {
                product.Fulfilled = true;
            }
        }

        if (productionOrder.Products.All(p => p.Fulfilled))
        {
            productionOrder.Status = ProductionOrderStatus.FullPackingReady;
            context.ProductionOrders.Update(productionOrder);
        }
        else if (productionOrder.Products.Any(p => p.Fulfilled))
        {
            productionOrder.Status = ProductionOrderStatus.PartialPackingReady;
            context.ProductionOrders.Update(productionOrder);
        }

        await context.SaveChangesAsync();

        return allocationEntity.Id;
    }


    public async Task<Result<Paginateable<IEnumerable<AllocateProductionOrderDto>>>> GetProductAllocations(bool? approved, int page,
        int pageSize, string searchQuery, Guid? productionOrderId)
    {
        var query = context.AllocateProductionOrders
            .IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(a => a.ProductionOrder)
            .ThenInclude(p => p.Customer)
            .Include(a => a.Products)
            .ThenInclude(p => p.FulfilledQuantities)
            .ThenInclude(p => p.FinishedGoodsTransferNote)
            .ThenInclude(p => p.BatchManufacturingRecord)
            .Include(a => a.Products)
            .ThenInclude(p => p.Product)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, b => b.ProductionOrder.Code);
        }

        if (approved.HasValue)
        {
            query = query.Where(q => q.Approved == approved.Value);
        }

        if (productionOrderId.HasValue)
        {
            query = query.Where(p => p.ProductionOrderId == productionOrderId.Value);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<AllocateProductionOrderDto>
        );
    }

    public async Task<Result<AllocateProductionOrderDto>> GetProductAllocation(Guid id)
    {
        return mapper.Map<AllocateProductionOrderDto>(
            await context.AllocateProductionOrders
                .IgnoreQueryFilters()
                .AsSplitQuery()
                .Include(a => a.ProductionOrder)
                .ThenInclude(p => p.Customer)
                .Include(a => a.Products)
                .ThenInclude(p => p.FulfilledQuantities)
                .ThenInclude(p => p.FinishedGoodsTransferNote)
                .ThenInclude(f => f.BatchManufacturingRecord)
                .Include(a => a.Products)
                .ThenInclude(p => p.Product)
                .Include(a => a.Products)
                .ThenInclude(p => p.ProductPacking)
                .ThenInclude(p => p.PackingLists)
                .Include(a => a.Products)
                .ThenInclude(p => p.ProductPacking)
                .ThenInclude(p => p.BasePackingUoM)
                .FirstOrDefaultAsync(p => p.Id == id)
        );
    }

    public async Task<Result> ValidateProductAllocation(AllocateProductionOrderRequest request)
    {
        // 1) Load the production order + products (as no-tracking; we're not persisting here)
        var productionOrder = await context.ProductionOrders
            .AsNoTracking()
            .Include(po => po.Products)
                .ThenInclude(p => p.FulfilledQuantities) // ensure RemainingQuantity is accurate if it's computed from these
            .FirstOrDefaultAsync(po => po.Id == request.ProductionOrderId);

        if (productionOrder is null)
            return Error.NotFound("ProductionOrder.NotFound", "Production order not found");

        // Quick lookup of products on this order
        var orderProductsById = productionOrder.Products
            .ToDictionary(p => $"{p.ProductId},{p.ProductPackingId}", p => p);

        // 2) Collect all FinishedGoodsTransferNote IDs in the request and fetch them in one go
        var allNoteIds = request.Products
            .SelectMany(p => p.FulfilledQuantities)
            .Select(q => q.FinishedGoodsTransferNoteId)
            .Distinct()
            .ToList();

        var notesById = await context.FinishedGoodsTransferNotes
            .AsNoTracking()
            .Where(n => allNoteIds.Contains(n.Id))
            .ToDictionaryAsync(n => n.Id, n => n);

        // 3) Ensure all referenced notes exist
        var missingNoteIds = allNoteIds.Where(id => !notesById.ContainsKey(id)).ToList();
        if (missingNoteIds.Count != 0)
            return Error.NotFound("FinishedGoodsTransferNote.NotFound",
                $"These finished goods transfer notes were not found: {string.Join(", ", missingNoteIds)}");

        // 4) Pre-compute the TOTAL requested per note across the whole request
        var requestedPerNote = request.Products
            .SelectMany(p => p.FulfilledQuantities)
            .GroupBy(q => q.FinishedGoodsTransferNoteId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        // 5) Validate each note has enough remaining for its total requested
        foreach (var kv in requestedPerNote)
        {
            var noteId = kv.Key;
            var totalRequestedFromNote = kv.Value;
            var note = notesById[noteId];

            if (totalRequestedFromNote <= 0)
                return Error.Validation("FinishedGoodsTransferNote.InvalidQuantity",
                    $"Requested allocation from note {noteId} must be > 0.");

            if (note.RemainingQuantity <= 0)
                return Error.Validation("FinishedGoodsTransferNote.NoRemaining",
                    $"Finished goods transfer note {noteId} has no remaining quantity.");

            if (totalRequestedFromNote > note.RemainingQuantity)
                return Error.Validation("FinishedGoodsTransferNote.OverAllocate",
                    $"Requesting {totalRequestedFromNote} from note {noteId}, but only {note.RemainingQuantity} remains.");
        }

        // 6) Per-product validations (membership, remaining, optional note↔product compatibility)
        foreach (var reqProduct in request.Products)
        {
            if (!orderProductsById.TryGetValue($"{reqProduct.ProductId},{reqProduct.ProductPackingId}", 
                    out var orderProduct))
            {
                return Error.NotFound("ProductionOrder.ProductNotFound",
                    $"Product {reqProduct.ProductId} and" +
                    $" Product packing {reqProduct.ProductPackingId} not found in this production order");
            }

            if (orderProduct.Fulfilled)
                return Error.Validation("ProductionOrder.ProductFulfilled",
                    $"Product {reqProduct.ProductId} has already been marked as fulfilled.");

            if (orderProduct.RemainingQuantity == 0)
                return Error.Validation("ProductionOrder.ProductFullyAllocated",
                    $"Product {reqProduct.ProductId} has already been allocated completely.");

            var totalToAllocateForProduct = reqProduct.FulfilledQuantities.Sum(q => q.Quantity);
            if (totalToAllocateForProduct <= 0)
                return Error.Validation("ProductionOrder.InvalidQuantity",
                    $"Total allocation for product {reqProduct.ProductId} must be > 0.");

            if (totalToAllocateForProduct > orderProduct.RemainingQuantity)
            {
                return Error.Validation("ProductionOrder.ProductOverAllocate",
                    $"Allocation quantity {totalToAllocateForProduct} is more than remaining {orderProduct.RemainingQuantity} for product {reqProduct.ProductId}.");
            }

            // Optional: if your FinishedGoodsTransferNote has a ProductId, ensure it matches this product
            foreach (var q in reqProduct.FulfilledQuantities)
            {
                var note = notesById[q.FinishedGoodsTransferNoteId];

                if (q.Quantity <= 0)
                    return Error.Validation("FinishedGoodsTransferNote.InvalidQuantity",
                        $"Quantity for note {q.FinishedGoodsTransferNoteId} must be > 0.");
            }
        }

        return Result.Success();
    }
}

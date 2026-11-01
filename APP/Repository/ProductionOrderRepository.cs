using DOMAIN.Entities.Materials.Batch;
using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Invoices;
using DOMAIN.Entities.Payments;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.ProformaInvoices;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ProductionOrderRepository(
    ApplicationDbContext context,
    IMapper mapper,
    IApprovalRepository approvalRepository)
    : IProductionOrderRepository
{
    public async Task<Result<Guid>> CreateProductionOrder(CreateProductionOrderRequest request)
    {
        if (
            request
                .Products.GroupBy(p => new { p.ProductId, p.ProductPackingId })
                .Any(g => g.Count() > 1)
        )
        {
            return Error.Validation(
                "Production.Order",
                "Production order product list "
                    + "cannot contain more than one of the same product and product packing"
            );
        }

        var productionOrder = mapper.Map<ProductionOrder>(request);
        var pricing = await ResolveProductPrices(productionOrder.CustomerId, productionOrder.Products);
        if (pricing.IsFailure) return pricing.Error;

        await context.AddAsync(productionOrder);
        await context.SaveChangesAsync();
        return productionOrder.Id;
    }

    /// <summary>
    /// Prices each line with the customer's active pricing agreement for that product/packing,
    /// falling back to the product's list price -- the same precedence customer quotations use,
    /// via <see cref="CustomerPricingResolver"/>, so a production order created directly never
    /// disagrees with one converted from a quotation.
    /// </summary>
    private async Task<Result> ResolveProductPrices(Guid customerId, List<ProductionOrderProducts> products)
    {
        var productIds = products.Select(item => item.ProductId).Distinct().ToList();
        var prices = await context.Products.IgnoreQueryFilters()
            .Where(item => productIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id);
        var baseCurrencyId = await CustomerPricingResolver.BaseCurrencyIdAsync(context);

        foreach (var item in products)
        {
            if (!prices.TryGetValue(item.ProductId, out var product))
                return Error.NotFound("Product.NotFound", "One or more products were not found.");
            var resolution = await CustomerPricingResolver.ResolveAsync(context, customerId, item.ProductId,
                item.ProductPackingId, product.Price, CustomerPricingResolver.ListPriceCurrency(product, baseCurrencyId), DateTime.UtcNow);
            if (resolution.Ambiguous)
                return Error.Conflict("CustomerPricing.Ambiguous", "Multiple active pricing agreements match.");
            item.UnitPrice = resolution.UnitPrice;
        }
        return Result.Success();
    }

    public async Task<Result<Paginateable<IEnumerable<ProductionOrderDto>>>> GetProductionOrders(
        int page,
        int pageSize,
        string searchQuery,
        ProductionOrderStatus? status
    )
    {
        var query = context
            .ProductionOrders.IgnoreQueryFilters()
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
            .Where(po => !po.DeletedAt.HasValue)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Code);
        }

        if (status.HasValue)
        {
            query = query.Where(q => q.Status == status.Value);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<ProductionOrderDto>
        );
    }

    public async Task<Result<ProductionOrderDetailDto>> GetProductionOrder(Guid id)
    {
        var productionOrder = await context
            .ProductionOrders.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(p => p.Products)
                .ThenInclude(p => p.Product)
            .Include(p => p.Customer)
            .Include(p => p.Products)
                .ThenInclude(p => p.ProductPacking)
                    .ThenInclude(p => p.PackingLists)
                        .ThenInclude(l => l.Uom)
            .Include(p => p.Products)
                .ThenInclude(p => p.ProductPacking)
                    .ThenInclude(p => p.BasePackingUoM)
            .Where(po => !po.DeletedAt.HasValue)
            .FirstOrDefaultAsync(po => po.Id == id);

        if (productionOrder == null)
            return Error.NotFound("ProductionOrder.NotFound", "Production Order not found");

        var productionOrderDto = mapper.Map<ProductionOrderDetailDto>(productionOrder);

        productionOrderDto.Invoice = mapper.Map<ProductionOrderInvoiceDto>(
            await context
                .Invoices.AsSplitQuery()
                .Include(p => p.ProformaInvoice)
                    .ThenInclude(p => p.AllocateProductionOrder)
                .FirstOrDefaultAsync(p =>
                    p.ProformaInvoice.AllocateProductionOrder.ProductionOrderId
                    == productionOrderDto.Id
                )
        );

        return productionOrderDto;
    }

    public async Task<Result> UpdateProductionOrder(Guid id, CreateProductionOrderRequest request)
    {
        var productionOrder = await context.ProductionOrders.FirstOrDefaultAsync(p => p.Id == id);
        if (productionOrder is null)
            return Error.NotFound("ProductionOrder.NotFound", "Production Order not found");

        productionOrder.Products = mapper.Map<List<ProductionOrderProducts>>(request.Products);
        mapper.Map(request, productionOrder);

        var pricing = await ResolveProductPrices(productionOrder.CustomerId, productionOrder.Products);
        if (pricing.IsFailure) return pricing.Error;

        context.ProductionOrders.Update(productionOrder);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteProductionOrder(Guid id, Guid userId)
    {
        var productionOrder = await context.ProductionOrders.FirstOrDefaultAsync(po => po.Id == id);
        if (productionOrder == null)
            return Error.NotFound("ProductionOrder.NotFound", "Production Order not found");

        productionOrder.DeletedAt = DateTime.UtcNow;
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
        var productionOrder = await context.AllocateProductionOrders.FirstOrDefaultAsync(po =>
            po.Id == request.AllocateProductionOrderId
        );
        if (productionOrder is null)
        {
            return Error.NotFound(
                "Allocation.ProductionOrder.NotFound",
                "Allocation production Order not found"
            );
        }

        var allocationApprovalGate = productionOrder.EnsureApprovedForProgression(
            "Production allocation"
        );
        if (allocationApprovalGate.IsFailure)
            return allocationApprovalGate.Error;

        var invoice = new ProformaInvoice
        {
            Code = request.Code,
            AllocateProductionOrderId = request.AllocateProductionOrderId,
            Products = request
                .Products.Select(p => new ProformaInvoiceProduct
                {
                    ProductId = p.ProductId,
                    Quantity = p.Quantity,
                })
                .ToList(),
        };
        await context.ProformaInvoices.AddAsync(invoice);
        await context.SaveChangesAsync();

        return invoice.Id;
    }

    public async Task<Result> SendProformaInvoiceToCustomer(Guid proformaInvoiceId, Guid userId)
    {
        var proformaInvoice = await context.ProformaInvoices.FirstOrDefaultAsync(p =>
            p.Id == proformaInvoiceId
        );
        if (proformaInvoice is null)
            return Error.NotFound("ProformaInvoice.notFound", "Proforma Invoice not found");

        proformaInvoice.Status = ProformaInvoiceStatus.SentToCustomer;
        proformaInvoice.LastUpdatedById = userId;
        //context.ProformaInvoices.Update(proformaInvoice);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<Paginateable<IEnumerable<ProformaInvoiceDto>>>> GetProformaInvoices(
        int page,
        int pageSize,
        string searchQuery,
        ProformaInvoiceStatus? status = null,
        bool? approved = null
    )
    {
        var query = context
            .ProformaInvoices.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(p => p.AllocateProductionOrder)
                .ThenInclude(p => p.ProductionOrder)
                    .ThenInclude(p => p.Customer)
            .Include(p => p.AllocateProductionOrder)
                .ThenInclude(p => p.ProductionOrder)
                    .ThenInclude(p => p.Products)
                        .ThenInclude(p => p.Product)
            .Include(p => p.AllocateProductionOrder)
                .ThenInclude(p => p.ProductionOrder)
                    .ThenInclude(p => p.Products)
                        .ThenInclude(p => p.ProductPacking)
            .Include(p => p.Products)
                .ThenInclude(p => p.Product)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(
                searchQuery,
                q => q.AllocateProductionOrder.ProductionOrder.Code
            );
        }

        if (approved.HasValue)
        {
            query = query.Where(q => q.AllocateProductionOrder.Approved == approved.Value);
        }

        if (status != null)
        {
            query = query.Where(q => q.Status == status);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<ProformaInvoiceDto>
        );
    }

    public async Task<Result<ProformaInvoiceDto>> GetProformaInvoice(Guid id)
    {
        var invoice = await context
            .ProformaInvoices.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(p => p.AllocateProductionOrder)
                .ThenInclude(p => p.ProductionOrder)
                    .ThenInclude(p => p.Customer)
            .Include(p => p.Products)
                .ThenInclude(p => p.Product)
            .Where(p => !p.DeletedAt.HasValue)
            .FirstOrDefaultAsync(p => p.Id == id);

        return invoice is null
            ? Error.NotFound("ProformaInvoice.NotFound", "Proforma Invoice not found")
            : mapper.Map<ProformaInvoiceDto>(invoice);
    }

    public async Task<Result> UpdateProformaInvoice(Guid id, CreateProformaInvoice request)
    {
        var invoice = await context
            .ProformaInvoices.Include(p => p.Products)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (invoice is null)
            return Error.NotFound("ProformaInvoice.NotFound", "Proforma Invoice not found");

        invoice.AllocateProductionOrderId = request.AllocateProductionOrderId;

        context.ProformaInvoiceProducts.RemoveRange(invoice.Products);

        invoice.Products = request
            .Products.Select(p => new ProformaInvoiceProduct
            {
                ProductId = p.ProductId,
                Quantity = p.Quantity,
                ProformaInvoiceId = invoice.Id,
            })
            .ToList();

        context.ProformaInvoices.Update(invoice);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeleteProformaInvoice(Guid id, Guid userId)
    {
        var invoice = await context.ProformaInvoices.FirstOrDefaultAsync(p => p.Id == id);
        if (invoice is null)
            return Error.NotFound("ProformaInvoice.NotFound", "Proforma Invoice not found");

        invoice.DeletedAt = DateTime.UtcNow;
        invoice.LastDeletedById = userId;
        context.ProformaInvoices.Update(invoice);

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<Guid>> CreateInvoice(CreateInvoice request)
    {
        var proforma = await context.ProformaInvoices
            .Include(p => p.Products)
                .ThenInclude(p => p.Product)
            .FirstOrDefaultAsync(p => p.Id == request.ProformaInvoiceId);
        if (proforma is null)
            return Error.NotFound("ProformaInvoice.NotFound", "Proforma Invoice not found");

        var proformaApprovalGate = proforma.EnsureApprovedForProgression("Proforma invoice");
        if (proformaApprovalGate.IsFailure)
            return proformaApprovalGate.Error;

        if (request.Amounts.Any(amount => amount.Amount <= 0))
            return Error.Validation("Invoice.Amount", "Invoice amounts must be greater than zero");
        if (request.Amounts.Select(amount => amount.CurrencyId).Distinct().Count() != request.Amounts.Count)
            return Error.Validation("Invoice.Currency", "Invoice currencies must be unique");
        if (request.Amounts.Count > 0)
        {
            var currencyIds = request.Amounts.Select(amount => amount.CurrencyId).ToList();
            if (await context.Currencies.CountAsync(currency => currencyIds.Contains(currency.Id)) != currencyIds.Count)
                return Error.NotFound("Invoice.Currency", "One or more invoice currencies were not found");
        }

        TermsOfPayment terms = null;
        if (request.TermsOfPaymentId.HasValue)
        {
            terms = await context.TermsOfPayments.FirstOrDefaultAsync(item => item.Id == request.TermsOfPaymentId);
            if (terms is null)
                return Error.NotFound("Invoice.TermsOfPayment", "Terms of payment not found");
        }

        var invoice = mapper.Map<Invoice>(request);
        invoice.Status = InvoiceStatus.Pending;
        invoice.DueDate = terms?.DueDays is { } dueDays
            ? DateTime.UtcNow.Date.AddDays(dueDays)
            : null;

        // Existing clients do not yet submit financial snapshots. Freeze the prices in effect
        // at invoice creation so later price edits cannot rewrite historical receivables.
        // Quantities are packs, and a customer's agreed price is per pack of its packing style,
        // so each line is quantity x the agreed price, or the list price when there is none.
        if (invoice.Amounts.Count == 0)
        {
            var amounts = await InvoiceAmountsAt(proforma, DateTime.UtcNow);
            invoice.Amounts.AddRange(amounts);
        }

        await context.Invoices.AddAsync(invoice);
        await context.SaveChangesAsync();
        return invoice.Id;
    }

    /// <summary>
    /// What a proforma's lines come to on a date, one amount per currency. A line uses the
    /// customer's agreed price for its product and packing style (in the agreement's currency)
    /// when one is in effect, else the product's list price in the list price's currency
    /// (the base currency for list prices saved before currencies were recorded).
    /// </summary>
    private async Task<List<InvoiceAmount>> InvoiceAmountsAt(ProformaInvoice proforma, DateTime asOf)
    {
        var allocation = await context.AllocateProductionOrders.AsNoTracking()
            .Include(item => item.ProductionOrder)
            .Include(item => item.Products)
            .FirstOrDefaultAsync(item => item.Id == proforma.AllocateProductionOrderId);
        var baseCurrencyId = await CustomerPricingResolver.BaseCurrencyIdAsync(context);

        var totals = new Dictionary<Guid, decimal>();
        foreach (var line in proforma.Products)
        {
            var packingId = allocation?.Products.FirstOrDefault(item => item.ProductId == line.ProductId)?.ProductPackingId;
            var listCurrencyId = CustomerPricingResolver.ListPriceCurrency(line.Product, baseCurrencyId);
            var resolution = allocation?.ProductionOrder is { } order
                ? await CustomerPricingResolver.ResolveAsync(context, order.CustomerId, line.ProductId, packingId, line.Product.Price, listCurrencyId, asOf)
                : new CustomerPricingResolution(line.Product.Price, listCurrencyId, false, false);
            var currencyId = resolution.CurrencyId;
            if (currencyId is null) continue;
            totals[currencyId.Value] = totals.GetValueOrDefault(currencyId.Value) + line.Quantity * resolution.UnitPrice;
        }

        return totals.Where(item => item.Value > 0)
            .Select(item => new InvoiceAmount { CurrencyId = item.Key, Amount = item.Value })
            .ToList();
    }

    public async Task<Result<Paginateable<IEnumerable<InvoiceDto>>>> GetInvoices(
        int page,
        int pageSize,
        string searchQuery
    )
    {
        var query = context
            .Invoices.AsSplitQuery()
            .Include(i => i.ProformaInvoice)
                .ThenInclude(p => p.AllocateProductionOrder)
            .Include(i => i.ProformaInvoice.Products)
                .ThenInclude(p => p.Product)
            .Include(i => i.Customer)
            .Include(i => i.TermsOfPayment)
            .Include(i => i.Amounts)
                .ThenInclude(amount => amount.Currency)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(
                searchQuery,
                i => i.Customer.Name,
                i => i.Customer.Address,
                i => i.Customer.Email
            );
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<InvoiceDto>
        );
    }

    public async Task<Result<InvoiceDto>> GetInvoice(Guid id)
    {
        var invoice = await context
            .Invoices.AsSplitQuery()
            .Include(i => i.ProformaInvoice)
                .ThenInclude(p => p.AllocateProductionOrder)
            .Include(i => i.ProformaInvoice.Products)
                .ThenInclude(p => p.Product)
            .Include(i => i.Customer)
            .Include(i => i.TermsOfPayment)
            .Include(i => i.Amounts)
                .ThenInclude(amount => amount.Currency)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice is null)
            return Error.NotFound("Invoice.NotFound", "Invoice not found");

        var dto = mapper.Map<InvoiceDto>(invoice);
        dto.Balances = await PaymentBalanceQuery.GetAsync(
            context,
            PayableType.CustomerInvoice,
            invoice.Id,
            invoice.Amounts.Select(amount => new PaymentBalanceQuery.Total(
                amount.CurrencyId,
                amount.Currency.Name,
                amount.Currency.Symbol,
                amount.Amount
            ))
        );
        return dto;
    }

    public async Task<Result> UpdateInvoice(Guid id, CreateInvoice request)
    {
        var invoice = await context.Invoices.FirstOrDefaultAsync(i => i.Id == id);
        if (invoice is null)
            return Error.NotFound("Invoice.NotFound", "Invoice not found");

        invoice.CustomerId = request.CustomerId;
        invoice.ProformaInvoiceId = request.ProformaInvoiceId;
        invoice.TermsOfPaymentId = request.TermsOfPaymentId;
        if (request.TermsOfPaymentId.HasValue)
        {
            var terms = await context.TermsOfPayments.FirstOrDefaultAsync(item => item.Id == request.TermsOfPaymentId);
            if (terms is null)
                return Error.NotFound("Invoice.TermsOfPayment", "Terms of payment not found");
            invoice.DueDate = terms.DueDays is { } dueDays
                ? invoice.CreatedAt.Date.AddDays(dueDays)
                : null;
        }

        context.Invoices.Update(invoice);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeleteInvoice(Guid id, Guid userId)
    {
        var invoice = await context.Invoices.FirstOrDefaultAsync(i => i.Id == id);
        if (invoice is null)
            return Error.NotFound("Invoice.NotFound", "Invoice not found");

        invoice.DeletedAt = DateTime.UtcNow;
        invoice.LastDeletedById = userId;

        context.Invoices.Update(invoice);
        await context.SaveChangesAsync();

        return Result.Success();
    }
    public async Task<Result> MarkAllocationProductionOrderAsLoaded(Guid id)
    {
        var productionOrder = await context.AllocateProductionOrders.FirstOrDefaultAsync(p =>
            p.Id == id
        );
        if (productionOrder == null)
            return Error.NotFound("Product.Order", "Product order not found");

        var approvalGate = productionOrder.EnsureApprovedForProgression("Production allocation");
        if (approvalGate.IsFailure)
            return approvalGate;

        productionOrder.LoadedAt = DateTime.UtcNow;
        productionOrder.Status = AllocateProductionOrderStatus.Loaded;
        context.AllocateProductionOrders.Update(productionOrder);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> CreateWaybillFromProductionOrder(
        CreateProductionOrderWaybill request,
        Guid id
    )
    {
        var productionOrder = await context.AllocateProductionOrders.FirstOrDefaultAsync(p =>
            p.Id == id
        );
        if (productionOrder == null)
            return Error.NotFound("Product.Order", "Product order not found");

        var approvalGate = productionOrder.EnsureApprovedForProgression("Production allocation");
        if (approvalGate.IsFailure)
            return approvalGate;

        if (await context.ProductionOrderWaybills.AnyAsync(p => p.AllocateProductionOrderId == id))
            return Error.Validation(
                "ProductionOrder.Waybill",
                "ProductionOrder.Waybill already exists for this allocation"
            );

        await context.ProductionOrderWaybills.AddAsync(
            new ProductionOrderWaybill
            {
                Code = request.Code,
                AllocateProductionOrderId = id,
                Comment = request.Comment,
            }
        );

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<
        Result<Paginateable<IEnumerable<ProductionOrderWaybillDto>>>
    > GetProductionOrderWaybills(
        int page,
        int pageSize,
        string searchQuery,
        Guid? allocateProductionOrderId = null
    )
    {
        var query = context
            .ProductionOrderWaybills.AsSplitQuery()
            .Include(w => w.AllocateProductionOrder)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(
                searchQuery,
                q => q.Comment,
                q => q.AllocateProductionOrder.ProductionOrder.Code
            );
        }

        if (allocateProductionOrderId.HasValue)
        {
            query = query.Where(q => q.AllocateProductionOrderId == allocateProductionOrderId);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<ProductionOrderWaybillDto>
        );
    }

    public async Task<Result<ProductionOrderWaybillDto>> GetProductionOrderWaybill(Guid id)
    {
        return mapper.Map<ProductionOrderWaybillDto>(
            await context
                .ProductionOrderWaybills.AsSplitQuery()
                .Include(w => w.AllocateProductionOrder)
                    .ThenInclude(w => w.Products)
                .Include(w => w.AllocateProductionOrder)
                    .ThenInclude(w => w.ProductionOrder)
                .FirstOrDefaultAsync(p => p.Id == id)
        );
    }

    public async Task<Result> SendWaybillToCustomer(Guid id)
    {
        var productionOrder = await context.AllocateProductionOrders.FirstOrDefaultAsync(p =>
            p.Id == id
        );
        if (productionOrder == null)
            return Error.NotFound("Product.Order", "Product order not found");

        var approvalGate = productionOrder.EnsureApprovedForProgression("Production allocation");
        if (approvalGate.IsFailure)
            return approvalGate;

        var waybill = await context.ProductionOrderWaybills.FirstOrDefaultAsync(p =>
            p.AllocateProductionOrderId == productionOrder.Id
        );
        if (waybill == null)
            return Error.NotFound(
                "Product.Order",
                "Waybill not created for production order {productionOrder.Id}"
            );

        productionOrder.Status = AllocateProductionOrderStatus.WaybillSentToCustomer;
        productionOrder.WaybillSentToCustomerAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> MarkAllocationProductionOrderAsDelivered(Guid id)
    {
        var productionOrder = await context.AllocateProductionOrders.FirstOrDefaultAsync(p =>
            p.Id == id
        );
        if (productionOrder == null)
            return Error.NotFound("Product.Order", "Product order not found");

        var approvalGate = productionOrder.EnsureApprovedForProgression("Production allocation");
        if (approvalGate.IsFailure)
            return approvalGate;

        productionOrder.DeliveredAt = DateTime.UtcNow;
        productionOrder.Status = AllocateProductionOrderStatus.Delivered;
        await context.SaveChangesAsync();
        return Result.Success();
    }

    /// <summary>
    /// Creates a packing list: packs of each order line taken from approved transfer notes. Orders and
    /// packing lists count packs; stock is held as yield, so each line is converted once, here, with the
    /// note's units per pack, and both figures are saved. The order, the notes and the packing list are
    /// saved together, so a failure leaves nothing half done.
    /// </summary>
    public async Task<Result<Guid>> CreateProductOrderAllocation(
        AllocateProductionOrderRequest request
    )
    {
        var plan = await PlanProductAllocation(request);
        if (plan.IsFailure)
            return Result.Failure<Guid>(plan.Error);

        var (productionOrder, notes, lines) = plan.Value;

        var allocationEntity = new AllocateProductionOrder
        {
            ProductionOrderId = productionOrder.Id,
            Products = AllocationProducts(lines),
        };
        TakeStock(productionOrder, notes, lines);

        if (productionOrder.Products.All(p => p.Fulfilled))
            productionOrder.Status = ProductionOrderStatus.FullPackingReady;
        else if (productionOrder.Products.Any(p => p.Fulfilled))
            productionOrder.Status = ProductionOrderStatus.PartialPackingReady;

        await context.AllocateProductionOrders.AddAsync(allocationEntity);
        await context.SaveChangesAsync();

        await approvalRepository.CreateInitialApprovalsAsync(
            nameof(AllocateProductionOrder),
            allocationEntity.Id
        );

        return allocationEntity.Id;
    }

    /// <summary>
    /// Changes a packing list while it is still waiting for approval: nothing loaded, no proforma invoice,
    /// no waybill. The stock and order packs its old lines took are given back first, then the new lines
    /// pass every check a new packing list passes and take theirs. Approval starts again, because what
    /// was being approved has changed. Everything is saved together, or nothing is.
    /// </summary>
    public async Task<Result> UpdateProductOrderAllocation(Guid id, AllocateProductionOrderRequest request)
    {
        var allocation = await context.AllocateProductionOrders
            .AsSplitQuery()
            .Include(a => a.Products)
                .ThenInclude(p => p.FulfilledQuantities)
            .Include(a => a.Approvals)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (allocation is null)
            return Error.NotFound("AllocateProductionOrder.NotFound", "Packing list not found.");
        if (request is null || request.ProductionOrderId != allocation.ProductionOrderId)
            return Error.Validation("AllocateProductionOrder.Order", "A packing list cannot be moved to another production order.");
        if (allocation.Approved || allocation.Status != AllocateProductionOrderStatus.Pending)
            return Error.Validation("AllocateProductionOrder.Locked", "Only a packing list that is still waiting for approval can be changed.");
        if (await context.ProformaInvoices.AnyAsync(p => p.AllocateProductionOrderId == id))
            return Error.Validation("AllocateProductionOrder.Invoiced", "This packing list already has a proforma invoice, so it cannot be changed.");
        if (await context.ProductionOrderWaybills.AnyAsync(w => w.AllocateProductionOrderId == id))
            return Error.Validation("AllocateProductionOrder.Waybill", "This packing list already has a waybill, so it cannot be changed.");

        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync()
            : null;

        var productionOrder = await context.ProductionOrders
            .AsSplitQuery()
            .Include(po => po.Products)
                .ThenInclude(p => p.FulfilledQuantities)
            .FirstOrDefaultAsync(po => po.Id == allocation.ProductionOrderId);
        if (productionOrder is null)
            return Error.NotFound("ProductionOrder.NotFound", "Production order not found");

        // Give back what the old lines took. The order and notes stay tracked, so the checks below see it.
        var oldNoteIds = allocation.Products.SelectMany(p => p.FulfilledQuantities).Select(q => q.FinishedGoodsTransferNoteId).Distinct().ToList();
        var oldNotes = await context.FinishedGoodsTransferNotes.Where(n => oldNoteIds.Contains(n.Id)).ToDictionaryAsync(n => n.Id);
        foreach (var product in allocation.Products)
        {
            var orderProduct = productionOrder.Products.FirstOrDefault(p =>
                p.ProductId == product.ProductId && p.ProductPackingId == product.ProductPackingId);
            foreach (var line in product.FulfilledQuantities)
            {
                var taken = orderProduct?.FulfilledQuantities.FirstOrDefault(q => q.FinishedGoodsTransferNoteId == line.FinishedGoodsTransferNoteId);
                if (taken is not null)
                {
                    taken.Quantity = Math.Max(0, taken.Quantity - line.Quantity);
                    taken.YieldQuantity = Math.Max(0, taken.YieldQuantity - line.YieldQuantity);
                }
                if (oldNotes.TryGetValue(line.FinishedGoodsTransferNoteId, out var note))
                    note.AllocatedQuantity = Math.Max(0, note.AllocatedQuantity - line.YieldQuantity);
            }
        }
        foreach (var product in productionOrder.Products.Where(p => p.RemainingQuantity > 0))
            product.Fulfilled = false;

        var plan = await PlanProductAllocation(request);
        if (plan.IsFailure)
        {
            // Nothing was saved; drop the give-backs so they cannot be saved by anything else in this request.
            context.ChangeTracker.Clear();
            return plan.Error;
        }

        var (_, notes, lines) = plan.Value;
        TakeStock(productionOrder, notes, lines);
        foreach (var product in productionOrder.Products)
            foreach (var empty in product.FulfilledQuantities.Where(q => q.Quantity <= 0).ToList())
            {
                product.FulfilledQuantities.Remove(empty);
                context.Remove(empty);
            }
        if (productionOrder.Products.All(p => p.Fulfilled))
            productionOrder.Status = ProductionOrderStatus.FullPackingReady;
        else if (productionOrder.Products.Any(p => p.Fulfilled || p.FulfilledQuantities.Count > 0))
            productionOrder.Status = ProductionOrderStatus.PartialPackingReady;
        else
            productionOrder.Status = ProductionOrderStatus.Pending;

        allocation.Products = AllocationProducts(lines);
        allocation.Approved = false;
        context.AllocateProductionOrderApprovals.RemoveRange(allocation.Approvals);
        allocation.Approvals = [];
        await context.SaveChangesAsync();

        await approvalRepository.CreateInitialApprovalsAsync(nameof(AllocateProductionOrder), allocation.Id);
        if (transaction is not null)
            await transaction.CommitAsync();
        return Result.Success();
    }

    /// <summary>The packing list's lines: one entry per product and packing style, each batch with its packs and yield.</summary>
    private static List<AllocateProductionOrderProduct> AllocationProducts(List<PlannedAllocationLine> lines) => lines
        .GroupBy(line => (line.OrderProduct.ProductId, line.OrderProduct.ProductPackingId))
        .Select(group => new AllocateProductionOrderProduct
        {
            ProductId = group.Key.ProductId,
            ProductPackingId = group.Key.ProductPackingId,
            FulfilledQuantities = group
                .Select(line => new AllocateProductQuantity
                {
                    FinishedGoodsTransferNoteId = line.NoteId,
                    Quantity = line.Packs,
                    YieldQuantity = line.Yield,
                    UnitsPerPack = line.UnitsPerPack,
                })
                .ToList(),
        })
        .ToList();

    /// <summary>Takes planned lines from the order (packs) and from the transfer notes (yield), and marks finished order lines.</summary>
    private static void TakeStock(ProductionOrder productionOrder, Dictionary<Guid, FinishedGoodsTransferNote> notes, List<PlannedAllocationLine> lines)
    {
        foreach (var line in lines)
        {
            var taken = line.OrderProduct.FulfilledQuantities.FirstOrDefault(q =>
                q.FinishedGoodsTransferNoteId == line.NoteId
            );
            if (taken is null)
            {
                line.OrderProduct.FulfilledQuantities.Add(
                    new ProductionOrderProductQuantity
                    {
                        FinishedGoodsTransferNoteId = line.NoteId,
                        Quantity = line.Packs,
                        YieldQuantity = line.Yield,
                        UnitsPerPack = line.UnitsPerPack,
                    }
                );
            }
            else
            {
                taken.Quantity += line.Packs;
                taken.YieldQuantity += line.Yield;
                taken.UnitsPerPack = line.UnitsPerPack;
            }

            notes[line.NoteId].AllocatedQuantity += line.Yield;
        }

        foreach (var product in productionOrder.Products.Where(product =>
                     product.RemainingQuantity == 0 && !product.Fulfilled))
        {
            product.Fulfilled = true;
        }
    }

    public async Task<
        Result<Paginateable<IEnumerable<AllocateProductionOrderDto>>>
    > GetProductAllocations(
        bool? approved,
        int page,
        int pageSize,
        string searchQuery,
        Guid? productionOrderId
    )
    {
        var query = context
            .AllocateProductionOrders.IgnoreQueryFilters()
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
            await context
                .AllocateProductionOrders.IgnoreQueryFilters()
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
                            .ThenInclude(l => l.Uom)
                .Include(a => a.Products)
                    .ThenInclude(p => p.ProductPacking)
                        .ThenInclude(p => p.BasePackingUoM)
                .FirstOrDefaultAsync(p => p.Id == id)
        );
    }

    /// <summary>A packing list line, checked and converted: packs for the order, yield for the stock.</summary>
    private sealed record PlannedAllocationLine(
        ProductionOrderProducts OrderProduct,
        Guid NoteId,
        decimal Packs,
        decimal UnitsPerPack,
        decimal Yield
    );

    /// <summary>Checks a packing list without saving anything. Returns the problem, or success.</summary>
    public async Task<Result> ValidateProductAllocation(AllocateProductionOrderRequest request)
    {
        var plan = await PlanProductAllocation(request);
        return plan.IsFailure ? Result.Failure(plan.Error) : Result.Success();
    }

    /// <summary>
    /// Every check a packing list must pass, and its lines converted to yield. Packs must be whole and
    /// above zero; an order line cannot get more packs than it still needs; a transfer note must be
    /// approved and received, hold the same product and packing style as the order line, and have the
    /// yield left for every pack taken from it across the whole packing list.
    /// </summary>
    private async Task<
        Result<(
            ProductionOrder Order,
            Dictionary<Guid, FinishedGoodsTransferNote> Notes,
            List<PlannedAllocationLine> Lines
        )>
    > PlanProductAllocation(AllocateProductionOrderRequest request)
    {
        if (request?.Products is null || request.Products.Count == 0)
            return Error.Validation("ProductionOrder.Allocation", "Add at least one product to the packing list.");

        var productionOrder = await context
            .ProductionOrders.AsSplitQuery()
            .Include(po => po.Products)
                .ThenInclude(p => p.FulfilledQuantities)
            .FirstOrDefaultAsync(po => po.Id == request.ProductionOrderId);

        if (productionOrder is null)
            return Error.NotFound("ProductionOrder.NotFound", "Production order not found");

        var approvalGate = productionOrder.EnsureApprovedForProgression("Production order");
        if (approvalGate.IsFailure)
            return approvalGate.Error;

        var noteIds = request
            .Products.SelectMany(p => p.FulfilledQuantities ?? [])
            .Select(q => q.FinishedGoodsTransferNoteId)
            .Distinct()
            .ToList();

        var notes = await context
            .FinishedGoodsTransferNotes.AsSplitQuery()
            .Include(n => n.BatchManufacturingRecord)
                .ThenInclude(b => b.ProductionScheduleProduct)
            .Include(n => n.ProductPacking)
                .ThenInclude(p => p.PackingLists)
                    .ThenInclude(l => l.Uom)
            .Where(n => noteIds.Contains(n.Id))
            .ToDictionaryAsync(n => n.Id);

        var missing = noteIds.Where(id => !notes.ContainsKey(id)).ToList();
        if (missing.Count != 0)
            return Error.NotFound(
                "FinishedGoodsTransferNote.NotFound",
                $"These finished goods transfer notes were not found: {string.Join(", ", missing)}"
            );

        var lines = new List<PlannedAllocationLine>();
        foreach (var requested in request.Products)
        {
            var orderProduct = productionOrder.Products.FirstOrDefault(p =>
                p.ProductId == requested.ProductId && p.ProductPackingId == requested.ProductPackingId
            );
            if (orderProduct is null)
                return Error.NotFound(
                    "ProductionOrder.ProductNotFound",
                    $"Product {requested.ProductId} in packing style {requested.ProductPackingId} is not on this order."
                );
            if (orderProduct.Fulfilled || orderProduct.RemainingQuantity <= 0)
                return Error.Validation(
                    "ProductionOrder.ProductFullyAllocated",
                    $"Product {requested.ProductId} already has every pack it was ordered."
                );

            var quantities = requested.FulfilledQuantities ?? [];
            if (quantities.Count == 0)
                return Error.Validation(
                    "ProductionOrder.InvalidQuantity",
                    $"Choose the batches product {requested.ProductId} is packed from."
                );

            foreach (var group in quantities.GroupBy(q => q.FinishedGoodsTransferNoteId))
            {
                var packs = group.Sum(q => q.Quantity);
                if (group.Any(q => q.Quantity <= 0 || q.Quantity != decimal.Truncate(q.Quantity)))
                    return Error.Validation(
                        "FinishedGoodsTransferNote.InvalidQuantity",
                        $"Packs taken from transfer note {group.Key} must be a whole number above zero."
                    );

                var note = notes[group.Key];
                var noteGate = note.EnsureApprovedForProgression("Finished goods transfer note");
                if (noteGate.IsFailure)
                    return noteGate.Error;
                if (!note.IsApproved)
                    return Error.Validation(
                        "FinishedGoodsTransferNote.NotReceived",
                        $"Transfer note {note.TransferNoteNumber ?? note.Id.ToString()} has not been received into the store yet."
                    );
                if (note.BatchManufacturingRecord?.ProductionScheduleProduct?.ProductId != requested.ProductId)
                    return Error.Validation(
                        "FinishedGoodsTransferNote.WrongProduct",
                        $"Transfer note {note.TransferNoteNumber ?? note.Id.ToString()} is for a different product."
                    );
                if (requested.ProductPackingId is not null && note.ProductPackingId != requested.ProductPackingId)
                    return Error.Validation(
                        "FinishedGoodsTransferNote.WrongPacking",
                        $"Transfer note {note.TransferNoteNumber ?? note.Id.ToString()} is packed in a different packing style from the order."
                    );

                var unitsPerPack = FinishedGoodsUnits.UnitsPerPack(note);
                if (unitsPerPack is null)
                    return Error.Validation("FinishedGoodsTransferNote.Packing", PackConversion.CannotConvert(note.ProductPacking));

                lines.Add(new PlannedAllocationLine(orderProduct, note.Id, packs, unitsPerPack.Value,
                    PackConversion.ToYield(packs, unitsPerPack.Value)));
            }

            var packsForProduct = lines.Where(line => line.OrderProduct == orderProduct).Sum(line => line.Packs);
            if (packsForProduct > orderProduct.RemainingQuantity)
                return Error.Validation(
                    "ProductionOrder.ProductOverAllocate",
                    $"{packsForProduct} packs were put on the list, but the order needs only {orderProduct.RemainingQuantity} more."
                );
        }

        // The same transfer note can feed several lines; its stock is checked once, for everything taken.
        foreach (var byNote in lines.GroupBy(line => line.NoteId))
        {
            var note = notes[byNote.Key];
            var yieldTaken = byNote.Sum(line => line.Yield);
            if (yieldTaken > note.RemainingQuantity)
            {
                var unitsPerPack = byNote.First().UnitsPerPack;
                var packsLeft = decimal.Floor(Math.Max(note.RemainingQuantity, 0) / unitsPerPack);
                return Error.Validation(
                    "FinishedGoodsTransferNote.OverAllocate",
                    $"Transfer note {note.TransferNoteNumber ?? note.Id.ToString()} has {packsLeft} packs left; "
                        + $"{byNote.Sum(line => line.Packs)} were put on the list."
                );
            }
        }

        return (productionOrder, notes, lines);
    }
}

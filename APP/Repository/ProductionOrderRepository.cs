using DOMAIN.Entities.Materials.Batch;
using APP.Services.Production;
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

public partial class ProductionOrderRepository(
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

        var deletedProduct = await DeletedProductGuard.Refuse(context, productIds);
        if (deletedProduct is not null) return deletedProduct;
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
            if (resolution.UnitPrice <= 0)
                return Error.Validation("Product.Price", "Set a price for this product packing style before creating an order.");
            item.UnitPrice = resolution.UnitPrice;
            item.PriceListRevisionId = resolution.PriceListRevisionId;
            item.PriceCurrencyId = resolution.CurrencyId;
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
                    !p.Voided && p.ProformaInvoice.AllocateProductionOrder.ProductionOrderId
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

    public Task<Result<Guid>> CreateProformaInvoice(CreateProformaInvoice request) =>
        FinishedGoodsStock.Write(context, PermissionKeys.CanGenerateProformaInvoice, null, () => CreateProformaInvoiceCore(request));

    private async Task<Result<Guid>> CreateProformaInvoiceCore(CreateProformaInvoice request)
    {
        if (request.Products is null || request.Products.Count == 0 || string.IsNullOrWhiteSpace(request.Code) || request.Code.Length > 100)
            return Error.Validation("ProformaInvoice.Document", "Enter a proforma number and every packed product line.");
        if (await context.ProformaInvoices.AnyAsync(p => p.DeletedAt == null && (p.AllocateProductionOrderId == request.AllocateProductionOrderId || p.Code == request.Code.Trim())))
            return Error.Conflict("ProformaInvoice.Exists", "A proforma already exists for this packing list or number. Correct an unsent document rather than duplicating it.");
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
            Code = request.Code.Trim(),
            AllocateProductionOrderId = request.AllocateProductionOrderId,
            Products = request
                .Products.Select(p => new ProformaInvoiceProduct
                {
                    ProductId = p.ProductId,
                    ProductPackingId = p.ProductPackingId,
                    Quantity = p.Quantity,
                })
                .ToList(),
        };
        var snapshot = await SnapshotProformaPrices(invoice);
        if (snapshot.IsFailure) return snapshot.Error;
        if (productionOrder.CancelledAt.HasValue) return Error.Validation("Invoice.Cancelled", "This packing list was cancelled.");
        var denied = await FinishedGoodsStock.Require(context, PermissionKeys.CanGenerateProformaInvoice);
        if (denied is not null) return denied;
        await context.ProformaInvoices.AddAsync(invoice);
        await context.SaveChangesAsync();

        return invoice.Id;
    }

    private async Task<Result> SnapshotProformaPrices(ProformaInvoice invoice)
    {
        var allocation = await context.AllocateProductionOrders.IgnoreAutoIncludes().AsNoTracking()
            .Include(item => item.Products).ThenInclude(p => p.FulfilledQuantities).Include(item => item.ProductionOrder).ThenInclude(order => order.Products)
            .FirstOrDefaultAsync(item => item.Id == invoice.AllocateProductionOrderId);
        if (allocation?.CancelledAt.HasValue == true) return Error.Validation("Invoice.Cancelled", "This packing list is cancelled.");
        if (allocation?.ProductionOrder is null) return Error.NotFound("Allocation.NotFound", "Production allocation not found.");
        var gate = allocation.EnsureApprovedForProgression("Production allocation");
        if (gate.IsFailure) return gate.Error;
        if (invoice.Products.GroupBy(line => new { line.ProductId, line.ProductPackingId }).Any(group => group.Count() > 1))
            return Error.Validation("Invoice.Packing", "Include each allocated product packing style once.");
        foreach (var line in invoice.Products)
        {
            var styles = allocation.Products.Where(item => item.ProductId == line.ProductId
                && (!line.ProductPackingId.HasValue || item.ProductPackingId == line.ProductPackingId)).ToList();
            if (styles.Count != 1) return Error.Validation("Invoice.Packing", "Choose the allocated packing style for each invoiced product.");
            line.ProductPackingId = styles[0].ProductPackingId;
            var saved = allocation.ProductionOrder.Products.FirstOrDefault(item => item.ProductId == line.ProductId && item.ProductPackingId == line.ProductPackingId);
            if (saved?.DiscountPercent is < 0 or > 100) return Error.Validation("Invoice.Discount", "Resolve the invalid order discount before invoicing.");
            // Orders with recorded price and currency remain fixed through fulfilment. Older rows retain their legacy resolution.
            if (saved?.UnitPrice is not null && saved.PriceCurrencyId.HasValue)
            {
                line.UnitPrice = saved.UnitPrice * (1 - saved.DiscountPercent / 100m); line.PriceCurrencyId = saved.PriceCurrencyId; line.PriceListRevisionId = saved.PriceListRevisionId;
            }
        }
        foreach (var line in invoice.Products.Where(p => !p.UnitPrice.HasValue || !p.PriceCurrencyId.HasValue))
        {
            var legacy = new ProductionOrderProducts { ProductId = line.ProductId, ProductPackingId = line.ProductPackingId };
            var priced = await ResolveProductPrices(allocation.ProductionOrder.CustomerId, [legacy]);
            if (priced.IsFailure) return priced;
            var discount = allocation.ProductionOrder.Products.FirstOrDefault(p => p.ProductId == line.ProductId && p.ProductPackingId == line.ProductPackingId)?.DiscountPercent ?? 0;
            if (discount is < 0 or > 100) return Error.Validation("Invoice.Discount", "Resolve the invalid order discount before invoicing.");
            line.UnitPrice = legacy.UnitPrice * (1 - discount / 100m); line.PriceCurrencyId = legacy.PriceCurrencyId; line.PriceListRevisionId = legacy.PriceListRevisionId;
        }
        if (invoice.Products.Count != allocation.Products.Count || invoice.Products.Any(line => line.Quantity <= 0
            || line.Quantity != allocation.Products.Single(p => p.ProductId == line.ProductId && p.ProductPackingId == line.ProductPackingId).FulfilledQuantities.Sum(q => q.Quantity)))
            return Error.Validation("Invoice.Quantity", "The proforma must match every product packing style and pack quantity on this packing list.");
        return Result.Success();
    }

    public Task<Result> SendProformaInvoiceToCustomer(Guid proformaInvoiceId, Guid userId) =>
        FinishedGoodsStock.Write(context, PermissionKeys.CanSendProformaInvoice, userId, () => SendProformaInvoiceCore(proformaInvoiceId, userId));

    private async Task<Result> SendProformaInvoiceCore(Guid proformaInvoiceId, Guid userId)
    {
        var proformaInvoice = await context.ProformaInvoices.FirstOrDefaultAsync(p =>
            p.Id == proformaInvoiceId
        );
        if (proformaInvoice is null)
            return Error.NotFound("ProformaInvoice.notFound", "Proforma Invoice not found");

        if (proformaInvoice.Status != ProformaInvoiceStatus.Pending)
            return Error.Validation("Invoice.Sent", "This proforma has already been sent or invoiced.");
        var denied = await FinishedGoodsStock.Require(context, PermissionKeys.CanSendProformaInvoice, userId);
        if (denied is not null) return denied;
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

    public Task<Result> UpdateProformaInvoice(Guid id, CreateProformaInvoice request) =>
        FinishedGoodsStock.Write(context, PermissionKeys.CanGenerateProformaInvoice, null, () => UpdateProformaInvoiceCore(id, request));

    private async Task<Result> UpdateProformaInvoiceCore(Guid id, CreateProformaInvoice request)
    {
        var invoice = await context
            .ProformaInvoices.Include(p => p.Products)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (invoice is null)
            return Error.NotFound("ProformaInvoice.NotFound", "Proforma Invoice not found");
        if (invoice.CustomerPoVerifiedAt.HasValue || invoice.Status != ProformaInvoiceStatus.Pending)
            return Error.Validation("ProformaInvoice.Immutable", "A sent or PO-verified proforma cannot be rewritten or deleted. Resolve it with a documented amendment.");


        if (request.Products is null || request.Products.Count == 0)
            return Error.Validation("ProformaInvoice.Products", "Select the packing list products.");
        var replacement = new ProformaInvoice { AllocateProductionOrderId = request.AllocateProductionOrderId,
            Products = request.Products.Select(p => new ProformaInvoiceProduct { ProductId = p.ProductId,
                ProductPackingId = p.ProductPackingId, Quantity = p.Quantity, ProformaInvoiceId = invoice.Id }).ToList() };
        var snapshot = await SnapshotProformaPrices(replacement);
        if (snapshot.IsFailure) return snapshot.Error;
        invoice.AllocateProductionOrderId = request.AllocateProductionOrderId;
        context.ProformaInvoiceProducts.RemoveRange(invoice.Products);
        invoice.Products = replacement.Products;

        context.ProformaInvoices.Update(invoice);
        var denied = await FinishedGoodsStock.Require(context, PermissionKeys.CanGenerateProformaInvoice);
        if (denied is not null) return denied;
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public Task<Result> DeleteProformaInvoice(Guid id, Guid userId) =>
        FinishedGoodsStock.Write(context, PermissionKeys.CanDeleteProformaInvoice, userId, () => DeleteProformaInvoiceCore(id, userId));

    private async Task<Result> DeleteProformaInvoiceCore(Guid id, Guid userId)
    {
        var invoice = await context.ProformaInvoices.FirstOrDefaultAsync(p => p.Id == id);
        if (invoice is null)
            return Error.NotFound("ProformaInvoice.NotFound", "Proforma Invoice not found");
        if (invoice.CustomerPoVerifiedAt.HasValue || invoice.Status != ProformaInvoiceStatus.Pending)
            return Error.Validation("ProformaInvoice.Immutable", "A sent or PO-verified proforma cannot be rewritten or deleted. Resolve it with a documented amendment.");


        invoice.DeletedAt = DateTime.UtcNow;
        invoice.LastDeletedById = userId;
        context.ProformaInvoices.Update(invoice);

        var denied = await FinishedGoodsStock.Require(context, PermissionKeys.CanDeleteProformaInvoice, userId);
        if (denied is not null) return denied;
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public Task<Result<Guid>> CreateInvoice(CreateInvoice request) =>
        FinishedGoodsStock.Write(context, PermissionKeys.CanCreateInvoice, null, () => CreateInvoiceCore(request));

    private async Task<Result<Guid>> CreateInvoiceCore(CreateInvoice request)
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

        if (proforma.Products.Count == 0 || proforma.Products.Any(p => !p.UnitPrice.HasValue || p.UnitPrice < 0 || p.Quantity <= 0 || !p.PriceCurrencyId.HasValue))
            return Error.Validation("Invoice.Price", "Every proforma line needs its saved pack price and currency; resolve unpriced historical documents before issuing an invoice.");
        var allocation = await context.AllocateProductionOrders.Include(a => a.ProductionOrder)
            .SingleAsync(a => a.Id == proforma.AllocateProductionOrderId);
        if (allocation.CancelledAt.HasValue) return Error.Validation("Invoice.Cancelled", "A cancelled packing list cannot be invoiced.");
        if (allocation.ProductionOrder.CustomerId != request.CustomerId)
            return Error.Validation("Invoice.Customer", "The invoice customer must match the order and customer PO.");
        if (!proforma.CustomerPoVerifiedAt.HasValue || !proforma.CustomerPoAttachmentId.HasValue)
            return Error.Validation("Invoice.CustomerPo", "Verify the customer's PO against this proforma before issuing its invoice.");
        if (await context.Invoices.AnyAsync(i => i.ProformaInvoiceId == proforma.Id && !i.Voided))
            return Error.Conflict("Invoice.Exists", "An invoice already exists for this proforma.");
        if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Length > 100)
            return Error.Validation("Invoice.Code", "Enter the invoice number (at most 100 characters).");
        if (await context.Invoices.AnyAsync(i => i.Code == request.Code.Trim()))
            return Error.Conflict("Invoice.Code", "This invoice number is already in use.");
        if (request.Amounts.Count != 0)
            return Error.Validation("Invoice.Snapshot", "Invoice amounts are calculated from the verified PO and saved proforma pack prices.");

        var selectedTermsId = request.TermsOfPaymentId ?? await context.Customers.Where(c => c.Id == request.CustomerId)
            .Select(c => c.TermsOfPaymentId).SingleOrDefaultAsync();
        TermsOfPayment terms = null;
        if (selectedTermsId.HasValue)
        {
            terms = await context.TermsOfPayments.FirstOrDefaultAsync(item => item.Id == selectedTermsId);
            if (terms is null) return Error.NotFound("Invoice.TermsOfPayment", "Terms of payment not found");
        }

        var invoice = mapper.Map<Invoice>(request);
        invoice.TermsOfPaymentId = selectedTermsId;
        invoice.Code = request.Code.Trim();
        invoice.Status = InvoiceStatus.Approved;
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
            if (amounts.IsFailure) return amounts.Error;
            invoice.Amounts.AddRange(amounts.Value);
        }

        invoice.Id = Guid.NewGuid();
        proforma.IssuedInvoiceId = invoice.Id;
        proforma.IssuedInvoiceCode = invoice.Code;
        proforma.InvoiceIssuedAt = DateTime.UtcNow;
        var denied = await FinishedGoodsStock.Require(context, PermissionKeys.CanCreateInvoice);
        if (denied is not null) return denied;
        proforma.Status = ProformaInvoiceStatus.Invoice;
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
    private async Task<Result<List<InvoiceAmount>>> InvoiceAmountsAt(ProformaInvoice proforma, DateTime asOf)
    {
        var allocation = await context.AllocateProductionOrders.AsNoTracking()
            .Include(item => item.ProductionOrder)
            .Include(item => item.Products)
            .FirstOrDefaultAsync(item => item.Id == proforma.AllocateProductionOrderId);
        var baseCurrencyId = await CustomerPricingResolver.BaseCurrencyIdAsync(context);

        var totals = new Dictionary<Guid, decimal>();
        foreach (var line in proforma.Products)
        {
            if (line.UnitPrice.HasValue && line.PriceCurrencyId.HasValue)
            {
                totals[line.PriceCurrencyId.Value] = totals.GetValueOrDefault(line.PriceCurrencyId.Value) + line.Quantity * line.UnitPrice.Value;
                continue;
            }
            var matches = allocation?.Products.Where(item => item.ProductId == line.ProductId
                && (!line.ProductPackingId.HasValue || item.ProductPackingId == line.ProductPackingId)).ToList();
            if (matches?.Count > 1) return Error.Validation("Invoice.Packing", "Choose the allocated packing style for each invoice product.");
            var packingId = line.ProductPackingId ?? matches?.FirstOrDefault()?.ProductPackingId;
            var listCurrencyId = CustomerPricingResolver.ListPriceCurrency(line.Product, baseCurrencyId);
            var resolution = allocation?.ProductionOrder is { } order
                ? await CustomerPricingResolver.ResolveAsync(context, order.CustomerId, line.ProductId, packingId, line.Product.Price, listCurrencyId, asOf)
                : new CustomerPricingResolution(line.Product.Price, listCurrencyId, false, false);
            if (resolution.Ambiguous) return Error.Conflict("CustomerPricing.Ambiguous", "Multiple active pricing agreements match this invoice line.");
            if (resolution.UnitPrice <= 0 || !resolution.CurrencyId.HasValue)
                return Error.Validation("Invoice.Price", "Set a price and currency for this product packing style before invoicing.");
            var currencyId = resolution.CurrencyId;
            if (currencyId is null) continue;
            totals[currencyId.Value] = totals.GetValueOrDefault(currencyId.Value) + line.Quantity * resolution.UnitPrice;
        }

        return totals.Select(item => new InvoiceAmount { CurrencyId = item.Key, Amount = item.Value })
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
                i => i.Code,
                i => i.ProformaInvoice.CustomerPoNumber,
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
        dto.Balances = invoice.Voided ? [] : await PaymentBalanceQuery.GetAsync(
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

    public Task<Result> UpdateInvoice(Guid id, CreateInvoice request) => Task.FromResult<Result>(
        Error.Validation("Invoice.Immutable", "Issued invoice snapshots are immutable. Use a documented void or financial adjustment."));

    public Task<Result> DeleteInvoice(Guid id, Guid userId) => Task.FromResult<Result>(
        Error.Validation("Invoice.Immutable", "Invoice history cannot be deleted. Use the documented void or financial adjustment task."));

    public Task<Result> MarkAllocationProductionOrderAsLoaded(Guid id) =>
        Task.FromResult<Result>(Error.Validation("FinishedGoods.LoadEvidence", "Record the vehicle, loading location and inspection through the loading task."));

    public Task<Result> CreateWaybillFromProductionOrder(CreateProductionOrderWaybill request, Guid id) =>
        new APP.Services.FinishedGoods.FinishedGoodsWorkflow(context).Waybill(id, request);

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

    public Task<Result> SendWaybillToCustomer(Guid id) =>
        FinishedGoodsStock.Write(context, PermissionKeys.CanSendWaybillToCustomer, null, () => SendWaybillToCustomerCore(id));

    private async Task<Result> SendWaybillToCustomerCore(Guid id)
    {
        var productionOrder = await context.AllocateProductionOrders.FirstOrDefaultAsync(p =>
            p.Id == id
        );
        if (productionOrder == null)
            return Error.NotFound("Product.Order", "Product order not found");

        if (productionOrder.CancelledAt.HasValue || productionOrder.DispatchedAt.HasValue || productionOrder.DeliveredAt.HasValue || productionOrder.WaybillSentToCustomerAt.HasValue)
            return Error.Validation("FinishedGoods.WaybillState", "A waybill can only be recorded as sent once, before departure, for an active packing list.");
        var approvalGate = productionOrder.EnsureApprovedForProgression("Production allocation");
        if (approvalGate.IsFailure)
            return approvalGate;

        var waybill = await context.ProductionOrderWaybills.FirstOrDefaultAsync(p =>
            p.AllocateProductionOrderId == productionOrder.Id && !p.Voided
        );
        if (waybill == null)
            return Error.NotFound(
                "Product.Order",
                "Waybill not created for production order {productionOrder.Id}"
            );

        productionOrder.Status = AllocateProductionOrderStatus.WaybillSentToCustomer;
        productionOrder.WaybillSentToCustomerAt = DateTime.UtcNow;
        var denied = await FinishedGoodsStock.Require(context, PermissionKeys.CanSendWaybillToCustomer);
        if (denied is not null) return denied;
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public Task<Result> MarkAllocationProductionOrderAsDelivered(Guid id) =>
        Task.FromResult<Result>(Error.Validation("FinishedGoods.DeliveryEvidence", "Record delivery quantities, receiver and proof of delivery through the delivery task."));

    /// <summary>
    /// Creates a packing list: packs of each order line taken from approved transfer notes. Orders and
    /// packing lists count packs; stock is held as yield, so each line is converted once, here, with the
    /// note's units per pack, and both figures are saved. The order, the notes and the packing list are
    /// saved together, so a failure leaves nothing half done.
    /// </summary>
    public Task<Result<Guid>> CreateProductOrderAllocation(AllocateProductionOrderRequest request) =>
        FinishedGoodsStock.Write(context, PermissionKeys.CanGeneratePackingList, null, () => CreateProductOrderAllocationCore(request));

    private async Task<Result<Guid>> CreateProductOrderAllocationCore(
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
            FefoExceptionReason = request.FefoExceptionReason?.Trim(),
            Products = AllocationProducts(lines),
        };
        TakeStock(productionOrder, notes, lines);

        if (productionOrder.Products.All(p => p.Fulfilled))
            productionOrder.Status = ProductionOrderStatus.FullPackingReady;
        else if (productionOrder.Products.Any(p => p.Fulfilled))
            productionOrder.Status = ProductionOrderStatus.PartialPackingReady;

        var denied = await FinishedGoodsStock.Require(context, PermissionKeys.CanGeneratePackingList);
        if (denied is not null) return denied;
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
    public Task<Result> UpdateProductOrderAllocation(Guid id, AllocateProductionOrderRequest request, Guid? actorId = null) =>
        FinishedGoodsStock.Write(context, PermissionKeys.CanGeneratePackingList, actorId, () => UpdateProductOrderAllocationCore(id, request, actorId));

    private async Task<Result> UpdateProductOrderAllocationCore(Guid id, AllocateProductionOrderRequest request, Guid? actorId)
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

        allocation.FefoExceptionReason = request.FefoExceptionReason?.Trim();
        allocation.Products = AllocationProducts(lines);
        allocation.Approved = false;
        context.AllocateProductionOrderApprovals.RemoveRange(allocation.Approvals);
        allocation.Approvals = [];
        var denied = await FinishedGoodsStock.Require(context, PermissionKeys.CanGeneratePackingList, actorId);
        if (denied is not null) return denied;
        await context.SaveChangesAsync();

        // A correction made while the packing list is in review is recorded on its review; it then stays in review.
        await APP.Services.Reviews.DocumentReviewGate.NoteEditAsync(context, nameof(AllocateProductionOrder), allocation.Id, actorId);
        await approvalRepository.CreateInitialApprovalsAsync(nameof(AllocateProductionOrder), allocation.Id);

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
                var eligibility = FinishedGoodsStock.Eligible(note);
                if (eligibility is not null) return eligibility;
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

        if (string.IsNullOrWhiteSpace(request.FefoExceptionReason))
        {
            foreach (var chosen in lines.GroupBy(l => l.OrderProduct.ProductPackingId))
            {
                var productId = chosen.First().OrderProduct.ProductId;
                var packingId = chosen.Key;
                var latestChosenExpiry = chosen.Max(l => notes[l.NoteId].BatchManufacturingRecord.ExpiryDate);
                var earlier = await context.FinishedGoodsTransferNotes.Include(n => n.BatchManufacturingRecord)
                    .Where(n => n.IsApproved && n.Approved && !n.IsOnHold && !n.RequiresStockReconciliation && n.ProductPackingId == packingId
                        && n.BatchManufacturingRecord.ProductionScheduleProduct.ProductId == productId
                        && (n.BatchManufacturingRecord.Status == DOMAIN.Entities.Products.Production.BatchManufacturingStatus.Approved
                            || n.BatchManufacturingRecord.Status == DOMAIN.Entities.Products.Production.BatchManufacturingStatus.Available)
                        && n.BatchManufacturingRecord.ExpiryDate > DateTime.UtcNow.Date
                        && n.BatchManufacturingRecord.ExpiryDate < latestChosenExpiry).ToListAsync();
                if (earlier.Any(n => n.RemainingQuantity - lines.Where(l => l.NoteId == n.Id).Sum(l => l.Yield) > 0))
                    return Error.Validation("FinishedGoods.Fefo", "Use available earlier-expiring stock first, or record the authorised FEFO exception reason.");
            }
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

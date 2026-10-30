using DOMAIN.Entities.Items.Requisitions;
using DOMAIN.Entities.JobRequests;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.ServiceProviders;
using F = APP.Services.Documents.DocumentText;

namespace APP.Services.Documents;

/// <summary>
/// The documents procurement sends out. Each one mirrors the desktop app's printed copy of the
/// same document, column for column, so the email and the printout never disagree:
/// <list type="bullet">
/// <item>Supplier quotation request — modules/quotation-requests/components/quotation-request-print.tsx</item>
/// <item>Vendor quotation request — modules/store-procurement/components/quotation-pages.tsx (RequestPrint)</item>
/// <item>Purchase order and proforma invoice request — modules/proforma-requests/components/purchase-order-print.tsx</item>
/// </list>
/// When a printed copy changes, change its document here too.
/// </summary>
public static class ProcurementDocuments
{
    private static string Salutation(string contactPerson, string name) =>
        string.IsNullOrWhiteSpace(contactPerson) ? name : contactPerson.Trim();

    // ------------------------------------------------------------------ quotation requests

    public static DocumentEmail SupplierQuotationRequest(SupplierQuotationRequest request, DateTime sentAt)
    {
        var supplier = request.Supplier;
        var document = new DocumentSheet
        {
            Title = "Quotation request",
            ToName = supplier.Name,
            ToLines = [supplier.ContactPerson, supplier.Email, supplier.ContactNumber, supplier.Country?.Name],
            Facts =
            [
                ("Materials", request.Items.Count.ToString()),
                ("Prices in", F.Currency(supplier.Currency?.Symbol, supplier.Currency?.Name)),
                ("Sent on", F.Date(sentAt)),
            ],
            Note = "Please quote your unit price for each material below, stating the unit the price is per, with availability, lead time and payment terms.",
            Columns =
            [
                new DocumentColumn("#", "6%"),
                new DocumentColumn("Material"),
                new DocumentColumn("Manufacturer"),
                new DocumentColumn("Quantity", AlignRight: true),
                new DocumentColumn("Unit price", "16%"),
                new DocumentColumn("Priced per", "12%"),
            ],
            Rows = request.Items.Select((item, index) => (IReadOnlyList<DocumentCell>)
            [
                (index + 1).ToString(),
                new DocumentCell(item.Material?.Name, item.Material?.Code ?? "No code"),
                item.DefaultManufacturer?.Name ?? F.Missing,
                F.Quantity(item.Quantity, item.UoM?.Symbol),
                "",
                "",
            ]).ToList(),
            EmptyText = "No materials on this request",
        };

        return new DocumentEmail
        {
            Subject = $"Quotation request from {CompanyProfile.Name}",
            Salutation = Salutation(supplier.ContactPerson, supplier.Name),
            Message = ["We would like a quotation for the materials listed below."],
            Document = document,
            FileName = $"Quotation request - {supplier.Name}",
        };
    }

    public static DocumentEmail VendorQuotationRequest(VendorQuotationRequest request, string requisitionCode)
    {
        var vendor = request.Vendor;
        var contact = string.Join(" · ", new[] { vendor.ContactPerson, vendor.Phone, vendor.Email }.Where(part => !string.IsNullOrWhiteSpace(part)));
        var document = new DocumentSheet
        {
            Title = "Quotation request",
            Reference = requisitionCode,
            Facts =
            [
                ("Vendor", vendor.Name),
                ("Contact", string.IsNullOrEmpty(contact) ? F.Missing : contact),
                ("Address", string.IsNullOrWhiteSpace(vendor.Address) ? F.Missing : vendor.Address),
                ("Items", request.Items.Count.ToString()),
                ("Prices in", F.Currency(vendor.Currency?.Symbol, vendor.Currency?.Name) ?? F.Missing),
            ],
            Note = "Please quote your unit price for each item below, stating the unit the price is per, with availability and delivery time.",
            Columns =
            [
                new DocumentColumn("#", "6%"),
                new DocumentColumn("Item"),
                new DocumentColumn("Quantity", AlignRight: true),
                new DocumentColumn("Unit price", "16%"),
                new DocumentColumn("Priced per", "12%"),
            ],
            Rows = request.Items.Select((item, index) => (IReadOnlyList<DocumentCell>)
            [
                (index + 1).ToString(),
                new DocumentCell(item.Item?.Name, item.Item?.Code),
                $"{F.Entered(item.Quantity)} {item.UoM?.Symbol}".Trim(),
                "",
                "",
            ]).ToList(),
            EmptyText = "No items on this request",
        };

        return new DocumentEmail
        {
            Subject = string.IsNullOrWhiteSpace(requisitionCode)
                ? $"Quotation request from {CompanyProfile.Name}"
                : $"Quotation request {requisitionCode} from {CompanyProfile.Name}",
            Salutation = Salutation(vendor.ContactPerson, vendor.Name),
            Message = ["We would like a quotation for the items listed below."],
            Document = document,
            FileName = string.IsNullOrWhiteSpace(requisitionCode) ? $"Quotation request - {vendor.Name}" : $"Quotation request {requisitionCode}",
        };
    }

    // ------------------------------------------------------------------ purchase orders

    /// <summary>
    /// The finalised order: lines, the charges and total recorded when it was finalised,
    /// amount in words and lines to sign on.
    /// </summary>
    public static DocumentEmail PurchaseOrder(PurchaseOrder order)
    {
        var document = OrderSheet(order, "Purchase order", order.Supplier?.Type);
        return new DocumentEmail
        {
            Subject = $"Purchase order {order.Code} from {CompanyProfile.Name}",
            Salutation = Salutation(order.Supplier?.ContactPerson, order.Supplier?.Name),
            Message = ExpectedDelivery(order) is { } expected
                ? [$"Please find our purchase order {order.Code} below. We expect delivery by {expected}."]
                : [$"Please find our purchase order {order.Code} below."],
            Document = document,
            FileName = $"Purchase order {order.Code}",
        };
    }

    /// <summary>The awarded lines and their value, asking the supplier for a proforma invoice.</summary>
    public static DocumentEmail ProformaInvoiceRequest(PurchaseOrder order)
    {
        var document = OrderSheet(order, "Proforma invoice request", null);
        return new DocumentEmail
        {
            Subject = $"Proforma invoice request {order.Code} from {CompanyProfile.Name}",
            Salutation = Salutation(order.Supplier?.ContactPerson, order.Supplier?.Name),
            Message = ["Your quotation has been awarded. Please send us a proforma invoice for the materials and prices below."],
            Document = document,
            FileName = $"Proforma invoice request {order.Code}",
        };
    }

    private static string ExpectedDelivery(PurchaseOrder order) =>
        F.Date(order.ExpectedDeliveryDate ?? order.EstimatedDeliveryDate);

    /// <summary>A recorded charge counts only when it was filled in: the desktop treats zero as not recorded.</summary>
    private static decimal? Recorded(decimal value) => value > 0 ? value : null;

    /// <param name="type">With a supplier type, the charges and total recorded when the order was finalised are shown, and lines to sign on.</param>
    private static DocumentSheet OrderSheet(PurchaseOrder order, string title, SupplierType? type)
    {
        var supplier = order.Supplier;
        var currency = F.Currency(supplier?.Currency?.Symbol, supplier?.Currency?.Name);

        var lines = order.Items.Select(item =>
        {
            var unit = item.UoM?.Symbol;
            return new
            {
                Item = item,
                Unit = unit,
                Cost = F.LineCost(item.Price, item.Quantity, unit, item.PriceUoM),
            };
        }).ToList();

        // No total is better than a wrong one: it is left out when any line cannot be costed.
        decimal? total = lines.All(line => line.Cost.HasValue) ? lines.Sum(line => line.Cost!.Value) : null;

        var grandTotal = Recorded(order.TotalCifValue);
        var freightOrTax = Recorded(order.SeaFreight);
        var totals = new List<DocumentTotal>
        {
            new(type == SupplierType.Foreign ? "Total FOB value" : "Order value", total.HasValue ? F.Money(total, currency) : "Not available"),
        };
        if (type == SupplierType.Foreign && grandTotal.HasValue)
        {
            totals.Add(new DocumentTotal("Insurance", F.Money(Recorded(order.Insurance), currency)));
            totals.Add(new DocumentTotal("Freight", F.Money(freightOrTax, currency)));
        }
        if (type == SupplierType.Local && grandTotal.HasValue && freightOrTax.HasValue)
            totals.Add(new DocumentTotal("Tax", F.Money(freightOrTax, currency)));
        if (type.HasValue && grandTotal.HasValue)
            totals.Add(new DocumentTotal(type == SupplierType.Foreign ? "Total CIF value" : "Grand total", F.Money(grandTotal, currency)));

        return new DocumentSheet
        {
            Title = title,
            Reference = order.Code,
            ToName = supplier?.Name,
            ToLines = [supplier?.ContactPerson, supplier?.Email, supplier?.ContactNumber, supplier?.Country?.Name],
            Facts =
            [
                ("Awarded on", F.Date(order.CreatedAt)),
                ("Currency", currency),
                ("Proforma invoice", order.ProFormaInvoiceNumber),
                ("Terms of payment", order.TermsOfPayment?.Name),
                ("Delivery mode", order.DeliveryMode?.Name),
                ("Delivery expected", ExpectedDelivery(order)),
            ],
            Columns =
            [
                new DocumentColumn("#", "6%"),
                new DocumentColumn("Material"),
                new DocumentColumn("Quantity", AlignRight: true),
                new DocumentColumn("Unit price", AlignRight: true),
                new DocumentColumn("Amount", AlignRight: true),
            ],
            Rows = lines.Select((line, index) => (IReadOnlyList<DocumentCell>)
            [
                (index + 1).ToString(),
                new DocumentCell(line.Item.Material?.Name, line.Item.Material?.Code ?? "No code"),
                F.Quantity(line.Item.Quantity, line.Unit),
                F.UnitPrice(line.Item.Price, currency, line.Item.PriceUoM),
                F.Money(line.Cost, currency),
            ]).ToList(),
            EmptyText = "No lines on this order",
            Totals = totals,
            AmountInWords = type.HasValue ? order.AmountInFigures?.Trim() : null,
            Signatures = type.HasValue ? ["Prepared by", "Checked by", "Approved by"] : [],
        };
    }

    // ------------------------------------------------------------------ services

    /// <summary>A job order's request to its chosen service provider for a proforma invoice.</summary>
    public static DocumentEmail ServiceProformaInvoiceRequest(JobOrder jobOrder, ServiceProvider provider, decimal? serviceCharge, string currency, string notes, DateTime requestedAt)
    {
        var document = new DocumentSheet
        {
            Title = "Proforma invoice request",
            Reference = jobOrder.Code,
            ToName = provider.Name,
            ToLines = [provider.Email, provider.Phone, provider.Address],
            Facts =
            [
                ("Job order", jobOrder.Code),
                ("Service", jobOrder.Service?.Name),
                ("Requested on", F.Date(requestedAt)),
                ("Service charge", serviceCharge.HasValue ? F.Money(serviceCharge, currency) : null),
                ("Currency", currency),
            ],
            Note = string.Join(" ", new[] { jobOrder.Description, string.IsNullOrWhiteSpace(notes) ? null : $"Notes: {notes.Trim()}" }
                .Where(part => !string.IsNullOrWhiteSpace(part))),
        };

        return new DocumentEmail
        {
            Subject = $"Proforma invoice request {jobOrder.Code} from {CompanyProfile.Name}",
            Salutation = provider.Name,
            Message = ["Your quotation has been selected. Please send us a proforma invoice for the service below at your earliest convenience."],
            Document = document,
            FileName = $"Proforma invoice request {jobOrder.Code}",
        };
    }
}

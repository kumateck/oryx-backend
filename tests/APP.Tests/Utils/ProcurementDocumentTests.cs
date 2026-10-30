using APP.Services.Documents;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.PurchaseOrders;
using Xunit;

namespace APP.Tests.Utils;

// Procurement emails used to attach their own PDF layout, which disagreed with the copy printed
// from the desktop app: different company details, "$" prices with no unit, no line amounts or
// totals, and a purchase order that asked the supplier for a quotation. Both now come from one
// document that mirrors the desktop print.
public class ProcurementDocumentTests
{
    [Theory]
    [InlineData(650_000_000, "mg", "650 kg")]
    [InlineData(1_500, "mg", "1.5 g")]
    [InlineData(250, "ml", "250 ml")]
    [InlineData(2_500, "ML", "2.5 L")]
    [InlineData(12, "pcs", "12 pcs")]
    public void Quantities_are_shown_in_their_largest_unit_like_the_print(decimal quantity, string unit, string expected)
    {
        Assert.Equal(expected, DocumentText.Quantity(quantity, unit));
    }

    [Fact]
    public void Money_keeps_two_decimals_but_never_rounds_a_real_amount_to_zero()
    {
        Assert.Equal("GH₵ 1,234.50", DocumentText.Money(1234.5m, "GH₵"));
        Assert.Equal("$ 0.0005", DocumentText.Money(0.0005m, "$"));
        Assert.Equal("–", DocumentText.Money(null, "$"));
    }

    [Fact]
    public void Unit_price_is_shown_as_quoted_with_its_unit()
    {
        Assert.Equal("$ 12.125 / kg", DocumentText.UnitPrice(12.125m, "$", "kg"));
    }

    [Fact]
    public void A_line_is_costed_in_the_unit_its_price_is_quoted_in()
    {
        Assert.Equal(13m, DocumentText.LineCost(1.3m, 10_000_000m, "mg", "kg"));
        Assert.Equal(50m, DocumentText.LineCost(5m, 10m, "pcs", null));
        Assert.Null(DocumentText.LineCost(5m, 10m, "mg", null));
    }

    [Fact]
    public void Purchase_order_email_carries_the_printed_document_with_totals_and_signatures()
    {
        var email = ProcurementDocuments.PurchaseOrder(Order(SupplierType.Foreign));
        var body = email.RenderBody();

        Assert.Equal("Purchase order PO-0042 from Entrance Pharmaceuticals & Research Centre", email.Subject);
        Assert.Equal("Purchase order PO-0042", email.FileName);
        Assert.Contains("Purchase order", body);
        Assert.Contains("PO-0042", body);
        Assert.Contains(CompanyProfile.Email, body);
        Assert.Contains("$ 1.3 / kg", body);
        Assert.Contains("$ 13.00", body);
        Assert.Contains("Total FOB value", body);
        Assert.Contains("Total CIF value", body);
        Assert.Contains("$ 20.00", body);
        Assert.Contains("Approved by", body);
        Assert.Contains("cid:" + CompanyProfile.LogoContentId, body);
        Assert.DoesNotContain("sales quotation", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Proforma_request_shows_order_value_without_recorded_charges_or_signatures()
    {
        var body = ProcurementDocuments.ProformaInvoiceRequest(Order(SupplierType.Foreign)).RenderBody();

        Assert.Contains("Order value", body);
        Assert.DoesNotContain("Total CIF value", body);
        Assert.DoesNotContain("Approved by", body);
    }

    [Fact]
    public void The_pdf_page_embeds_the_logo_instead_of_referring_to_an_email_part()
    {
        var page = ProcurementDocuments.PurchaseOrder(Order(SupplierType.Local)).Document.RenderPage();

        Assert.DoesNotContain("cid:", page);
        Assert.Contains("Grand total", page);
    }

    [Fact]
    public void Text_from_records_is_escaped()
    {
        var order = Order(SupplierType.Local);
        order.Supplier.Name = "Acme <Chemicals> & Co";

        var body = ProcurementDocuments.PurchaseOrder(order).RenderBody();

        Assert.Contains("Acme &lt;Chemicals&gt; &amp; Co", body);
        Assert.DoesNotContain("<Chemicals>", body);
    }

    private static PurchaseOrder Order(SupplierType type) => new()
    {
        Code = "PO-0042",
        CreatedAt = new DateTime(2026, 10, 2),
        ExpectedDeliveryDate = new DateTime(2026, 11, 20),
        Supplier = new Supplier
        {
            Name = "Acme Chemicals",
            ContactPerson = "Ama Mensah",
            Email = "sales@acme.invalid",
            Type = type,
            Currency = new Currency { Name = "US Dollar", Symbol = "$" },
        },
        TermsOfPayment = new TermsOfPayment { Name = "30 days" },
        DeliveryMode = new DeliveryMode { Name = "Sea" },
        TotalCifValue = 20m,
        Insurance = 2m,
        SeaFreight = 5m,
        AmountInFigures = "Twenty US dollars",
        Items =
        [
            new PurchaseOrderItem
            {
                Material = new Material { Name = "Paracetamol", Code = "RM-001" },
                UoM = new UnitOfMeasure { Symbol = "mg" },
                Quantity = 10_000_000m,
                Price = 1.3m,
                PriceUoM = "kg",
            },
        ],
    };
}

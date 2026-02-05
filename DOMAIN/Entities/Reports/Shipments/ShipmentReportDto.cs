using DOMAIN.Entities.Shipments;

namespace DOMAIN.Entities.Reports.Shipments;

public class ShipmentReportDto
{
    public int No { get; set; }

    public string SupplierName { get; set; }

    public decimal InvoiceAmount { get; set; }

    public List<string> Materials { get; set; }


    public DateTime ExpectedArrivalDate { get; set; }

    public string FreeDays { get; set; }

    public DateTime? DemurrageStarts { get; set; }

    public string ContainerSize { get; set; }

    public string BillOfLadingNo { get; set; }

    public string TransactionType { get; set; }

    public string Status { get; set; }
}

public class ShipmentReportFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<Guid>? SupplierIds { get; set; }
    public List<string>? Statuses { get; set; }
}
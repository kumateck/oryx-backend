using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Reports.FinishedGoodsTransferNotes;

public class FinishedGoodsTransferDetailedReportDto
{
    public int No { get; set; }
    public string ProductName { get; set; }
    public string ProductCode { get; set; }
    public string BatchNumber { get; set; }
    public decimal QuantityTransferred { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string PackingStyle { get; set; }
    public string UomName { get; set; }
    public string ProductionDepartment { get; set; }
    public string DestinationWarehouse { get; set; }
    public DateTime TransferDate { get; set; }
    public DateTime? AcceptedDate { get; set; }
    public string Status { get; set; }
}


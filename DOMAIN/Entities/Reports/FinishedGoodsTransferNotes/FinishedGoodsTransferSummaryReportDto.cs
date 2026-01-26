using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Reports.FinishedGoodsTransferNotes;

public class FinishedGoodsTransferSummaryReportDto
{
    public int No { get; set; }
    public string ProductName { get; set; }
    public string ProductCode { get; set; }
    public int NumberOfBatches { get; set; }
    public decimal TotalQuantity { get; set; }

    public string UomName { get; set; }

    public string ProductionDepartment { get; set; }
    public string DestinationWarehouse { get; set; }

    public DateTime TransferDate { get; set; }
    public DateTime? AcceptedDate { get; set; }
}

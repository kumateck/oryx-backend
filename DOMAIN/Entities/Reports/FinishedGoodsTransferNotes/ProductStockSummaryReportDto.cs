namespace DOMAIN.Entities.Reports.FinishedGoodsTransferNotes
{
    public class ProductStockSummaryReportDto
    {
        public int No { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public string Warehouse { get; set; }
        public string ProductionDepartment { get; set; }
        public int NumberOfBatches { get; set; }
        public decimal TotalQuantity { get; set; }
        public string UomName { get; set; }
    }
}
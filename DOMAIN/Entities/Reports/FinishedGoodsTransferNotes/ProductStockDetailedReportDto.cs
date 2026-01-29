using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DOMAIN.Entities.Reports.FinishedGoodsTransferNotes
{
    public class ProductStockDetailedReportDto
    {
        public int No { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public string BatchNumber { get; set; }
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal TotalQuantity { get; set; }
        public string UomName { get; set; }
        public string ProductionDepartment { get; set; }
        public string Warehouse { get; set; }
    }
}
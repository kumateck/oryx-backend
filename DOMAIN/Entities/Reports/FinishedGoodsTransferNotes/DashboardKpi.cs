using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DOMAIN.Entities.Reports.FinishedGoodsTransferNotes
{
    public class DashboardKpiReportDto
    {
        public ProductCountKpiDto ProductCount { get; set; }
        public int TotalCustomers { get; set; }
        public ProductionOrderKpiDto ProductionOrders { get; set; }
        public FgtnKpiDto FinishedGoodsTransferNotes { get; set; }
    }

    public class ProductCountKpiDto
    {
        public int BetaProducts { get; set; }
        public int NonBetaProducts { get; set; }
        public int TotalProducts { get; set; }
    }

    public class ProductionOrderKpiDto
    {
        public int PendingProductionOrders { get; set; }
        public int PartialPackingReady { get; set; }
        public int FullPackingReady { get; set; }
        public int TotalProductionOrders { get; set; }
    }

    public class FgtnKpiDto
    {
        public int PendingTransferNote { get; set; }
        public int AcceptedTransferNote { get; set; }
        public int TotalFgtnTransferNotes { get; set; }
    }

    public class DashboardFilterDto
    {
        public DateFilterType DateFilter { get; set; } = DateFilterType.AllTime;
        public DateTime? CustomStartDate { get; set; }
        public DateTime? CustomEndDate { get; set; }
        public Guid? MaterialId { get; set; }
        public Guid? ProductId { get; set; }
        public Guid? DepartmentId { get; set; }
    }

    public enum DateFilterType
    {
        AllTime = 0,
        OneWeek = 1,
        Custom = 2
    }

}
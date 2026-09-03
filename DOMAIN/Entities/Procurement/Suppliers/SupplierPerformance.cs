using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Procurement.Suppliers;

public class SupplierPerformanceRecord : BaseEntity
{
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal OnTimeDeliveryRate { get; set; }
    public decimal QualityRejectRate { get; set; }
    public decimal Score { get; set; }
}

public class ComputeSupplierPerformanceRequest
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    [Range(typeof(decimal), "0", "1")] public decimal OnTimeDeliveryWeight { get; set; } = 0.6m;
    [Range(typeof(decimal), "0", "1")] public decimal QualityWeight { get; set; } = 0.4m;
}

public class SupplierPerformanceDto : BaseDto
{
    public Guid SupplierId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int EvaluatedDeliveries { get; set; }
    public int OnTimeDeliveries { get; set; }
    public int EvaluatedBatches { get; set; }
    public int RejectedBatches { get; set; }
    public decimal OnTimeDeliveryRate { get; set; }
    public decimal QualityRejectRate { get; set; }
    public decimal Score { get; set; }
    public List<string> DataQualityWarnings { get; set; } = [];
}

public class SupplierComplianceDueDto
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; }
    public DateTime DueDate { get; set; }
    public int DaysUntilDue { get; set; }
}

public class SupplierSpendSummaryDto
{
    public Guid SupplierId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public Guid? BaseCurrencyId { get; set; }
    public string BaseCurrencyName { get; set; }
    public decimal TotalBase { get; set; }
    public List<SupplierMonthlySpendDto> Months { get; set; } = [];
    public List<string> DataQualityWarnings { get; set; } = [];
}

public class SupplierMonthlySpendDto
{
    public DateTime Month { get; set; }
    public decimal TotalBase { get; set; }
    public List<SupplierCurrencySpendDto> OriginalCurrencies { get; set; } = [];
}

public class SupplierCurrencySpendDto
{
    public Guid CurrencyId { get; set; }
    public string CurrencyName { get; set; }
    public decimal Amount { get; set; }
}

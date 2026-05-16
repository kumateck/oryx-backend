using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.PayrollResultLines;
using SHARED;

namespace DOMAIN.Entities.PayrollRunEmployees;

public class PayrollRunEmployeeDto : BaseDto
{
    public CollectionItemDto PayrollRun { get; set; }
    public CollectionItemDto Employee { get; set; }

    public decimal BasePay { get; set; }
    public decimal Gross { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalTaxes { get; set; }
    public decimal Net { get; set; }
    public decimal EmployerCost { get; set; }

    public EmployeeStatus EmployeeStatusAtRun { get; set; }
    public bool IsExcluded { get; set; }
    public string ExclusionReason { get; set; }

    public CollectionItemDto RetroSourcePeriod { get; set; }
    public List<PayrollResultLineDto> ResultLines { get; set; }
}
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.PayRollPeriods;

public class PayrollPeriodDto : BaseDto
{
    public string Code { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime CutoffDate { get; set; }
    public DateTime PaymentDueDate { get; set; }
    public PayrollPeriodStatus Status { get; set; }
    public DateTime? SoftClosedAt { get; set; }
    public DateTime? HardClosedAt { get; set; }
    public UserDto ClosedBy { get; set; }
    public CollectionItemDto PayrollCompany { get; set; }
    public CollectionItemDto PayrollCalendar { get; set; }
}
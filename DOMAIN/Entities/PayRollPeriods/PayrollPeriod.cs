using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.PayrollCalendars;
using DOMAIN.Entities.PayrollCompanies;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.PayRollPeriods;

public class PayrollPeriod : BaseEntity
{
    [StringLength(50)] public string Code { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime CutoffDate { get; set; }
    public DateTime PaymentDueDate { get; set; }
    public PayrollPeriodStatus Status { get; set; } = PayrollPeriodStatus.Open;
    public DateTime? SoftClosedAt { get; set; }
    public DateTime? HardClosedAt { get; set; }
    public Guid? ClosedById { get; set; }
    public User ClosedBy { get; set; }

    public Guid PayrollCompanyId { get; set; }
    public PayrollCompany PayrollCompany { get; set; }

    public Guid PayrollCalendarId { get; set; }
    public PayrollCalendar PayrollCalendar { get; set; }
}

public enum PayrollPeriodStatus
{
    Open = 0,
    SoftClosed = 1,
    HardClosed = 2
}
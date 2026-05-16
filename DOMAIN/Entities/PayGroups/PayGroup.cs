using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.PayrollCalendars;
using DOMAIN.Entities.PayrollCompanies;

namespace DOMAIN.Entities.PayGroups;

public class PayGroup : BaseEntity
{
    [StringLength(50)] public string Code { get; set; }
    [StringLength(255)] public string Name { get; set; }
    [StringLength(100)] public string CostCenterDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid PayrollCompanyId { get; set; }
    public PayrollCompany PayrollCompany { get; set; }

    public Guid PayrollCalendarId { get; set; }
    public PayrollCalendar PayrollCalendar { get; set; }

    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; }
}

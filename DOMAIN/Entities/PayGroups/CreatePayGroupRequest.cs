using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.PayGroups;

public class CreatePayGroupRequest
{
    [Required, StringLength(50)] public string Code { get; set; }
    [Required, StringLength(255)] public string Name { get; set; }
    [StringLength(100)] public string CostCenterDefault { get; set; }
    [Required] public Guid PayrollCompanyId { get; set; }
    [Required] public Guid PayrollCalendarId { get; set; }
    [Required] public Guid DefaultCurrencyId { get; set; }
}
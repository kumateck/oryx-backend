using DOMAIN.Entities.Base;
using SHARED;

namespace DOMAIN.Entities.PayGroups;

public class PayGroupDto : BaseDto
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string CostCenterDefault { get; set; }
    public bool IsActive { get; set; }
    public CollectionItemDto PayrollCompany { get; set; }
    public CollectionItemDto PayrollCalendar { get; set; }
    public CollectionItemDto DefaultCurrency { get; set; }
}
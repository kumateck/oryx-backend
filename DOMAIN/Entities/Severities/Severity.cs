using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Severities;

public class Severity: BaseEntity, IOrganizationType
{
    [StringLength(100)] public string Name { get; set; }
    [StringLength(100)] public string OrganizationName { get; set; }
}
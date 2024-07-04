using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.WorkRelateds;

public class WorkRelated : BaseEntity, IOrganizationType
{
    [StringLength(100)] public string Name { get; set; }
    public string OrganizationName { get; set; }
}
using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Sites;

public class Site : BaseEntity, IOrganizationType
{
    [StringLength(100)] public string Name { get; set; }
    [StringLength(100)] public string ModelType { get; set; }
    public bool IsDisabled { get; set; }
    public string OrganizationName { get; set; }
}
using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.FormTypes;

public class FormType : BaseEntity, IOrganizationType
{
    [StringLength(100)]public string Name { get; set; }
    [StringLength(100)]public string ModelType { get; set; }
    public bool IsDisabled { get; set; }
    [StringLength(100)]public string OrganizationName { get; set; }
}
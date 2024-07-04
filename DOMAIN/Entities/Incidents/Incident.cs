using System.ComponentModel.DataAnnotations;
using DOMAIN.Attribute;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.FormTypes;
using DOMAIN.Entities.Severities;
using DOMAIN.Entities.Sites;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.WorkRelateds;

namespace DOMAIN.Entities.Incidents;

public class Incident : BaseEntity, IOrganizationType
{
    public string Title { get; set; }
    public string Reference { get; set; }
    public DateTime DateOccured { get; set; }
    
    [RelatedEntityType(typeof(FormType))]
    public Guid? TypeId { get; set; } 
    public FormType FormType { get; set; }
    
    [RelatedEntityType(typeof(Site))]
    public Guid? SiteId { get; set; }
    public Site Site { get; set; }
    
    [RelatedEntityType(typeof(User))]
    public Guid? ResponsiblePersonId { get; set; }
    public User ResponsiblePerson { get; set; }
    
    [RelatedEntityType(typeof(Severity))]
    public Guid? SeverityId { get; set; }
    public Severity Severity { get; set; }
  
    [RelatedEntityType(typeof(WorkRelated))]
    public Guid? WorkRelatedId { get; set; }
    public WorkRelated WorkRelated { get; set; }
    
    [StringLength(1000)] public string RootCauseDescription { get; set; }
    [StringLength(1000)] public string Notes { get; set; }
    public string OrganizationName { get; set; }
    //public List<TagReference> Tags { get; set; }
}
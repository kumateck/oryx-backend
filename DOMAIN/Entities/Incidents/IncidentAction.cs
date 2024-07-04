using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Incidents;

public class IncidentAction : BaseEntity, IOrganizationType
{
    [StringLength(100)] public string Title { get; set; }
    [StringLength(100)]  public string Reference { get; set; }
    [StringLength(255)]  public string Description { get; set; }
    public DateTime DueDate { get; set; }
    public Guid ResponsiblePersonId { get; set; }
    public User ResponsiblePerson { get; set; }
    public Guid IncidentId { get; set; }
    public Incident Incident { get; set; }
    // public Guid? StatusId { get; set; }
    // public ActionStatus Status { get; set; }
    public List<IncidentActionComment> Comments { get; set; } = [];
    public List<IncidentActionHistory> History { get; set; } = [];
    public string OrganizationName { get; set; }
}
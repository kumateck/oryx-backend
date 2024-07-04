using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Incidents;

public class IncidentActionHistory : BaseEntity
{
    public Guid ActionId { get; set; }
    public IncidentAction Action { get; set; }
    [StringLength(255)] public string Activity { get; set; }
    public List<string> Attachments { get; set; }
}
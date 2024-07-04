using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Incidents;

public class IncidentActionComment : BaseEntity
{
    public Guid ActionId { get; set; }
    public IncidentAction Action { get; set; }
    [StringLength(1000)] public string Comment { get; set; }
}
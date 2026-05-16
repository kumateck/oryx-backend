using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Countries;

namespace DOMAIN.Entities.StatutoryProfiles;

public class StatutoryProfile : BaseEntity
{
    [StringLength(50)] public string Code { get; set; }
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    public bool IsActive { get; set; } = true;

    [StringLength(int.MaxValue)] 
    public string AttributesJson { get; set; }

    public Guid CountryId { get; set; }
    public Country Country { get; set; }
}
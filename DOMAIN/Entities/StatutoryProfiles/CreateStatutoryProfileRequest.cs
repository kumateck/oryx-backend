using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.StatutoryProfiles;

public class CreateStatutoryProfileRequest
{
    [Required, StringLength(50)] public string Code { get; set; }
    [Required, StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    public string AttributesJson { get; set; }
    [Required] public Guid CountryId { get; set; }
}
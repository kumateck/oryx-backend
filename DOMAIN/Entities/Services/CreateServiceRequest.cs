using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Services;

public class CreateServiceRequest
{
    [Required] public string Name { get; set; }
    public string Code { get; set; }
    [Required] public DateTime StartDate { get; set; }
    [Required] public DateTime EndDate { get; set; }
    public string Description { get; set; }
}
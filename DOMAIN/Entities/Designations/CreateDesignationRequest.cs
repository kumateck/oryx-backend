using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Designations;

public class CreateDesignationRequest
{
    [Required(ErrorMessage = "Name is required.")]
    public string Name { get; set; }

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string Description { get; set; }

    [Required(ErrorMessage = "At least one department must be selected.")]
    public List<Guid> DepartmentIds { get; set; } = [];
}
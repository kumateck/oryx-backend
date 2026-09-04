using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.ShiftAssignments;

public class CreateShiftCategoryRequest
{
    [Required, MaxLength(100)] public string Name { get; set; }
}

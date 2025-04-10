using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.EmergencyContacts;

public class EmergencyContactDto
{
    [StringLength(100)] [Required] public string FullName { get; set; }
    
    [StringLength(15)] [Required] public string ContactNumber { get; set; }
    
    [StringLength(20)] [Required] public string Relationship { get; set; }
    
    [StringLength(50)] [Required] public string ResidentialAddress { get; set; }
    
}
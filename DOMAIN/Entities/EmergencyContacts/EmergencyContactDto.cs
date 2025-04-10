using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.EmergencyContacts;

public class EmergencyContactDto
{
    [StringLength(100)] public string FullName { get; set; }
    
    [StringLength(15)] public string ContactNumber { get; set; }
    
    [StringLength(20)] public string Relationship { get; set; }
    
    [StringLength(50)] public string ResidentialAddress { get; set; }
    
}
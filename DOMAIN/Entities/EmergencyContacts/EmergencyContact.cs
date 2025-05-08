using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;

namespace DOMAIN.Entities.EmergencyContacts;

[Owned]
public class EmergencyContact
{
    
    [StringLength(100)] public string FullName { get; set; }
    
    [StringLength(15)] public string ContactNumber { get; set; }
    
    [StringLength(20)] public string Relationship { get; set; }
    
    [StringLength(50)] public string ResidentialAddress { get; set; }
    
}
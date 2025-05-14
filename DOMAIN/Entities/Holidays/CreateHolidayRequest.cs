using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Holidays;

public class CreateHolidayRequest 
{
    [Required, StringLength(200)] public string Name { get; set; }
    
    [Required] public DateTime Date { get; set; }
    
    public string Description { get; set; }
}

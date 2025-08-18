using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Customers;

public class Customer : BaseEntity
{
    public string Name { get; set; }
    
    public string Email { get; set; }
    
    public string Phone { get; set; }
    
    public string Address { get; set; }
}
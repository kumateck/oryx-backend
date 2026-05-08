namespace DOMAIN.Entities.Products.Equipments;

public class CreateEquipmentRequest
{
    public string Name { get; set; }
    public string EquipmentNumber { get; set; }
    public bool IsStorage { get; set; }
    public decimal CapacityQuantity { get; set; }
    public Guid? UoMId { get; set; }
    public bool RelevanceCheck { get; set; }
    public Guid DepartmentId { get; set; }
    public string Location { get; set; }
    public string Model { get; set; }
    public string SerialNumber { get; set; }
}
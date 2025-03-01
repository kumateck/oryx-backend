using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using SHARED;

namespace DOMAIN.Entities.Products.Equipments;

public class Equipment : BaseEntity
{
    [StringLength(100)] public string Name { get; set; }
    public string MachineId { get; set; }
    public bool IsStorage { get; set; }
    public decimal CapacityQuantity { get; set; }
    public Guid UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public bool RelevanceCheck{ get; set; }
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; }
    [StringLength(1000)] public string StorageLocation { get; set; }
}

public class EquipmentDto : BaseDto
{
    public string Name { get; set; }
    public string MachineId { get; set; }
    public bool IsStorage { get; set; }
    public decimal CapacityQuantity { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public bool RelevanceCheck{ get; set; }
    public CollectionItemDto Department { get; set; }
    public string StorageLocation { get; set; }
}
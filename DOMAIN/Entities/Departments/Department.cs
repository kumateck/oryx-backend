using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Warehouses;

namespace DOMAIN.Entities.Departments;

public class Department 
{
    public Guid Id { get; set; }
    [StringLength(100)] public string Code { get; set; }
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    public Guid? WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public Guid? LastUpdatedById { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? LastDeletedById { get; set; }
}
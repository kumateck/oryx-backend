using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Warehouses;

public class Warehouse : BaseEntity
{
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    public List<WarehouseLocation> Locations { get; set; } = [];
    public WarehouseType Type { get; set; }
}

public class WarehouseLocation : BaseEntity
{
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; }
    [StringLength(255)] public string Name { get; set; }
}

public enum WarehouseType
{
    Storage, 
    Production
}
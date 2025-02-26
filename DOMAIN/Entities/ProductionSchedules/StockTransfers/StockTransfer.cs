using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;

namespace DOMAIN.Entities.ProductionSchedules.StockTransfers;

public class StockTransfer : BaseEntity
{
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    [StringLength(1000)] public string Reason { get; set; }
    public decimal RequiredQuantity { get; set; }
    public List<StockTransferSource> Sources { get; set; } = [];
    public DateTime? ApprovedAt { get; set; }
}

public class StockTransferSource : BaseEntity
{
    public Guid StockTransferId { get; set; }
    public StockTransfer StockTransfer { get; set; }
    public Guid FromDepartmentId { get; set; }
    public Department FromDepartment { get; set; }
    public Guid ToDepartmentId { get; set; }
    public Department ToDepartment { get; set; }
    public decimal Quantity { get; set; }
    public Guid UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
}

public class StockTransferDto : BaseDto
{
    public MaterialDto Material { get; set; }
    [StringLength(1000)] public string Reason { get; set; }
    public decimal RequiredQuantity { get; set; }
    public List<StockTransferSourceDto> Sources { get; set; } = [];
    public DateTime? ApprovedAt { get; set; }
}

public class StockTransferSourceDto : BaseDto
{
    public DepartmentDto FromDepartment { get; set; }
    public DepartmentDto ToDepartment { get; set; }
    public decimal Quantity { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
}
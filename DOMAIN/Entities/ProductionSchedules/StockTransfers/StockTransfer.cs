using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.ProductionSchedules.StockTransfers;

public class StockTransfer : BaseEntity
{
    [StringLength(100)] public string Code { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid? UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    [StringLength(1000)] public string Reason { get; set; }
    public decimal RequiredQuantity { get; set; }
    public Guid? ProductId { get; set; }
    public Product Product { get; set; }
    public Guid? ProductionScheduleId { get; set; }
    public ProductionSchedule ProductionSchedule { get; set; }
    public Guid? ProductionActivityStepId { get; set; }
    public ProductionActivityStep ProductionActivityStep { get; set; }
    public List<StockTransferSource> Sources { get; set; } = [];
}

public enum StockTransferStatus
{
    InProgress,
    Approved,
    Issued
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
    public StockTransferStatus Status { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedById { get; set; }
    public User ApprovedBy { get; set; }
    public Guid? IssuedById { get; set; }
    public User IssuedBy { get; set; }
    public DateTime? IssuedAt { get; set; }
    
}

public class StockTransferDto : BaseDto
{
    public string Code { get; set; }
    public MaterialDto Material { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public CollectionItemDto Product { get; set; }
    public CollectionItemDto ProductionSchedule { get; set; }
    [StringLength(1000)] public string Reason { get; set; }
    public decimal RequiredQuantity { get; set; }
    public StockTransferStatus Status { get; set; }
    public List<StockTransferSourceDto> Sources { get; set; } = [];
}

public class StockTransferSourceDto : BaseDto
{
    public DepartmentDto FromDepartment { get; set; }
    public DepartmentDto ToDepartment { get; set; }
    public decimal Quantity { get; set; }
    public StockTransferStatus Status { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? IssuedAt { get; set; }
}

public class DepartmentStockTransferDto 
{
    public Guid Id { get; set; } 
    public MaterialDto Material { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public StockTransferStatus Status { get; set; }
    public decimal Quantity { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? IssuedAt { get; set; }
}
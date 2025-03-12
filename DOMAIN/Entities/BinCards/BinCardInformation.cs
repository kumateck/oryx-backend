using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products;

namespace DOMAIN.Entities.BinCards;

public class BinCardInformation:BaseEntity
{
    public Guid? BatchId { get; set; }
    public MaterialBatch Batch { get; set; }
    [StringLength(500)]public string Description { get; set; }
    [StringLength(500)]public string WayBill { get; set; }
    [StringLength(500)]public string ArNumber { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityIssued { get; set; }
    public decimal BalanceQuantity { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public Guid? UoMId { get; set; }
    public Product Product { get; set; }
    public Guid? ProductId { get; set; }
    
    public BinCardType Type {get; set; }
}

public enum BinCardType
{
    Material,
    Product
}

public class BinCardInformationDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public MaterialBatchDto MaterialBatch { get; set; }
    public string Description { get; set; }
    public string WayBill { get; set; }
    public string ArNumber { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityIssued { get; set; }
    public decimal BalanceQuantity { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public ProductDto Product { get; set; }
}
using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.BinCards;

public class BinCardInformation:BaseEntity
{
    [StringLength(500)]public string BatchNumber { get; set; }
    [StringLength(500)]public string Description { get; set; }
    [StringLength(500)]public string WayBill { get; set; }
    [StringLength(500)]public string ArNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityIssued { get; set; }
    public decimal BalanceQuantity { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public Guid? UoMId { get; set; }
    [StringLength(500)]public string ProductName { get; set; }
}

public class BinCardInformationDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string BatchNumber { get; set; }
    public string Description { get; set; }
    public string WayBill { get; set; }
    public string ArNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityIssued { get; set; }
    public decimal BalanceQuantity { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public string ProductName { get; set; }
}
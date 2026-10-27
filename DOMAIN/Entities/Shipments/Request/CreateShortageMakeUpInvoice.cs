namespace DOMAIN.Entities.Shipments.Request;

public sealed class CreateShortageMakeUpInvoice
{
    public string Code { get; set; } = string.Empty;
    public Guid ShipmentDiscrepancyItemId { get; set; }
    public decimal Quantity { get; set; }
    public Guid RequestKey { get; set; }
}

public sealed class ShipmentShortageBalanceDto
{
    public Guid ShipmentDiscrepancyId { get; set; }
    public Guid ShipmentDiscrepancyItemId { get; set; }
    public decimal DeclaredQuantity { get; set; }
    public decimal InitialPhysicalQuantity { get; set; }
    public decimal ConfirmedMakeUpQuantity { get; set; }
    public decimal InFlightMakeUpQuantity { get; set; }
    public decimal OutstandingQuantity { get; set; }
    public decimal AvailableToShipQuantity { get; set; }
}

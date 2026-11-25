namespace DOMAIN.Entities.Materials.Batch;

public enum MaterialStockTraceState { Store, Reserved, ProductionFloor, Consumed, InTransit }

/// <summary>A current, department-scoped snapshot. Reservations are a subset of store stock, never an additional position.</summary>
public sealed class MaterialStockTraceDto
{
    public Guid MaterialId { get; set; }
    public string MaterialCode { get; set; }
    public string MaterialName { get; set; }
    public MaterialKind MaterialKind { get; set; }
    public string Category { get; set; }
    public string Pharmacopoeia { get; set; }
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; }
    public string Unit { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime GeneratedAt { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal Received { get; set; }
    public decimal Issued { get; set; }
    public decimal LedgerClosing { get; set; }
    public decimal StoreStock { get; set; }
    public decimal ReservedStock { get; set; }
    public decimal FreeStock { get; set; }
    public decimal ExpiredStock { get; set; }
    public decimal UnavailableStock { get; set; }
    public decimal ProductionFloorStock { get; set; }
    public decimal ConsumedStock { get; set; }
    public decimal InTransitStock { get; set; }
    /// <summary>Ledger closing minus physical store stock. A nonzero value is a reconciliation gap, not a stock adjustment.</summary>
    public decimal LedgerGap { get; set; }
    public List<string> Gaps { get; set; } = [];
    public List<MaterialStockTraceRow> Rows { get; set; } = [];
    public List<MaterialStockTraceLedgerRow> LedgerRows { get; set; } = [];
}

public sealed class MaterialStockTraceRow
{
    public Guid Id { get; set; }
    public Guid MaterialBatchId { get; set; }
    public string MaterialBatchNumber { get; set; }
    public MaterialStockTraceState State { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; }
    public string Warehouse { get; set; }
    public string Shelf { get; set; }
    public string Reference { get; set; }
    public string Product { get; set; }
    public string ProductBatchNumber { get; set; }
    public DateTime? OccurredAt { get; set; }
    public string Actor { get; set; }
    public string Evidence { get; set; }
    public string ArNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? EffectiveExpiryDate { get; set; }
}

public sealed class MaterialStockTraceLedgerRow
{
    public Guid Id { get; set; }
    public Guid MaterialBatchId { get; set; }
    public string MaterialBatchNumber { get; set; }
    public DateTime OccurredAt { get; set; }
    public string Description { get; set; }
    public string Reference { get; set; }
    public string ArNumber { get; set; }
    public string Warehouse { get; set; }
    public string Product { get; set; }
    public string ProductBatchNumber { get; set; }
    public string Unit { get; set; }
    public decimal Received { get; set; }
    public decimal Issued { get; set; }
    public decimal Balance { get; set; }
}

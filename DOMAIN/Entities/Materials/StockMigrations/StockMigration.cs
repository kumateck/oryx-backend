using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;

namespace DOMAIN.Entities.Materials.StockMigrations;

/// <summary>
/// A department's opening stock, read from a spreadsheet and held for checking. Nothing touches the real
/// stock until it is approved; approving writes off all the department's current storage stock of the
/// kinds in the sheet and loads the sheet's rows in its place.
/// </summary>
public class StockMigration : BaseEntity
{
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; }

    [StringLength(500)]
    public string FileName { get; set; }

    public StockMigrationStatus Status { get; set; }

    /// <summary>The unit the sheet's weighed quantities are written in (kg, g or mg).</summary>
    [StringLength(10)]
    public string MassUnit { get; set; } = "kg";

    /// <summary>The unit the sheet's measured liquid quantities are written in (L or ml).</summary>
    [StringLength(10)]
    public string VolumeUnit { get; set; } = "L";

    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedById { get; set; }
    public User ApprovedBy { get; set; }

    public List<StockMigrationLine> Lines { get; set; } = [];
    public List<StockMigrationWriteOff> WriteOffs { get; set; } = [];
}

public enum StockMigrationStatus
{
    Draft = 0,
    Approved = 1,
    Discarded = 2,
}

public enum StockMigrationLineStatus
{
    Ok = 0,
    Warning = 1,
    Blocked = 2,
    /// <summary>No quantity in the sheet, so nothing is loaded.</summary>
    Skipped = 3,
    /// <summary>Left out by the person checking the sheet.</summary>
    Excluded = 4,
}

/// <summary>One sheet row: what the sheet said, what it was read as (and may be corrected to), and what is wrong with it.</summary>
public class StockMigrationLine : BaseEntity
{
    public Guid StockMigrationId { get; set; }
    public StockMigration StockMigration { get; set; }

    [StringLength(200)] public string Sheet { get; set; }
    public int RowNumber { get; set; }

    // As written in the sheet, kept for display.
    [StringLength(500)] public string SheetWarehouse { get; set; }
    [StringLength(200)] public string SheetShelf { get; set; }
    [StringLength(200)] public string SheetMaterialCode { get; set; }
    [StringLength(1000)] public string SheetMaterialName { get; set; }
    [StringLength(500)] public string SheetBatchNumber { get; set; }
    [StringLength(50)] public string SheetUnit { get; set; }
    [StringLength(100)] public string SheetQuantity { get; set; }
    [StringLength(100)] public string SheetManufacturingDate { get; set; }
    [StringLength(100)] public string SheetExpiryDate { get; set; }
    [StringLength(500)] public string Grn { get; set; }
    [StringLength(500)] public string ArNumber { get; set; }

    // The working values, read from the sheet and correctable on the verification page.
    [StringLength(200)] public string MaterialCode { get; set; }
    /// <summary>Set when someone picked the material, overriding the code.</summary>
    public Guid? MaterialId { get; set; }
    public Material Material { get; set; }
    [StringLength(500)] public string WarehouseName { get; set; }
    [StringLength(200)] public string ShelfName { get; set; }
    /// <summary>Set when someone picked the shelf, overriding the warehouse and shelf names.</summary>
    public Guid? ShelfId { get; set; }
    public WarehouseLocationShelf Shelf { get; set; }
    [StringLength(500)] public string BatchNumber { get; set; }
    /// <summary>Excel turned the batch number into a date or number in a way that cannot be undone.</summary>
    public bool BatchNumberDamaged { get; set; }
    [StringLength(50)] public string UnitSymbol { get; set; }
    /// <summary>The quantity as written, in the sheet's own unit (see the migration's mass and volume units).</summary>
    public decimal? QuantityInSheet { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool Excluded { get; set; }

    // Worked out on every check.
    public Guid? ResolvedMaterialId { get; set; }
    public Guid? ResolvedShelfId { get; set; }
    public Guid? ResolvedWarehouseId { get; set; }
    public Guid? UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    /// <summary>The quantity to load, in <see cref="UoM"/>.</summary>
    public decimal? Quantity { get; set; }
    public StockMigrationLineStatus Status { get; set; }
    [StringLength(4000)] public string Blockers { get; set; }
    [StringLength(4000)] public string Warnings { get; set; }
    /// <summary>Corrections made while reading the sheet, such as a month/year date Excel misread.</summary>
    [StringLength(4000)] public string Corrections { get; set; }

    public Guid? ImportedBatchId { get; set; }
    public MaterialBatch ImportedBatch { get; set; }
}

/// <summary>A shelf quantity set to zero when the migration was approved, kept so the write-off can be traced.</summary>
public class StockMigrationWriteOff : BaseEntity
{
    public Guid StockMigrationId { get; set; }
    public StockMigration StockMigration { get; set; }
    public Guid ShelfMaterialBatchId { get; set; }
    public ShelfMaterialBatch ShelfMaterialBatch { get; set; }
    public Guid MaterialBatchId { get; set; }
    public decimal PreviousQuantity { get; set; }
    public BatchStatus PreviousBatchStatus { get; set; }
}

public class StockMigrationDto
{
    public Guid Id { get; set; }
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; }
    public string FileName { get; set; }
    public StockMigrationStatus Status { get; set; }
    public string MassUnit { get; set; }
    public string VolumeUnit { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string ApprovedBy { get; set; }
    public int Total { get; set; }
    public int Ok { get; set; }
    public int Warning { get; set; }
    public int Blocked { get; set; }
    public int Skipped { get; set; }
    public int Excluded { get; set; }
    public int WrittenOff { get; set; }
    public List<string> Sheets { get; set; } = [];
    /// <summary>The storage warehouses whose stock approval writes off.</summary>
    public List<StockMigrationWarehouseDto> Warehouses { get; set; } = [];
}

public class StockMigrationWarehouseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public WarehouseType Type { get; set; }
    public bool WillBeWiped { get; set; }
    public List<StockMigrationShelfDto> Shelves { get; set; } = [];
}

public class StockMigrationShelfDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
}

public class StockMigrationLineDto
{
    public Guid Id { get; set; }
    public string Sheet { get; set; }
    public int RowNumber { get; set; }
    public string SheetWarehouse { get; set; }
    public string SheetShelf { get; set; }
    public string SheetMaterialCode { get; set; }
    public string SheetMaterialName { get; set; }
    public string SheetBatchNumber { get; set; }
    public string SheetUnit { get; set; }
    public string SheetQuantity { get; set; }
    public string SheetManufacturingDate { get; set; }
    public string SheetExpiryDate { get; set; }
    public string Grn { get; set; }
    public string ArNumber { get; set; }
    public string MaterialCode { get; set; }
    public Guid? MaterialId { get; set; }
    public string WarehouseName { get; set; }
    public string ShelfName { get; set; }
    public Guid? ShelfId { get; set; }
    public string BatchNumber { get; set; }
    public string UnitSymbol { get; set; }
    public decimal? QuantityInSheet { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool Excluded { get; set; }
    public Guid? ResolvedMaterialId { get; set; }
    public string ResolvedMaterialCode { get; set; }
    public string ResolvedMaterialName { get; set; }
    public MaterialKind? Kind { get; set; }
    public Guid? ResolvedShelfId { get; set; }
    public string ResolvedShelf { get; set; }
    public string ResolvedWarehouse { get; set; }
    public decimal? Quantity { get; set; }
    public string Unit { get; set; }
    public StockMigrationLineStatus Status { get; set; }
    public List<string> Blockers { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public List<string> Corrections { get; set; } = [];
    /// <summary>The materials sharing this row's code, when more than one does and none has been picked.</summary>
    public List<StockMigrationMaterialChoiceDto> MaterialChoices { get; set; } = [];
}

public class StockMigrationMaterialChoiceDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
}

public class StockMigrationLineFilter
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string Search { get; set; }
    public string Sheet { get; set; }
    public StockMigrationLineStatus? Status { get; set; }
}

/// <summary>A material's storage stock now, against what the migration would leave.</summary>
public class StockMigrationComparisonDto
{
    public Guid MaterialId { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public MaterialKind Kind { get; set; }
    public string Unit { get; set; }
    public decimal Current { get; set; }
    public decimal New { get; set; }
    public int CurrentBatches { get; set; }
    public int NewBatches { get; set; }
}

public class UpdateStockMigrationLineRequest
{
    public string MaterialCode { get; set; }
    public Guid? MaterialId { get; set; }
    public string WarehouseName { get; set; }
    public string ShelfName { get; set; }
    public Guid? ShelfId { get; set; }
    public string BatchNumber { get; set; }
    public string UnitSymbol { get; set; }
    public decimal? QuantityInSheet { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool Excluded { get; set; }
}

/// <summary>A change applied to many rows at once; only the fields given are changed.</summary>
public class BulkUpdateStockMigrationLinesRequest
{
    [Required] public List<Guid> LineIds { get; set; } = [];
    public Guid? ShelfId { get; set; }
    public bool? Excluded { get; set; }
}

public class UpdateStockMigrationUnitsRequest
{
    [Required] public string MassUnit { get; set; }
    [Required] public string VolumeUnit { get; set; }
}

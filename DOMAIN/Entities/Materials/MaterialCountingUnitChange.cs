using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Materials;

/// <summary>
/// Append-only record of a department changing the unit it counts a material in. The physical amount never changes:
/// every value the department stores in its counting unit is re-expressed, with the material's density when the
/// change crosses mass and volume. Lots, shelf rows, bin-card rows and holds keep their own recorded unit.
/// </summary>
public sealed class MaterialCountingUnitChange
{
    public Guid Id { get; set; }
    public Guid MaterialId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid MaterialDepartmentId { get; set; }
    public Guid ActorId { get; set; }
    public DateTime RecordedAt { get; set; }
    public Guid FromUnitId { get; set; }
    public Guid ToUnitId { get; set; }
    /// <summary>The density used, in g/mL; null when the change stayed within mass or within volume.</summary>
    public decimal? DensityGramsPerMilliliter { get; set; }
    [MaxLength(64)] public string PreviewHash { get; set; } = string.Empty;
    [MaxLength(2000)] public string Reason { get; set; } = string.Empty;
    /// <summary>The confirmed preview: every value before and after.</summary>
    public string PreviewJson { get; set; } = "{}";
}

/// <summary>One stored value, before and after the change.</summary>
public sealed record CountingUnitValue
{
    public string Label { get; init; } = string.Empty;
    public decimal Before { get; init; }
    public string BeforeUnit { get; init; } = string.Empty;
    public decimal After { get; init; }
    public string AfterUnit { get; init; } = string.Empty;
}

/// <summary>Values held by one record (a production batch or a requisition) that are re-expressed.</summary>
public sealed record CountingUnitRecord
{
    public Guid Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public List<CountingUnitValue> Values { get; init; } = [];
}

public sealed record MaterialCountingUnitPreview
{
    public Guid MaterialId { get; init; }
    public string Material { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public Guid DepartmentId { get; init; }
    public Guid FromUnitId { get; init; }
    public string FromUnit { get; init; } = string.Empty;
    public Guid ToUnitId { get; init; }
    public string ToUnit { get; init; } = string.Empty;
    /// <summary>The density used, in g/mL; null when no mass/volume conversion is needed.</summary>
    public decimal? DensityGramsPerMilliliter { get; init; }
    public string PreviewHash { get; init; } = string.Empty;
    /// <summary>True when something is stored in a unit other than the target and would be converted.</summary>
    public bool HasChanges { get; init; }
    /// <summary>Minimum, re-order and maximum levels.</summary>
    public List<CountingUnitValue> Levels { get; init; } = [];
    /// <summary>The department's stock, read from each lot's own unit. It is not written: shown to prove nothing changes physically.</summary>
    public CountingUnitValue Stock { get; init; } = new();
    /// <summary>Open holds, read from each hold's own unit and density. Not written.</summary>
    public CountingUnitValue Reserved { get; init; } = new();
    public List<CountingUnitRecord> Batches { get; init; } = [];
    public List<CountingUnitRecord> Requisitions { get; init; } = [];
    public List<CountingUnitRecord> ExtraRequests { get; init; } = [];
    public List<CountingUnitRecord> Packaging { get; init; } = [];
    public List<string> Blockers { get; init; } = [];
}

public sealed record ChangeMaterialCountingUnit
{
    public Guid UnitId { get; init; }
    [Required, StringLength(64, MinimumLength = 64)] public string PreviewHash { get; init; } = string.Empty;
    [Required, StringLength(2000, MinimumLength = 5)] public string Reason { get; init; } = string.Empty;
}

public sealed record MaterialCountingUnitResult
{
    public Guid ChangeId { get; init; }
    public bool Changed { get; init; }
}

namespace DOMAIN.Entities.ProductionSchedules;

/// <summary>
/// Optional when starting production. For a BOM or packaging line with substitutes, which material the batch uses:
/// that one is reserved now, requested on the stock requisition and issued by the warehouse.
/// </summary>
public class StartProductionRequest
{
    public List<StartProductionMaterialChoice> MaterialChoices { get; set; } = [];
}

/// <summary><see cref="MaterialId"/> is the line's main material; <see cref="UseMaterialId"/> the main itself or one of its substitutes.</summary>
public class StartProductionMaterialChoice
{
    public Guid MaterialId { get; set; }
    public Guid UseMaterialId { get; set; }
}

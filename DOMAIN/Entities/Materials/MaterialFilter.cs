using SHARED;

namespace DOMAIN.Entities.Materials;

public class MaterialFilter : PagedQuery
{
    public string SearchQuery { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<Guid> WarehouseIds { get; set; } = [];
    public MaterialKind? MaterialKind { get; set; }
}
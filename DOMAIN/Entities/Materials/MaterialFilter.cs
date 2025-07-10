using SHARED;

namespace DOMAIN.Entities.Materials;

public class MaterialFilter 
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<Guid> WarehouseIds { get; set; } = [];
}
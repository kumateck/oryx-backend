using SHARED;

namespace DOMAIN.Entities.Warehouses.Request;

public class GetSwapRequestsFilter : PagedQuery
{
    public string SearchQuery { get; set; }
    public Guid? DepartmentId { get; set; }
    public SwapRequestDirection? Direction { get; set; }
}

public enum SwapRequestDirection
{
    Incoming = 0,
    Outgoing = 1,
}

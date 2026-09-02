using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Procurement.Suppliers;

public class UpdateSupplierStatusRequest
{
    public SupplierStatus Status { get; set; }
    [Range(1, 3650)] public int RequalificationIntervalDays { get; set; } = 365;
}

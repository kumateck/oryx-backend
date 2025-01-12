namespace DOMAIN.Entities.Requisitions.Request;

public class CreateSourceRequisitionRequest
{
    public string Code { get; set; }
    public Guid RequisitionId { get; set; }
    public List<CreateSourceRequisitionItemRequest> Items { get; set; } = [];
}
public class CreateSourceRequisitionItemRequest
{
    public Guid MaterialId { get; set; }
    public Guid UoMId { get; set; }
    public decimal Quantity { get; set; }
    public ProcurementSource Source { get; set; }
    public List<CreateSourceRequisitionItemSupplierRequest> Suppliers { get; set; } = [];
}

public class CreateSourceRequisitionItemSupplierRequest
{
    public Guid SupplierId { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Base;

public class Operation : BaseEntity
{
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    public int Order { get; set; }
    public OperationAction? Action { get; set; }
}

public class OperationDto : BaseDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public int Order { get; set; }
    public OperationAction? Action { get; set; }
}
public enum OperationAction
{
    BmrAndBprRequisition = 0,
    StockRequisition = 1,
    FullReturn = 2,
    AdditionalStockRequest = 3,
    FinalPackingOrPartialReturn = 4,
    FinishedGoodsTransferNote = 5,
    Dispatch = 6
}

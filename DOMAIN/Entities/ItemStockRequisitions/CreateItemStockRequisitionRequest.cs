using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Items;

namespace DOMAIN.Entities.ItemStockRequisitions;

public class CreateItemStockRequisitionRequest
{
    public string Number { get; set; }
    [Required] public DateTime RequisitionDate { get; set; }
    
    [Required] public Guid RequestedById { get; set; }
    
    [Required]  public Guid DepartmentId { get; set; }
    
    public string Justification { get; set; }
    
    [Required, MinLength(1, ErrorMessage = "At least one item must be selected")]
    public List<StockItems> StockItems { get; set; }
}
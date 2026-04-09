using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.ItemGrns;

public class CreateItemGrnRequest
{
    [Required] public Guid ItemId { get; set; }

    [Required] public Guid SupplierId { get; set; }
    [Required] public string InvoiceNumber { get; set; } 
    [Required, Range(1, int.MaxValue)] public int QuantityReceived { get; set; }
}
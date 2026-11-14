namespace DOMAIN.Entities.BillOfMaterials.Request;

public class CreateBillOfMaterialRequest
{
    public Guid ProductId { get; set; }
    public List<CreateBoMItemsRequest> Items { get; set; } = [];
}

public class CreateBoMItemsRequest
{
    public Guid MaterialId { get; set; }
    public Guid? UoMId { get; set; }
    public bool IsSubstitutable { get; set; }  // Allows for substitution in production
    public string Grade { get; set; }
    public string CasNumber { get; set; }
    public decimal BaseQuantity { get; set; }
    public Guid? BaseUoMId { get; set; }
    public int Order { get; set; }
    public decimal PrescribedQuantity { get; set; }
    public bool UseDensityConversion { get; set; }
    /// <summary>Substitutes without their own quantity. Prefer <see cref="Substitutes"/>; these cannot be used until a quantity is set.</summary>
    public List<Guid> SubstituteMaterialIds { get; set; } = [];
    /// <summary>Substitutes with their own quantity. When present, <see cref="SubstituteMaterialIds"/> is ignored.</summary>
    public List<CreateBoMSubstituteRequest> Substitutes { get; set; } = [];
}

/// <summary>A substitute and the quantity of it that replaces the main material.</summary>
public class CreateBoMSubstituteRequest
{
    public Guid MaterialId { get; set; }
    /// <summary>Quantity per unit of product, in <see cref="BaseUoMId"/>.</summary>
    public decimal BaseQuantity { get; set; }
    /// <summary>The substitute's department stock unit, or mg when the line uses density conversion.</summary>
    public Guid? BaseUoMId { get; set; }
    /// <summary>Quantity for a full batch, in <see cref="BaseUoMId"/>.</summary>
    public decimal PrescribedQuantity { get; set; }
}

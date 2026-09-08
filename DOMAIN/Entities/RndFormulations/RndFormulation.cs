using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.RndProjects;
using SHARED;

namespace DOMAIN.Entities.RndFormulations;

public class RndFormulation : BaseEntity
{
    public Guid RndProjectId { get; set; }
    public RndProject RndProject { get; set; }
    public int Version { get; set; }
    public RndFormulationStatus Status { get; set; }
    public List<RndFormulationItem> Items { get; set; } = [];
}

public enum RndFormulationStatus
{
    Draft = 0,
    InReview = 1,
    Approved = 2,
    Superseded = 3,
}

public class RndFormulationItem : BaseEntity
{
    public Guid RndFormulationId { get; set; }
    public RndFormulation RndFormulation { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid? MaterialTypeId { get; set; }
    public MaterialType MaterialType { get; set; }
    [StringLength(255)] public string Grade { get; set; }
    [StringLength(255)] public string CasNumber { get; set; }
    public int Order { get; set; }
    public bool IsSubstitutable { get; set; }
    public decimal BaseQuantity { get; set; }
    public Guid? BaseUoMId { get; set; }
    public UnitOfMeasure BaseUoM { get; set; }
    public decimal PrescribedQuantity { get; set; }
    public decimal? Percentage { get; set; }
    public List<RndFormulationItemSubstitute> Substitutes { get; set; } = [];
}

public class RndFormulationItemSubstitute : BaseEntity
{
    public Guid RndFormulationItemId { get; set; }
    public RndFormulationItem RndFormulationItem { get; set; }
    public Guid SubstituteMaterialId { get; set; }
    public Material SubstituteMaterial { get; set; }
}

public class RndFormulationDto : BaseDto
{
    public Guid RndProjectId { get; set; }
    public int Version { get; set; }
    public RndFormulationStatus Status { get; set; }
    public List<RndFormulationItemDto> Items { get; set; } = [];
}

public class RndFormulationItemDto : BaseDto
{
    public CollectionItemDto Material { get; set; }
    public CollectionItemDto MaterialType { get; set; }
    public string Grade { get; set; }
    public string CasNumber { get; set; }
    public int Order { get; set; }
    public bool IsSubstitutable { get; set; }
    public decimal BaseQuantity { get; set; }
    public CollectionItemDto BaseUoM { get; set; }
    public decimal PrescribedQuantity { get; set; }
    public decimal? Percentage { get; set; }
    public List<Guid> SubstituteMaterialIds { get; set; } = [];
}

public class CreateRndFormulationItemRequest
{
    public Guid MaterialId { get; set; }
    public Guid? MaterialTypeId { get; set; }
    [StringLength(255)] public string Grade { get; set; }
    [StringLength(255)] public string CasNumber { get; set; }
    public int Order { get; set; }
    public bool IsSubstitutable { get; set; }
    public decimal BaseQuantity { get; set; }
    public Guid? BaseUoMId { get; set; }
    public decimal PrescribedQuantity { get; set; }
    public decimal? Percentage { get; set; }
    public List<Guid> SubstituteMaterialIds { get; set; } = [];
}

public class CreateRndFormulationRequest
{
    public List<CreateRndFormulationItemRequest> Items { get; set; } = [];
}

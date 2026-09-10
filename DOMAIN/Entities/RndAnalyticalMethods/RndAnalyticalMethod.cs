using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.RndProjects;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.RndAnalyticalMethods;

public class RndAnalyticalMethod : BaseEntity
{
    public Guid RndProjectId { get; set; }
    public RndProject RndProject { get; set; }
    public Guid? MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid? ProductId { get; set; }
    public Product Product { get; set; }
    [StringLength(255)] public string MethodName { get; set; }
    [StringLength(4000)] public string Description { get; set; }
    public RndAnalyticalMethodStatus Status { get; set; }
    public Guid? ValidationProtocolFormId { get; set; }
    public Form ValidationProtocolForm { get; set; }
    public DateTime? ValidatedAt { get; set; }
    public Guid? ValidatedById { get; set; }
    public User ValidatedBy { get; set; }
}

public enum RndAnalyticalMethodStatus
{
    Draft = 0,
    UnderValidation = 1,
    Validated = 2,
}

public class RndAnalyticalMethodDto : BaseDto
{
    public Guid RndProjectId { get; set; }
    public CollectionItemDto Material { get; set; }
    public CollectionItemDto Product { get; set; }
    public string MethodName { get; set; }
    public string Description { get; set; }
    public RndAnalyticalMethodStatus Status { get; set; }
    public Guid? ValidationProtocolFormId { get; set; }
    public DateTime? ValidatedAt { get; set; }
    public UserDto ValidatedBy { get; set; }
}

public class CreateRndAnalyticalMethodRequest
{
    public Guid? MaterialId { get; set; }
    public Guid? ProductId { get; set; }
    [StringLength(255)] public string MethodName { get; set; }
    [StringLength(4000)] public string Description { get; set; }
    public Guid? ValidationProtocolFormId { get; set; }
}

public class UpdateRndAnalyticalMethodStatusRequest
{
    public RndAnalyticalMethodStatus Status { get; set; }
}

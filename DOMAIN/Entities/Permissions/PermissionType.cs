using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Permissions;

public class PermissionType
{
    public Guid Id { get; set; }
    [StringLength(1000)] public string Key { get; set; }
    public int RoleClaimId { get; set; }
    [StringLength(100)] public string Type { get; set; }
}
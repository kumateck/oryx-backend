using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Auth;

public class BlacklistedToken : BaseEntity
{
    [StringLength(10000)] public string Token { get; set; }
    public DateTime ExpiryDate { get; set; }
}
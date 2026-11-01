using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Auth;

/// <summary>
/// Sets a new password for another user. Only the password administrator may call it
/// (see <c>UserPasswordAdminRepository.PasswordAdminEmail</c>).
/// </summary>
public class AdminResetUserPasswordRequest
{
    [Required] public Guid UserId { get; set; }
    [Required] public string NewPassword { get; set; }
}

using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Employees;

/// <summary>
/// An invitation to fill in the onboarding form. Only a hash of its single-use token is stored; the email address and
/// employee type come from the invitation, never from the form, and the token stops working once used, revoked or expired.
/// </summary>
public class EmployeeInvitation : BaseEntity
{
    [StringLength(320)] public string Email { get; set; }
    [StringLength(300)] public string Name { get; set; }
    public EmployeeType EmployeeType { get; set; }
    [StringLength(64)] public string TokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? EmployeeId { get; set; }
}

public class OnboardingInvitationDto
{
    public string Email { get; set; }
    public string Name { get; set; }
    public EmployeeType EmployeeType { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class OnboardingTokenRequest
{
    [Required] public string Token { get; set; }
}

public class CompleteOnboardingRequest
{
    [Required] public string Token { get; set; }
    [Required] public CreateEmployeeRequest Employee { get; set; }
}

public class OnboardingAvatarRequest
{
    [Required] public string Token { get; set; }
    /// <summary>The photograph as a base64 data URL, as the avatar upload takes it.</summary>
    [Required] public string File { get; set; }
}

public class EmployeeInvitationResult
{
    public string Email { get; set; }
    public bool Sent { get; set; }
    public string Problem { get; set; }
}

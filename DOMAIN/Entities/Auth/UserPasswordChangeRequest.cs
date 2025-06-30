namespace DOMAIN.Entities.Auth;

public class UserPasswordChangeRequest
{
    public string CurrentPassword { get; set; }
    
    public string NewPassword { get; set; }
    
    public string ConfirmNewPassword { get; set; }
}
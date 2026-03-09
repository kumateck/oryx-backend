namespace DOMAIN.Entities.Base;

public interface IVerifiable
{
    bool IsVerified { get; set; }
    DateTime? VerifiedAt { get; set; }
    Guid? VerifiedById { get; set; }
}

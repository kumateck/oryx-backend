namespace SHARED.Services.Identity;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? DepartmentId { get; }
}
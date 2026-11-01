using DOMAIN.Entities.Auth;
using SHARED;

namespace APP.IRepository;

public interface IUserPasswordAdminRepository
{
    /// <summary>
    /// Resets <paramref name="request"/>.UserId's password when <paramref name="callerId"/> is the
    /// password administrator. Returns Forbidden for every other caller.
    /// </summary>
    Task<Result> ResetUserPassword(AdminResetUserPasswordRequest request, Guid callerId);
}

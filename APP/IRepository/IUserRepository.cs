using APP.Utils;
using DOMAIN.Entities.Auth;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Users.Request;
using SHARED;
using SHARED.Requests;
namespace APP.IRepository;

public interface IUserRepository
{
    Task<Result<UserDto>> CreateUser(CreateUserRequest request);
    Task<Result<LoginResponse>> CreateNewUser(CreateClientRequest request);
    Task<Result<Paginateable<IEnumerable<UserWithRoleDto>>>> GetUsers(int page, int pageSize,
        string searchQuery);
    Task<Result<UserDto>> GetUser(Guid userId);
    Task<Result> UpdateUser(UpdateUserRequest request, Guid id, Guid userId);
    Task<Result> UpdateRolesOfUser(UpdateUserRoleRequest request, Guid id, Guid userId);
    Task<Result> DeleteUser(Guid id, Guid userId);
    Task<Result> ToggleDisableUser(Guid id, Guid userId);
    Task<Result> UploadAvatar(UploadFileRequest request, Guid userId);
    Task<Result> UploadSignature(UploadFileRequest request, Guid userId);
}
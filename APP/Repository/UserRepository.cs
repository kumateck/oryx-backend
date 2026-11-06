using APP.IRepository;
using APP.Services.Authorization;
using APP.Services.Storage;
using APP.Services.Token;
using APP.Utils;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using APP.Extensions;
using DOMAIN.Entities.Auth;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Users.Request;
using INFRASTRUCTURE.Context;
using Microsoft.Extensions.Caching.Memory;
using SHARED;
using SHARED.Requests;

namespace APP.Repository;

public class UserRepository(ApplicationDbContext context, UserManager<User> userManager, IJwtService jwtService, IBlobStorageService blobStorage, IMapper mapper, IMemoryCache cache)
    : IUserRepository
{
    public async Task<Result<Guid>> CreateUser(CreateUserRequest request, Guid actorId)
    {
        var actor = await CurrentPermissionSet.Load(context, actorId);
        if (AccessGuard.Missing(actor, PermissionKeys.CanCreateUser, "create users") is { } cannotCreate) return cannotCreate;
        var roleNames = request.RoleNames ?? [];
        var roleIds = await context.Roles.Where(item => roleNames.Contains(item.Name)).Select(item => item.Id).ToListAsync();
        if (roleIds.Count > 0)
        {
            if (AccessGuard.Missing(actor, PermissionKeys.CanAssignUserRole, "give a new user a role") is { } cannotAssign) return cannotAssign;
            if (await AccessGuard.RoleAssignment(context, actor, roleIds) is { } escalation) return escalation;
        }
        var user = mapper.Map<User>(request);
        user.CreatedAt = DateTime.UtcNow;

        if (!await EmailIsUnique(request.Email))
            return UserErrors.EmailNotUnique;

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return Error.Failure("User.Create", result.Errors.First().Description);
        }

        var roleName = context.Roles
            .Where(item => roleNames.Contains(item.Name))
            .Select(item => item.Name)
            .ToList();

        await userManager.AddToRolesAsync(user, roleName);
        await context.SaveChangesAsync();

        if (string.IsNullOrEmpty(request.Avatar)) return user.Id;

        if (!request.Avatar.IsValidBase64String()) return UserErrors.InvalidAvatar;

        var image = request.Avatar.ConvertFromBase64();
        var reference = $"{user.Id}.{image.FileName.Split(".").Last()}";
        var uploadResult = await blobStorage.UploadBlobAsync("avatar", image, reference);
        if (uploadResult.IsSuccess)
        {
            user.Avatar = reference;
            context.Users.Update(user);
            await context.SaveChangesAsync();
        }

        return user.Id;
    }

    public async Task<Result<LoginResponse>> CreateNewUser(CreateClientRequest request)
    {
        var user = mapper.Map<User>(request);
        user.UserName = request.Email;
        user.CreatedAt = DateTime.UtcNow;

        if (!await EmailIsUnique(request.Email))
            return UserErrors.EmailNotUnique;

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return Result.Failure<LoginResponse>(Error.Failure("User.Create", result.Errors.First().Description));
        }

        // An account created here has no role. Roles are given separately, by someone allowed to.
        await context.SaveChangesAsync();

        return await jwtService.AuthenticateNewUser(user);
    }

    public async Task<Result<Paginateable<IEnumerable<UserWithRoleDto>>>> GetUsers(int page, int pageSize,
        string searchQuery, bool? isDisabled, Guid? roleId = null, Guid? departmentId = null)
    {
        var query = context.Users.IgnoreQueryFilters().AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.FirstName, q => q.LastName, q => q.Email);
        }

        if (isDisabled.HasValue)
        {
            query = query.Where(q => q.IsDisabled == isDisabled.Value);
        }

        if (roleId.HasValue)
        {
            var userIdsInRole = context.UserRoles.Where(ur => ur.RoleId == roleId.Value).Select(ur => ur.UserId);
            query = query.Where(q => userIdsInRole.Contains(q.Id));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(q => q.DepartmentId == departmentId.Value);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<UserWithRoleDto>
        );
    }

    public async Task<Result<Paginateable<IEnumerable<UserWithRoleDto>>>> GetUsersByPermissions(
        List<string> permissionKeys, int page, int pageSize, string searchQuery)
    {
        // Only people who hold one of the keys now, by the same rule the server checks with; search narrows that list
        // and never widens it. Disabled and deleted users are left out here so every page is full and counts are right.
        var keys = permissionKeys.Where(key => !string.IsNullOrWhiteSpace(key)).Distinct().ToList();
        var query = APP.Services.Authorization.PermissionHolders.Query(context, keys);
        if (!string.IsNullOrEmpty(searchQuery))
            query = query.WhereSearch(searchQuery, q => q.FirstName, q => q.LastName, q => q.Email);
        query = query.OrderBy(user => user.FirstName).ThenBy(user => user.LastName).ThenBy(user => user.Id);

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<UserWithRoleDto>
        );
    }

    public async Task<Result<UserWithRoleDto>> GetUser(Guid userId)
    {
        var user = await context.Users
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return UserErrors.NotFound(userId);
        return mapper.Map<UserWithRoleDto>(user);
    }

    public async Task<Result<IEnumerable<UserWithRoleDto>>> GetUsersByRoleId(Guid roleId, Guid? departmentId = null)
    {
        var role = await context.Roles.FirstOrDefaultAsync(u => u.Id == roleId);
        if (role == null) return RoleErrors.NotFound(roleId);
        var roleName = role.Name ?? "";
        var usersInThisRole = await userManager.GetUsersInRoleAsync(roleName);

        if (departmentId is not null)
        {
            if (role.Type == DepartmentType.Production)
            {
                // Production-department roles are scoped to the caller's department
                // (e.g. the product's own production department).
                usersInThisRole = usersInThisRole
                    .Where(u => u.DepartmentId == departmentId)
                    .ToList();
            }
            else
            {
                // Non-production roles (e.g. QA/QC) are scoped to whichever
                // department(s) the role is explicitly associated with, since their
                // members won't sit in the caller's production department. If no
                // association is configured, fall back to unfiltered rather than
                // silently dropping a required responsible party.
                var roleDepartmentIds = await context
                    .RoleDepartments.Where(rd => rd.RoleId == roleId)
                    .Select(rd => rd.DepartmentId)
                    .ToListAsync();

                if (roleDepartmentIds.Count > 0)
                {
                    usersInThisRole = usersInThisRole
                        .Where(u =>
                            u.DepartmentId.HasValue
                            && roleDepartmentIds.Contains(u.DepartmentId.Value)
                        )
                        .ToList();
                }
            }
        }

        return mapper.Map<List<UserWithRoleDto>>(usersInThisRole);
    }

    public async Task<Result> UpdateUser(UpdateUserRequest request, Guid id, Guid userId)
    {
        var actor = await CurrentPermissionSet.Load(context, userId);
        if (AccessGuard.Missing(actor, PermissionKeys.CanEditUser, "edit users") is { } cannotEdit) return cannotEdit;
        if (!string.IsNullOrEmpty(request.RoleName))
        {
            if (AccessGuard.Missing(actor, PermissionKeys.CanAssignUserRole, "change a user's role") is { } cannotAssign) return cannotAssign;
            if (AccessGuard.Self(userId, id, "change the role") is { } self) return self;
            var roleIds = await context.Roles.Where(item => item.Name == request.RoleName).Select(item => item.Id).ToListAsync();
            if (roleIds.Count == 0) return Error.Validation("Role.Name", $"Could not find role {request.RoleName}");
            if (await AccessGuard.RoleAssignment(context, actor, roleIds) is { } escalation) return escalation;
        }
        var user = await context.Users.FirstOrDefaultAsync(item => item.Id == id);
        if (user != null)
        {
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.DateOfBirth = request.DateOfBirth;
            user.PhoneNumber = request.PhoneNumber;
            user.DepartmentId = request.DepartmentId;
            user.LastUpdatedById = userId;
            user.UpdatedAt = DateTime.UtcNow;

            context.Users.Update(user);
            await context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(request.Avatar))
            {
                var image = request.Avatar.ConvertFromBase64();
                var reference = $"{user.Id}.{image.FileName.Split(".").Last()}";
                await blobStorage.UploadBlobAsync("avatar", image, reference, user.Avatar);

                user.Avatar = reference;
                context.Users.Update(user);
                await context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(request.RoleName))
            {
                var existingRoles = await userManager.GetRolesAsync(user);
                await userManager.RemoveFromRolesAsync(user, existingRoles);
                await userManager.AddToRoleAsync(user, request.RoleName);
                await context.SaveChangesAsync();
            }
        }
        return Result.Success();
    }

    public async Task<Result> UpdateRolesOfUser(UpdateUserRoleRequest request, Guid id, Guid actorId)
    {
        if (request.RoleNames is not { Count: 1 })
            return Error.Validation("RoleNames.Count", "Only one role per user");
        if (AccessGuard.Self(actorId, id, "change the role") is { } self) return self;

        var role = await context.Roles.FirstOrDefaultAsync(item => item.Name == request.RoleNames[0]);
        if (role == null) return Error.Validation("Role.Name",
            $"Could not find role for {request.RoleNames[0]}");
        // Checked again here, immediately before the change, from current data.
        var actor = await CurrentPermissionSet.Load(context, actorId);
        if (AccessGuard.Missing(actor, PermissionKeys.CanAssignUserRole, "change a user's role") is { } cannotAssign) return cannotAssign;
        if (await AccessGuard.RoleAssignment(context, actor, [role.Id]) is { } escalation) return escalation;

        var user = await context.Users.FirstOrDefaultAsync(item => item.Id == id);
        if (user == null)
            return Error.NotFound("User", $"Could not find user {id}");
        
        var existingRoles = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, existingRoles);
        await userManager.AddToRoleAsync(user, role.Name ?? "");
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteUser(Guid id, Guid userId)
    {
        if (AccessGuard.Self(userId, id, "delete the account") is { } self) return self;
        var actor = await CurrentPermissionSet.Load(context, userId);
        if (AccessGuard.Missing(actor, PermissionKeys.CanDeleteUser, "delete users") is { } cannotDelete) return cannotDelete;
        // The account deleted is the one named in the request, never the caller's.
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return UserErrors.NotFound(id);

        user.DeletedAt = DateTime.UtcNow;
        user.LastDeletedById = userId;
        context.Users.Update(user);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> ToggleDisableUser(Guid id, Guid userId)
    {
        if (AccessGuard.Self(userId, id, "block or unblock") is { } self) return self;
        var user = await context.Users.FirstOrDefaultAsync(item => item.Id == id);
        if (user == null) return UserErrors.NotFound(id);
        // Blocking and unblocking are separate tasks with their own permissions.
        var actor = await CurrentPermissionSet.Load(context, userId);
        var refused = user.IsDisabled
            ? AccessGuard.Missing(actor, PermissionKeys.CanUnblockUser, "unblock users")
            : AccessGuard.Missing(actor, PermissionKeys.CanBlockUser, "block users");
        if (refused is not null) return refused;

        user.IsDisabled = !user.IsDisabled;
        if (!user.IsDisabled)
        {
            user.DeletedAt = null;
            user.LastDeletedById = null;
        }

        user.LastDeletedById = userId;
        context.Users.Update(user);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UploadAvatar(UploadFileRequest request, Guid userId)
    {
        var avatar = request.File.ConvertFromBase64();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return UserErrors.NotFound(userId);
        var reference = $"{userId}.{avatar.FileName.Split(".").Last()}";

        var result = await blobStorage.UploadBlobAsync("avatar", avatar, reference, user.Avatar);
        if (result.IsSuccess)
        {
            user.Avatar = reference;
            context.Users.Update(user);
            await context.SaveChangesAsync();
        }

        return result;
    }

    public async Task<Result> UploadSignature(UploadFileRequest request, Guid userId)
    {
        var signature = request.File.ConvertFromBase64();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return UserErrors.NotFound(userId);
        var reference = $"{userId}.{signature.FileName.Split(".").Last()}";

        var result = await blobStorage.UploadBlobAsync("signature", signature, reference, user.Signature);
        if (result.IsSuccess)
        {
            user.Signature = reference;
            context.Users.Update(user);
            await context.SaveChangesAsync();
        }

        return result;
    }

    public async Task<Result> SwitchDepartment(Guid? departmentId, Guid userId, string token)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return UserErrors.NotFound(userId);

        if (departmentId.HasValue)
        {
            var departmentExists = await context.Departments.AnyAsync(d => d.Id == departmentId.Value);
            if (!departmentExists) return Error.NotFound("Department.NotFound", "Department not found");
        }

        user.DepartmentId = departmentId;
        context.Users.Update(user);
        await context.SaveChangesAsync();

        // Invalidate the cache for this token to force reload in JwtMiddleware
        cache.Remove(token);

        return Result.Success();
    }

    private async Task<bool> EmailIsUnique(string email)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        return user == null;
    }

    private async Task<bool> ValidRoleName(string roleName)
    {
        var role = await context.Roles.FirstOrDefaultAsync(r => r.Name.Trim() == roleName.Trim());
        return role != null;
    }
}

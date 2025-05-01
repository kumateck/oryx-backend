using APP.IRepository;
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
using SHARED;
using SHARED.Requests;

namespace APP.Repository;

public class UserRepository(ApplicationDbContext context, UserManager<User> userManager, IJwtService jwtService, IBlobStorageService blobStorage, IMapper mapper)
    : IUserRepository
{
    public async Task<Result<Guid>> CreateUser(CreateUserRequest request)
    {
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
            .Where(item => request.RoleNames.Contains(item.Name))
            .Select(item => item.Name)
            .ToList();
            
        await userManager.AddToRolesAsync(user, roleName);
        await context.SaveChangesAsync();
        
        if (string.IsNullOrEmpty(request.Avatar))  return user.Id;
        
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
            
        var roleName = context.Roles
            .Where(item => item.Name == RoleUtils.AppRoleSuper)
            .Select(item => item.Name)
            .ToList();

        await userManager.AddToRolesAsync(user, roleName);

        await context.SaveChangesAsync();

        return await jwtService.AuthenticateNewUser(user);
    }

    public async Task<Result<Paginateable<IEnumerable<UserWithRoleDto>>>> GetUsers(int page, int pageSize, string searchQuery)
    {
        var query = context.Users.AsQueryable();
        
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.FirstName, q => q.LastName, q => q.Email);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query, 
            page, 
            pageSize, 
            mapper.Map<UserWithRoleDto>
        );
    }
    
    public async Task<Result<UserWithRoleDto>> GetUser(Guid userId)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return UserErrors.NotFound(userId);
        return mapper.Map<UserWithRoleDto>(user);
    }

    public async Task<Result> UpdateUser(UpdateUserRequest request, Guid id, Guid userId)
    {
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

            //var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

            //var result = await userManager.ConfirmEmailAsync(user, token);

            // if (result.Succeeded)
            // {
            //     user.UserName = request.Email;
            //     user.Email = request.Email;
            //     context.Users.Update(user);
            //     await context.SaveChangesAsync();
            //     await userManager.UpdateNormalizedEmailAsync(user);
            //     await userManager.UpdateNormalizedUserNameAsync(user);
            // }
        }

        return Result.Success();
    }
    
    public async Task<Result> UpdateRolesOfUser(UpdateUserRoleRequest request, Guid id, Guid userId)
    {
        foreach (var roleName in request.RoleNames)
        {
            if (!await ValidRoleName(roleName))
            {
                return UserErrors.InvalidRoleName(roleName);
            }
        }
        
        var user = await context.Users.FirstOrDefaultAsync(item => item.Id == id);
        var existingRoles =  await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, existingRoles);
        await userManager.AddToRolesAsync(user, request.RoleNames);
        user.LastDeletedById = userId;
        context.Users.Update(user);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteUser(Guid id, Guid userId)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return UserErrors.NotFound(userId);

        user.DeletedAt = DateTime.UtcNow;
        user.LastDeletedById = userId;
        context.Users.Update(user);
        await context.SaveChangesAsync();
        
        return Result.Success();
    }
    
    public async Task<Result> ToggleDisableUser(Guid id, Guid userId)
    {
        var user = await context.Users.FirstOrDefaultAsync(item => item.Id == id);
        if (user == null) return UserErrors.NotFound(userId);
        
        user.IsDisabled = !user.IsDisabled;
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
    
    private async Task<bool> EmailIsUnique(string email)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        return user == null;
    }

    private async Task<bool> ValidRoleName(string roleName)
    {
        var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        return role != null;
    }
}
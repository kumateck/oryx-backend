using AutoMapper;
using DOMAIN.Context;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;

namespace APP.Mapper.Resolvers;

public class UserRoleResolver(OryxContext context, IMapper mapper)
    : IValueResolver<User, UserDto, ICollection<RoleDto>>
{
    public ICollection<RoleDto> Resolve(User source, UserDto destination, ICollection<RoleDto> destMember,
        ResolutionContext context1)
    {
        var roleIds = context
            .UserRoles
            .Where(item => item.UserId == source.Id)
            .Select(user => user.RoleId)
            .ToList();
        return context.Roles.Where(role => roleIds.Contains(role.Id)).Select(role => mapper.Map<RoleDto>(role))
            .ToList();
    }
}
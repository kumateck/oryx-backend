using AutoMapper;
using DOMAIN.Entities.Users;
using Microsoft.AspNetCore.Http;

namespace APP.Mapper.Resolvers;

public class AvatarResolver(IHttpContextAccessor request) : IValueResolver<User, UserDto, string>
{
    public string Resolve(User source, UserDto destination, string destMember, ResolutionContext context)
    {
        return string.IsNullOrEmpty(source.Avatar) ? null : $"https://{request.HttpContext?.Request.Host}/api/v1/file/avatar/{source.Avatar}";
    }
}

public class SignatureResolver(IHttpContextAccessor request) : IValueResolver<User, UserDto, string>
{
    public string Resolve(User source, UserDto destination, string destMember, ResolutionContext context)
    {
        return string.IsNullOrEmpty(source.Avatar) ? null : $"https://{request.HttpContext?.Request.Host}/api/v1/file/signature/{source.Signature}";
    }
}
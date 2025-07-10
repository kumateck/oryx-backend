using AutoMapper;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.Users;
using Microsoft.AspNetCore.Http;

namespace APP.Mapper.Resolvers;

public class AvatarResolver(IHttpContextAccessor request) : IValueResolver<User, UserDto, string>
{
    public string Resolve(User source, UserDto destination, string destMember, ResolutionContext context)
    {
        return string.IsNullOrEmpty(source.Avatar) ? null : $"http://{request.HttpContext?.Request.Host}/api/v1/file/avatar/{source.Avatar}";
    }
}

public class EmployeeAvatarResolver(IHttpContextAccessor request) : IValueResolver<Employee, EmployeeDto, string>
{
    public string Resolve(Employee source, EmployeeDto destination, string destMember, ResolutionContext context)
    {
        return string.IsNullOrEmpty(source.Avatar) ? null : $"http://{request.HttpContext?.Request.Host}/api/v1/file/avatar/{source.Avatar}";
    }
}

public class SignatureResolver(IHttpContextAccessor request) : IValueResolver<User, UserDto, string>
{
    public string Resolve(User source, UserDto destination, string destMember, ResolutionContext context)
    {
        return string.IsNullOrEmpty(source.Signature) ? null : $"http://{request.HttpContext?.Request.Host}/api/v1/file/signature/{source.Signature}";
    }
}

/*public class QCSignatureResolver(IHttpContextAccessor request) : IValueResolver<AnalyticalTestRequest, AnalyticalTestRequestDto, string>
{
    public string Resolve(AnalyticalTestRequest source, AnalyticalTestRequestDto destination, string destMember,
        ResolutionContext context)
    {
        return string.IsNullOrWhiteSpace(source.QcManagerSignature)
            ? null
            : $"https://{request.HttpContext?.Request.Host}/api/v1/file/signature/{source.QcManagerSignature}";
    }
}

public class QASignatureResolver(IHttpContextAccessor request) : IValueResolver<AnalyticalTestRequest, AnalyticalTestRequestDto, string>
{
    public string Resolve(AnalyticalTestRequest source, AnalyticalTestRequestDto destination, string destMember,
        ResolutionContext context)
    {
        return string.IsNullOrWhiteSpace(source.QaManagerSignature)
            ? null
            : $"https://{request.HttpContext?.Request.Host}/api/v1/file/signature/{source.QaManagerSignature}";
    }
}*/
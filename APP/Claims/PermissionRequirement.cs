using Microsoft.AspNetCore.Authorization;

namespace APP.Claims;


internal class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; private set; } = permission;
}
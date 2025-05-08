using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace APP.Claims;

internal class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; } = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => FallbackPolicyProvider.GetDefaultPolicyAsync();
    
    public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
    {
        //if (!policyName.StartsWith("permission", StringComparison.OrdinalIgnoreCase))
        //return FallbackPolicyProvider.GetPolicyAsync(policyName);
        var policy = new AuthorizationPolicyBuilder();
        policy.AddRequirements(new PermissionRequirement(policyName));
        return Task.FromResult(policy.Build());
    }
    public Task<AuthorizationPolicy> GetFallbackPolicyAsync() => FallbackPolicyProvider.GetDefaultPolicyAsync();
}
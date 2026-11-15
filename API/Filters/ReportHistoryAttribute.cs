namespace API.Filters;
[AttributeUsage(AttributeTargets.Method)]
public sealed class ReportHistoryAttribute(params string[] permissions) : Attribute
{
    public bool RequireForCurrent { get; set; }
    /// <summary>A lookup every hub report filters by: any one report's key also opens its history.</summary>
    public bool OrAnyHubReport { get; init; }
    public string[] Permissions => OrAnyHubReport ? [.. permissions.Concat(APP.Utils.ReportPermissions.Hub).Distinct()] : permissions;
}

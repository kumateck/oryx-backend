namespace API.Filters;
[AttributeUsage(AttributeTargets.Method)]
public sealed class ReportHistoryAttribute(params string[] permissions) : Attribute
{
    public bool RequireForCurrent { get; set; }
    public string[] Permissions { get; } = permissions;
}

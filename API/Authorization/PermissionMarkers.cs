namespace API.Authorization;

/// <summary>
/// The action checks its keys itself because they depend on the record (its store, kind, owner or approval stage). The
/// reason says what it checks, so the permission catalog can list the action as covered without hiding a gap.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PermissionCheckedInActionAttribute(string reason) : Attribute
{
    public string Reason { get; } = reason;
}

/// <summary>
/// Any signed-in user may call this, because it acts only on the caller's own data (their password, avatar or
/// notifications). The reason says why.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SignedInUserAttribute(string reason) : Attribute
{
    public string Reason { get; } = reason;
}

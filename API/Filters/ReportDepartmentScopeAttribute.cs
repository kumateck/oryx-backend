namespace API.Filters;

/// <summary>
/// Marks a report endpoint whose department inputs (a <c>departmentId</c> or <c>departmentName</c>
/// parameter, or a <c>DepartmentId</c> on its query filter) follow <see cref="APP.Services.Authorization.ReportDepartmentScope"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ReportDepartmentScopeAttribute : Attribute;

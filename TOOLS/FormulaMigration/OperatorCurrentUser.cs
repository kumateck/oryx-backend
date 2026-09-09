using SHARED.Services.Identity;

namespace FormulaMigration;

internal sealed class OperatorCurrentUser(Guid? userId) : ICurrentUserService
{
    public Guid? UserId => userId;
    public Guid? DepartmentId => null;
    public string DepartmentType => "FormulaMigrationOperator";
}

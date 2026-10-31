using System.Text.Json.Serialization;

namespace DOMAIN.Entities.Users.Request;

/// <summary>Sets the signed-in user's department; an explicit null removes the assignment.</summary>
public sealed class SwitchDepartmentRequest
{
    [JsonRequired]
    public Guid? DepartmentId { get; set; }
}

namespace APP.Services.Forms;

/// <summary>A single existing field assignment to transfer, with its reviewed owner for stale-state protection.</summary>
public sealed record TestFieldReassignmentInput(Guid AssignmentId, Guid ExpectedAssigneeId, Guid NewAssigneeId);
public sealed record ReassignTestFieldsRequest(List<TestFieldReassignmentInput> Fields, string Reason);
public sealed record ReassignTestFieldsResultDto(int FieldsChanged, bool AlreadyApplied);

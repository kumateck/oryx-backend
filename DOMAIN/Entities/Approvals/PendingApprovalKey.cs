namespace DOMAIN.Entities.Approvals;

/// <summary>A document identity for counting and merging approval queues without loading details.</summary>
public sealed record PendingApprovalKey(string ModelType, Guid Id);

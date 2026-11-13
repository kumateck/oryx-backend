namespace DOMAIN.Entities.Specifications;

public sealed record SpecificationPrintHistory(
    string RevisedBy,
    DateTime? RevisedAt,
    DateTime? HistoryAvailableFrom,
    IReadOnlyList<SpecificationPrintRevision> Revisions);

public sealed record SpecificationPrintRevision(
    string SpecificationNumber,
    string RevisionNumber,
    string SupersedesNumber,
    string RecordedBy,
    DateTime? OccurredAt,
    string Event);

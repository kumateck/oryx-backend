using System.ComponentModel.DataAnnotations;
namespace APP.Services.Forms;
public sealed record ReturnTestForCorrectionRequest([property: Required, MaxLength(2000)] string Reason);
public sealed record TestCorrectionHistoryItemDto(Guid Id, Guid SourceResponseId, Guid CorrectionResponseId,
    Guid ReturnedById, string ReturnedBy, DateTime ReturnedAt, string Reason);
public sealed record TestCorrectionStateDto(Guid ResponseId, bool CanReturn, string BlockedReason,
    List<TestCorrectionHistoryItemDto> History);

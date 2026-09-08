using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Formulas;

public class LegacyFormulaArtifact
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public Question Question { get; set; }
    public Guid? QuestionOptionId { get; set; }
    public QuestionOption QuestionOption { get; set; }
    public string OriginalPayload { get; set; }
    public string SourceHash { get; set; }
    public string SourcePath { get; set; }
    public bool SourceWasDeleted { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime? SourceDeletedAt { get; set; }
    public string DatabaseFingerprint { get; set; }
    public string MigrationReleaseId { get; set; }
    public DateTime CapturedAt { get; set; }
}

public class LegacyKeyMapping
{
    public Guid Id { get; set; }
    public Guid LegacyFormulaArtifactId { get; set; }
    public LegacyFormulaArtifact LegacyFormulaArtifact { get; set; }
    public string KeyKind { get; set; }
    public string LegacyPath { get; set; }
    public string LegacyKey { get; set; }
    public string CanonicalKey { get; set; }
    public string NormalizationReason { get; set; }
    public Guid ReviewedById { get; set; }
    public User ReviewedBy { get; set; }
    public string ApprovalReference { get; set; }
    public DateTime ReviewedAt { get; set; }
}

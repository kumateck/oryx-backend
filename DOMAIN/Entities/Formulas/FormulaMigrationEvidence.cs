using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Formulas;

public class FormulaMigrationRun
{
    public Guid Id { get; set; }
    public string ReleaseId { get; set; }
    public string SourceFingerprint { get; set; }
    public string CorpusChecksum { get; set; }
    public string CodeVersion { get; set; }
    public string ApplyManifestHash { get; set; }
    public string SignedReportHash { get; set; }
    public FormulaMigrationRunMode Mode { get; set; }
    public FormulaMigrationRunStatus Status { get; set; }
    public Guid InitiatedById { get; set; }
    public User InitiatedBy { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalCount { get; set; }
    public int SucceededCount { get; set; }
    public int FailedCount { get; set; }
    public string SignedReportLocation { get; set; }
    public List<FormulaMigrationItem> Items { get; set; } = [];
}

public class FormulaMigrationItem
{
    public Guid Id { get; set; }
    public Guid FormulaMigrationRunId { get; set; }
    public FormulaMigrationRun FormulaMigrationRun { get; set; }
    public Guid LegacyFormulaArtifactId { get; set; }
    public LegacyFormulaArtifact LegacyFormulaArtifact { get; set; }
    public string SourceHash { get; set; }
    public string PlacementKey { get; set; }
    public Guid? TargetId { get; set; }
    public FormulaMigrationClass? MigrationClass { get; set; }
    public string Action { get; set; }
    public string ApprovalReference { get; set; }
    public FormulaMigrationApprovalScope? ApprovalScope { get; set; }
    public FormulaMigrationItemStatus Status { get; set; }
    public string Error { get; set; }
    public string BeforeHash { get; set; }
    public string AfterHash { get; set; }
}

public class FormulaReconciliationResult
{
    public Guid Id { get; set; }
    public Guid FormulaMigrationRunId { get; set; }
    public FormulaMigrationRun FormulaMigrationRun { get; set; }
    public string ControlName { get; set; }
    public string ExpectedValue { get; set; }
    public string ActualValue { get; set; }
    public bool Passed { get; set; }
    public string EvidenceJson { get; set; }
    public DateTime CheckedAt { get; set; }
}

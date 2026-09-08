using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Formulas;

public class ResponseFormulaSnapshot
{
    public Guid Id { get; set; }
    public Guid ResponseId { get; set; }
    public Response Response { get; set; }
    public string PlacementKey { get; set; }
    public int Sequence { get; set; }
    public Guid FormulaRevisionId { get; set; }
    public FormulaRevision FormulaRevision { get; set; }
    public string DefinitionHash { get; set; }
    public string ConfigurationHash { get; set; }
    public string ExecutableDefinitionJson { get; set; }
    public string BindingsJson { get; set; }
    public string ResultTargetsJson { get; set; }
    public string TableShapeJson { get; set; }
    public string CalculationPolicyJson { get; set; }
    public string DisplayPolicyJson { get; set; }
    public string MethodReference { get; set; }
    public Guid? SupersedesSnapshotId { get; set; }
    public ResponseFormulaSnapshot SupersedesSnapshot { get; set; }
    public FormulaSnapshotReason Reason { get; set; }
    public string ApprovalReference { get; set; }
    public Guid CapturedById { get; set; }
    public User CapturedBy { get; set; }
    public DateTime CapturedAt { get; set; }
}

public class FormulaExecution
{
    public Guid Id { get; set; }
    public Guid ResponseFormulaSnapshotId { get; set; }
    public ResponseFormulaSnapshot ResponseFormulaSnapshot { get; set; }
    public Guid? SupersedesExecutionId { get; set; }
    public FormulaExecution SupersedesExecution { get; set; }
    public FormulaExecutionTrigger Trigger { get; set; }
    public FormulaExecutionAuthority Authority { get; set; }
    public FormulaExecutionStatus Status { get; set; }
    public string EngineVersion { get; set; }
    public string EngineBuildHash { get; set; }
    public string FormulaLanguageVersion { get; set; }
    public string NumericPolicyVersion { get; set; }
    public string InputHash { get; set; }
    public string ResultHash { get; set; }
    public string ResolvedInputsJson { get; set; }
    public string RawResultsJson { get; set; }
    public string RoundedResultsJson { get; set; }
    public string DisplayResultsJson { get; set; }
    public string CalculationTraceJson { get; set; }
    public string IdempotencyKey { get; set; }
    public Guid ActorId { get; set; }
    public User Actor { get; set; }
    public DateTime ExecutedAt { get; set; }
}

public class ResponseFormulaSubmissionSet
{
    public Guid Id { get; set; }
    public Guid ResponseId { get; set; }
    public Response Response { get; set; }
    public int Sequence { get; set; }
    public string SetHash { get; set; }
    public string InputAggregateHash { get; set; }
    public string ConfigurationAggregateHash { get; set; }
    public Guid SubmittedById { get; set; }
    public User SubmittedBy { get; set; }
    public DateTime SubmittedAt { get; set; }
    public List<ResponseFormulaSubmissionExecution> Executions { get; set; } = [];
}

public class ResponseFormulaSubmissionExecution
{
    public Guid Id { get; set; }
    public Guid ResponseFormulaSubmissionSetId { get; set; }
    public ResponseFormulaSubmissionSet ResponseFormulaSubmissionSet { get; set; }
    public Guid FormulaExecutionId { get; set; }
    public FormulaExecution FormulaExecution { get; set; }
    public string PlacementKey { get; set; }
    public int Ordinal { get; set; }
}

namespace DOMAIN.Entities.Formulas;

public sealed class QuestionFormulaRepair
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public Guid ActorId { get; set; }
    public bool UpdatedQuestion { get; set; }
    public string Reason { get; set; } = "";
    public string PreviewToken { get; set; } = "";
    public string RequestHash { get; set; } = "";
    public string DefinitionBefore { get; set; } = "";
    public string DefinitionAfter { get; set; } = "";
    public string EvidenceJson { get; set; } = "{}";
    public DateTime AppliedAt { get; set; }
}

/// <summary>Repair-time capture of a legacy definition, never an approved formula revision.</summary>
public sealed class LegacyFormulaConfigurationPin
{
    public Guid Id { get; set; }
    public Guid ResponseId { get; set; }
    public Guid FormFieldId { get; set; }
    public int Sequence { get; set; }
    public Guid RepairId { get; set; }
    public string Definition { get; set; } = "";
    public string DefinitionHash { get; set; } = "";
    public bool RequiresRecalculation { get; set; }
    public DateTime CapturedAt { get; set; }
}

public sealed class LegacyFormulaRepairEvaluation
{
    public Guid Id { get; set; }
    public Guid PinId { get; set; }
    public Guid ActorId { get; set; }
    public string InputHash { get; set; } = "";
    public string ResultsJson { get; set; } = "[]";
    public string EvidenceJson { get; set; } = "{}";
    public string EngineBuildHash { get; set; } = "";
    public DateTime EvaluatedAt { get; set; }
}

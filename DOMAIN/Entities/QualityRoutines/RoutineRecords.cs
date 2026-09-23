using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.RndTrialBatches;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.QualityRoutines;

public class RoutineArd : BaseEntity, IVerifiable
{
    public RoutineType Type { get; set; }
    public AnalysisType AnalysisType { get; set; }
    [StringLength(100)] public string SpecNumber { get; set; }
    [StringLength(4000)] public string Description { get; set; }
    public Guid FormId { get; set; }
    public Form Form { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedById { get; set; }
    public List<RoutineCoaItem> CoaItems { get; set; } = [];
}

public class RoutineCoaItem : BaseEntity
{
    public Guid RoutineArdId { get; set; }
    public RoutineArd RoutineArd { get; set; }
    public Guid FormFieldId { get; set; }
    public FormField FormField { get; set; }
    public bool IncludeOnCoa { get; set; }
    [StringLength(255)] public string DisplayLabel { get; set; }
    [StringLength(255)] public string GroupName { get; set; }
    [StringLength(2000)] public string SpecificationText { get; set; }
    [StringLength(100)] public string Unit { get; set; }
    [StringLength(255)] public string Reference { get; set; }
    public int DisplayOrder { get; set; }
}

public class RoutineDefinition : BaseEntity
{
    [StringLength(255)] public string Name { get; set; }
    public RoutineType Type { get; set; }
    public RoutineCadence Cadence { get; set; }
    public bool IsActive { get; set; }
    public List<RoutineExecution> Executions { get; set; } = [];
}

public class RoutineExecution : BaseEntity
{
    [StringLength(100)] public string RoutineCode { get; set; }
    public RoutineType Type { get; set; }
    public RoutineOrigin Origin { get; set; }
    public RoutineCadence? Cadence { get; set; }
    public Guid? RoutineDefinitionId { get; set; }
    public RoutineDefinition RoutineDefinition { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public DateTime RoutineDate { get; set; }
    [StringLength(4000)] public string EmergencyTrigger { get; set; }
    [StringLength(4000)] public string EmergencyReason { get; set; }
    public Guid? RndTrialBatchId { get; set; }
    public RndTrialBatch RndTrialBatch { get; set; }
    public Guid? DoneById { get; set; }
    public User DoneBy { get; set; }
    public DateTime? DoneAt { get; set; }
    public RoutineStatus Status { get; set; }
    public List<RoutineSample> Samples { get; set; } = [];
    public List<RoutineAuditEvent> AuditEvents { get; set; } = [];
}

public class RoutineSample : BaseEntity
{
    public Guid RoutineExecutionId { get; set; }
    public RoutineExecution RoutineExecution { get; set; }
    [StringLength(255)] public string SamplingPoint { get; set; }
    [StringLength(255)] public string AreaName { get; set; }
    public DateTime CollectedAt { get; set; }
    public List<RoutineTrack> Tracks { get; set; } = [];
}

public class RoutineTrack : BaseEntity
{
    public Guid RoutineSampleId { get; set; }
    public RoutineSample RoutineSample { get; set; }
    public AnalysisType AnalysisType { get; set; }
    public Guid RoutineArdId { get; set; }
    public RoutineArd RoutineArd { get; set; }
    public Guid FormId { get; set; }
    public Guid? FormRevisionId { get; set; }
    public DateTime? WorksheetFinalizedAt { get; set; }
    public Guid? WorksheetFinalizedById { get; set; }
    public Response Response { get; set; }
    public string CoaItemsSnapshotJson { get; set; }
}

public class RoutineAuditEvent : BaseEntity
{
    public Guid RoutineExecutionId { get; set; }
    public RoutineExecution RoutineExecution { get; set; }
    [StringLength(100)] public string Action { get; set; }
    [StringLength(4000)] public string Detail { get; set; }
    public DateTime OccurredAt { get; set; }
    public Guid ActorId { get; set; }
}

public class RoutineCertificate : BaseEntity
{
    public Guid RoutineExecutionId { get; set; }
    public RoutineExecution RoutineExecution { get; set; }
    public Guid? RoutineSampleId { get; set; }
    public RoutineSample RoutineSample { get; set; }
    [StringLength(100)] public string CertificateCode { get; set; }
    public bool Combined { get; set; }
    public string RowsJson { get; set; }
    public DateTime IssuedAt { get; set; }
    public Guid IssuedById { get; set; }
}

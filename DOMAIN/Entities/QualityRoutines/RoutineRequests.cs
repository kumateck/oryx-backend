using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.QualityRoutines;

public class CreateRoutineArdRequest
{
    public RoutineType Type { get; set; }
    public AnalysisType AnalysisType { get; set; }
    [Required] public Guid FormId { get; set; }
    [StringLength(100)] public string SpecNumber { get; set; }
    [StringLength(4000)] public string Description { get; set; }
    public List<RoutineCoaItemRequest> CoaItems { get; set; } = [];
}

public class RoutineCoaItemRequest
{
    public Guid FormFieldId { get; set; }
    public bool IncludeOnCoa { get; set; }
    [StringLength(255)] public string DisplayLabel { get; set; }
    [StringLength(255)] public string GroupName { get; set; }
    [StringLength(2000)] public string SpecificationText { get; set; }
    [StringLength(100)] public string Unit { get; set; }
    [StringLength(255)] public string Reference { get; set; }
    public int DisplayOrder { get; set; }
}

public class CreateRoutineDefinitionRequest
{
    [Required, StringLength(255)] public string Name { get; set; }
    public RoutineType Type { get; set; }
    public RoutineCadence Cadence { get; set; }
}

public class CreateRoutineExecutionRequest
{
    public RoutineOrigin Origin { get; set; }
    public RoutineType Type { get; set; }
    public Guid? RoutineDefinitionId { get; set; }
    public DateTime RoutineDate { get; set; }
    public Guid? DoneById { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public string EmergencyTrigger { get; set; }
    public string EmergencyReason { get; set; }
    public Guid? RndTrialBatchId { get; set; }
}

public class CreateRoutineSampleRequest
{
    [StringLength(255)] public string SamplingPoint { get; set; }
    [StringLength(255)] public string AreaName { get; set; }
    public DateTime CollectedAt { get; set; }
}

public class RoutineExecutionDto
{
    public Guid Id { get; set; }
    public string RoutineCode { get; set; }
    public RoutineOrigin Origin { get; set; }
    public RoutineType Type { get; set; }
    public RoutineCadence? Cadence { get; set; }
    public DateTime RoutineDate { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public string EmergencyTrigger { get; set; }
    public string EmergencyReason { get; set; }
    public Guid? RndTrialBatchId { get; set; }
    public Guid? DoneById { get; set; }
    public DateTime? DoneAt { get; set; }
    public RoutineStatus Status { get; set; }
    public List<RoutineSampleDto> Samples { get; set; } = [];
}

public class RoutineSampleDto
{
    public Guid Id { get; set; }
    public string SamplingPoint { get; set; }
    public string AreaName { get; set; }
    public DateTime CollectedAt { get; set; }
    public List<RoutineTrackDto> Tracks { get; set; } = [];
}

public class RoutineTrackDto
{
    public Guid Id { get; set; }
    public AnalysisType AnalysisType { get; set; }
    public Guid RoutineArdId { get; set; }
    public Guid FormId { get; set; }
    public Guid? ResponseId { get; set; }
    public bool Approved { get; set; }
    public bool Rejected { get; set; }
}


public class RoutineArdDto
{
    public Guid Id { get; set; }
    public RoutineType Type { get; set; }
    public AnalysisType AnalysisType { get; set; }
    public Guid FormId { get; set; }
    public string SpecNumber { get; set; }
    public bool IsVerified { get; set; }
    public List<RoutineCoaItemRequest> CoaItems { get; set; } = [];
}

public class RoutineDefinitionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public RoutineType Type { get; set; }
    public RoutineCadence Cadence { get; set; }
    public bool IsActive { get; set; }
}

public class RoutineCertificateDto
{
    public Guid Id { get; set; }
    public Guid RoutineExecutionId { get; set; }
    public Guid? RoutineSampleId { get; set; }
    public string CertificateCode { get; set; }
    public bool Combined { get; set; }
    public string RowsJson { get; set; }
    public DateTime IssuedAt { get; set; }
}


public class RoutineTrackContextDto
{
    public Guid Id { get; set; }
    public Guid RoutineExecutionId { get; set; }
    public string RoutineCode { get; set; }
    public Guid RoutineSampleId { get; set; }
    public string SampleIdentity { get; set; }
    public AnalysisType AnalysisType { get; set; }
    public Guid FormId { get; set; }
    public Guid RoutineArdId { get; set; }
    public Guid? ResponseId { get; set; }
    public bool Approved { get; set; }
    public bool Rejected { get; set; }
}

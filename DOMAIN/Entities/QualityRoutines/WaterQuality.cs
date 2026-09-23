using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.RndTrialBatches;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.QualityRoutines;

public enum WaterQualityPeriodStatus
{
    Active = 0,
    Held = 1,
}

public enum WaterUseStatus
{
    Recorded = 0,
    Held = 1,
}

public class WaterQualityPeriod : BaseEntity
{
    public Guid RoutineCertificateId { get; set; }
    public RoutineCertificate RoutineCertificate { get; set; }
    public Guid RoutineSampleId { get; set; }
    public RoutineSample RoutineSample { get; set; }
    [StringLength(255)] public string SamplingPoint { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidUntil { get; set; }
    public WaterQualityPeriodStatus Status { get; set; }
    [StringLength(4000)] public string RetrospectiveReason { get; set; }
    public Guid ApprovedById { get; set; }
    public User ApprovedBy { get; set; }
    public DateTime ApprovedAt { get; set; }
    [StringLength(4000)] public string HoldReason { get; set; }
    public Guid? HeldById { get; set; }
    public User HeldBy { get; set; }
    public DateTime? HeldAt { get; set; }
    public List<WaterUseRecord> UseRecords { get; set; } = [];
}

public class WaterUseRecord : BaseEntity
{
    public Guid WaterQualityPeriodId { get; set; }
    public WaterQualityPeriod WaterQualityPeriod { get; set; }
    public DateTime UsedAt { get; set; }
    [StringLength(255)] public string SamplingPoint { get; set; }
    public Guid? BatchManufacturingRecordId { get; set; }
    public BatchManufacturingRecord BatchManufacturingRecord { get; set; }
    public Guid? ProductionActivityStepId { get; set; }
    public ProductionActivityStep ProductionActivityStep { get; set; }
    public Guid? RndTrialBatchId { get; set; }
    public RndTrialBatch RndTrialBatch { get; set; }
    public WaterUseStatus Status { get; set; }
}

public class ActivateWaterQualityPeriodRequest
{
    public Guid RoutineCertificateId { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidUntil { get; set; }
    [StringLength(4000)] public string RetrospectiveReason { get; set; }
}

public class RecordWaterUseRequest
{
    public Guid WaterQualityPeriodId { get; set; }
    public DateTime UsedAt { get; set; }
    public Guid? BatchManufacturingRecordId { get; set; }
    public Guid? ProductionActivityStepId { get; set; }
    public Guid? RndTrialBatchId { get; set; }
}

public class HoldWaterQualityPeriodRequest
{
    [Required, StringLength(4000)] public string Reason { get; set; }
}

public class EligibleWaterCertificateDto
{
    public Guid Id { get; set; }
    public string CertificateCode { get; set; }
    public string SamplingPoint { get; set; }
    public DateTime IssuedAt { get; set; }
}

public class WaterQualityPeriodDto
{
    public Guid Id { get; set; }
    public Guid RoutineCertificateId { get; set; }
    public Guid RoutineSampleId { get; set; }
    public string SamplingPoint { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidUntil { get; set; }
    public WaterQualityPeriodStatus Status { get; set; }
    public string RetrospectiveReason { get; set; }
    public DateTime ApprovedAt { get; set; }
    public string HoldReason { get; set; }
    public DateTime? HeldAt { get; set; }
    public int UseRecordCount { get; set; }
}

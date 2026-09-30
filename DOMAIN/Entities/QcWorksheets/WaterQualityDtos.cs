using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.QcWorksheets;

public class WaterQualityPeriodSummaryDto : BaseDto
{
    public Guid SamplingPointId { get; set; }
    public string SamplingPointCode { get; set; }
    public string SamplingPointName { get; set; }

    public Guid TestRequestSubjectId { get; set; }

    /// <summary>The round the backing Subject belongs to, so a window can be traced to its ARD.</summary>
    public Guid TestRequestId { get; set; }

    public string TestRequestArNumber { get; set; }

    public DateTime ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public WaterQualityPeriodStatus Status { get; set; }

    public string RetrospectiveReason { get; set; }
    public UserDto ActivatedBy { get; set; }
    public DateTime? ActivatedAt { get; set; }

    public string HoldReason { get; set; }
    public UserDto HeldBy { get; set; }
    public DateTime? HeldAt { get; set; }

    /// <summary>How many uses are booked against this window, and how many of them are flagged.</summary>
    public int UseRecordCount { get; set; }

    public int HeldUseRecordCount { get; set; }
}

/// <summary>The window with its use log — what the period drawer renders.</summary>
public class WaterQualityPeriodDetailDto : WaterQualityPeriodSummaryDto
{
    public List<WaterUseRecordDto> UseRecords { get; set; } = [];
}

public class WaterUseRecordDto : BaseDto
{
    public Guid WaterQualityPeriodId { get; set; }
    public DateTime UsedAt { get; set; }
    public Guid? BatchManufacturingRecordId { get; set; }
    public string BatchNumber { get; set; }
    public Guid? ProductionActivityStepId { get; set; }
    public UserDto RecordedBy { get; set; }
    public WaterUseRecordStatus Status { get; set; }
}

/// <summary>
/// The reason is required by the endpoint, not merely by the column. Activating a window tells
/// production it may rely on water for a stretch of time that has already elapsed; that claim
/// has to come with a justification from the person making it.
/// </summary>
public class ActivateWaterQualityPeriodRequest
{
    [Required, StringLength(1000)] public string RetrospectiveReason { get; set; }
}

public class HoldWaterQualityPeriodRequest
{
    [Required, StringLength(1000)] public string HoldReason { get; set; }
}

/// <summary>
/// Manual entry. Automatic capture from production consumption stays deferred
/// (deferred-and-next-steps.md), so nothing populates this from a production step.
/// </summary>
public class RecordWaterUseRequest
{
    [Required] public Guid WaterQualityPeriodId { get; set; }

    [Required] public DateTime UsedAt { get; set; }

    public Guid? BatchManufacturingRecordId { get; set; }

    public Guid? ProductionActivityStepId { get; set; }
}

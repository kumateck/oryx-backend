using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using SHARED;

namespace DOMAIN.Entities.RndStabilityStudies;

public class RndStabilityChamber : BaseEntity
{
    [StringLength(100)] public string Code { get; set; }
    [StringLength(255)] public string Name { get; set; }

    // Free text rather than a hardcoded enum: ICH's stability condition/zone
    // tables (Q1A(R2)/Q1E) are under active revision (a consolidated ICH Q1
    // draft was published in 2025), so this shouldn't be locked to today's
    // exact set of conditions - e.g. "Long-term 25C/60%RH", "Accelerated 40C/75%RH".
    [StringLength(255)] public string ConditionType { get; set; }
    public decimal TargetTemperature { get; set; }
    public decimal? TargetHumidity { get; set; }
    public DateTime? CalibrationDueDate { get; set; }
    public DateTime? LastCalibratedAt { get; set; }
}

public class RndStabilityChamberDto : BaseDto
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string ConditionType { get; set; }
    public decimal TargetTemperature { get; set; }
    public decimal? TargetHumidity { get; set; }
    public DateTime? CalibrationDueDate { get; set; }
    public DateTime? LastCalibratedAt { get; set; }
}

public class CreateRndStabilityChamberRequest
{
    [StringLength(100)] public string Code { get; set; }
    [StringLength(255)] public string Name { get; set; }
    [StringLength(255)] public string ConditionType { get; set; }
    public decimal TargetTemperature { get; set; }
    public decimal? TargetHumidity { get; set; }
    public DateTime? CalibrationDueDate { get; set; }
    public DateTime? LastCalibratedAt { get; set; }
}

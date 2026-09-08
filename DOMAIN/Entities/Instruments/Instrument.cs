using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Instruments;

public class Instrument : BaseEntity
{
    [StringLength(100)] public string Code { get; set; }
    [StringLength(1000)] public string Name { get; set; }
    public DateTime? CalibrationDueDate { get; set; }
    public DateTime? LastCalibratedAt { get; set; }
    public Guid? CalibrationCertificateAttachmentId { get; set; }
    public Attachment CalibrationCertificateAttachment { get; set; }
    public InstrumentQualificationStatus QualificationStatus { get; set; }
}

public enum InstrumentQualificationStatus
{
    Qualified = 0,
    DueForCalibration = 1,
    Overdue = 2,
    OutOfService = 3,
}

public class InstrumentDto : BaseDto
{
    public string Code { get; set; }
    public string Name { get; set; }
    public DateTime? CalibrationDueDate { get; set; }
    public DateTime? LastCalibratedAt { get; set; }
    public Guid? CalibrationCertificateAttachmentId { get; set; }
    public InstrumentQualificationStatus QualificationStatus { get; set; }
}

public class UpdateInstrumentCalibrationRequest
{
    public DateTime? CalibrationDueDate { get; set; }
    public DateTime? LastCalibratedAt { get; set; }
    public Guid? CalibrationCertificateAttachmentId { get; set; }
    public InstrumentQualificationStatus QualificationStatus { get; set; }
}

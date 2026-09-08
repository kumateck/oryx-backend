using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using SHARED;

namespace DOMAIN.Entities.Products.Equipments;

public class Equipment : BaseEntity
{
    [StringLength(100)] public string Name { get; set; }
    [StringLength(100000)] public string EquipmentNumber { get; set; }
    public bool IsStorage { get; set; }
    public decimal CapacityQuantity { get; set; }
    public Guid? UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public bool RelevanceCheck { get; set; }
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; }
    [StringLength(1000)] public string Location { get; set; }
    [StringLength(1000)] public string Model { get; set; }
    [StringLength(1000)] public string SerialNumber { get; set; }
}

public class EquipmentDto : BaseDto
{
    public string Name { get; set; }
    public string EquipmentNumber { get; set; }
    public bool IsStorage { get; set; }
    public decimal CapacityQuantity { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public bool RelevanceCheck { get; set; }
    public CollectionItemDto Department { get; set; }
    public string Location { get; set; }
    public string Model { get; set; }
    public string SerialNumber { get; set; }
}

public class CreateQcEquipment
{
    [StringLength(10000, ErrorMessage = "Field must be less than 1000 characters")]
    public string EquipmentId { get; set; }
    [StringLength(10000, ErrorMessage = "Field must be less than 1000 characters")]
    public string Name { get; set; }
    public Guid QcEquipmentCategoryId { get; set; }
    [StringLength(10000, ErrorMessage = "Field must be less than 1000 characters")]
    public string SerialNumber { get; set; }
    [StringLength(10000, ErrorMessage = "Field must be less than 1000 characters")]
    public string Make { get; set; }
    [StringLength(10000, ErrorMessage = "Field must be less than 1000 characters")]
    public string Model { get; set; }
    public DateTime? CalibrationDueDate { get; set; }
    public DateTime? LastCalibratedAt { get; set; }
    public Guid? CalibrationCertificateAttachmentId { get; set; }
    public QcEquipmentQualificationStatus QualificationStatus { get; set; }
}

public class QcEquipment : BaseEntity
{
    [StringLength(10000)] public string EquipmentId { get; set; }
    [StringLength(10000)] public string Name { get; set; }
    public Guid QcEquipmentCategoryId { get; set; }
    public QcEquipmentCategory QcEquipmentCategory { get; set; }
    [StringLength(10000)] public string SerialNumber { get; set; }
    [StringLength(10000)] public string Make { get; set; }
    [StringLength(10000)] public string Model { get; set; }
    public DateTime? CalibrationDueDate { get; set; }
    public DateTime? LastCalibratedAt { get; set; }
    public Guid? CalibrationCertificateAttachmentId { get; set; }
    public Attachment CalibrationCertificateAttachment { get; set; }
    public QcEquipmentQualificationStatus QualificationStatus { get; set; }
}

public enum QcEquipmentQualificationStatus
{
    Qualified = 0,
    DueForCalibration = 1,
    Overdue = 2,
    OutOfService = 3,
}

public class CreateQcEquipmentCategory
{
    [StringLength(10000, ErrorMessage = "Field must be less than 1000 characters")]
    public string Name { get; set; }
}

public class QcEquipmentCategory : BaseEntity
{
    [StringLength(10000)] public string Name { get; set; }
}

public class QcEquipmentCategoryDto : BaseDto
{
    public string Name { get; set; }
}

public class QcEquipmentDto : BaseDto
{
    public string EquipmentId { get; set; }
    public string Name { get; set; }
    public QcEquipmentCategoryDto QcEquipmentCategory { get; set; }
    public string SerialNumber { get; set; }
    public string Make { get; set; }
    public string Model { get; set; }
    public DateTime? CalibrationDueDate { get; set; }
    public DateTime? LastCalibratedAt { get; set; }
    public Guid? CalibrationCertificateAttachmentId { get; set; }
    public QcEquipmentQualificationStatus QualificationStatus { get; set; }
}
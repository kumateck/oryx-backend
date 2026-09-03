using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Procurement.Suppliers;

public class SupplierCertification : BaseEntity
{
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; }
    public SupplierCertificationType CertificationType { get; set; }
    [Required, StringLength(255)] public string CertificateNumber { get; set; }
    [Required, StringLength(255)] public string IssuingBody { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public Guid? AttachmentId { get; set; }
    public Attachment Attachment { get; set; }
}

public enum SupplierCertificationType
{
    Gmp = 0,
    Iso9001 = 1,
    Iso13485 = 2,
    WhoPrequalified = 3,
    Other = 4,
}

public class SupplierCertificationRequest
{
    public SupplierCertificationType CertificationType { get; set; }
    [Required, StringLength(255)] public string CertificateNumber { get; set; }
    [Required, StringLength(255)] public string IssuingBody { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public Guid? AttachmentId { get; set; }
}

public class SupplierCertificationDto : BaseDto
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; }
    public SupplierCertificationType CertificationType { get; set; }
    public string CertificateNumber { get; set; }
    public string IssuingBody { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public Guid? AttachmentId { get; set; }
}

using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.MaterialSampling;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.QualityRoutines;

public enum CommercialCertificateTarget
{
    Material = 0,
    Product = 1,
}

public class CommercialCertificate : BaseEntity
{
    public CommercialCertificateTarget Target { get; set; }
    public Guid? MaterialSamplingId { get; set; }
    public DOMAIN.Entities.MaterialSampling.MaterialSampling MaterialSampling { get; set; }
    public Guid? AnalyticalTestRequestId { get; set; }
    public AnalyticalTestRequest AnalyticalTestRequest { get; set; }
    [StringLength(100)] public string CertificateCode { get; set; }
    public bool Combined { get; set; }
    [StringLength(100000000)] public string RowsJson { get; set; }
    public DateTime IssuedAt { get; set; }
    public Guid IssuedById { get; set; }
    public User IssuedBy { get; set; }
}

public class CommercialCertificateDto
{
    public Guid Id { get; set; }
    public CommercialCertificateTarget Target { get; set; }
    public Guid? MaterialSamplingId { get; set; }
    public Guid? AnalyticalTestRequestId { get; set; }
    public string CertificateCode { get; set; }
    public bool Combined { get; set; }
    public string RowsJson { get; set; }
    public DateTime IssuedAt { get; set; }
}

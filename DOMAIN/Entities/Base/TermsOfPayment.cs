using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Base;

public class TermsOfPayment:BaseEntity
{
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
}
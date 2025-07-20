using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Instruments;

public class Instrument : BaseEntity
{
    [StringLength(100)] public string Code { get; set; }
    [StringLength(1000)] public string Name { get; set; }
}